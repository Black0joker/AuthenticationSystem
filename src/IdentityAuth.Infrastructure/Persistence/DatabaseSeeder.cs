using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentityAuth.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.MigrateAsync();

            // Seed default roles
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "User",
                        NormalizedName = "USER",
                        Description = "Standard user role",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "Admin",
                        NormalizedName = "ADMIN",
                        Description = "Administrator role with full access",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} roles", roles.Count);
            }

            // Seed default permissions
            if (!await context.Permissions.AnyAsync())
            {
                var permissions = new List<Permission>
                {
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "users.read",
                        Description = "Can read user data",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "users.write",
                        Description = "Can create and update users",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "users.delete",
                        Description = "Can delete users",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = "roles.manage",
                        Description = "Can manage roles and permissions",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Permissions.AddRangeAsync(permissions);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} permissions", permissions.Count);

                // Assign all permissions to Admin role
                var adminRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");

                if (adminRole != null)
                {
                    var rolePermissions = permissions.Select(p => new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = p.Id
                    }).ToList();

                    await context.RolePermissions.AddRangeAsync(rolePermissions);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Assigned {Count} permissions to Admin role", rolePermissions.Count);
                }

                // Assign users.read to User role
                var userRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.NormalizedName == "USER");

                if (userRole != null)
                {
                    var usersReadPermission = permissions.FirstOrDefault(p => p.Name == "users.read");
                    if (usersReadPermission != null)
                    {
                        await context.RolePermissions.AddAsync(new RolePermission
                        {
                            RoleId = userRole.Id,
                            PermissionId = usersReadPermission.Id
                        });
                        await context.SaveChangesAsync();

                        logger.LogInformation("Assigned users.read permission to User role");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
