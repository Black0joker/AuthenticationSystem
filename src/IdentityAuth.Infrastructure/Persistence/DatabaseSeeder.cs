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
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Apply pending migrations (skip for InMemory provider used in tests)
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // Seed roles
            await SeedRolesAsync(context);

            // Seed permissions
            await SeedPermissionsAsync(context);

            // Seed role-permission assignments
            await SeedRolePermissionsAsync(context);

            // Seed default admin user
            await SeedAdminUserAsync(context, passwordHasher);

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        var roles = new[]
        {
            new Role
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567801"),
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Full system access and administrative privileges",
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567802"),
                Name = "User",
                NormalizedName = "USER",
                Description = "Standard user access",
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var role in roles)
        {
            var exists = await context.Roles.AnyAsync(r => r.NormalizedName == role.NormalizedName);
            if (!exists)
            {
                await context.Roles.AddAsync(role);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        var permissions = new[]
        {
            new Permission
            {
                Id = Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567901"),
                Name = "users.read",
                Description = "View user profiles",
                CreatedAt = DateTime.UtcNow
            },
            new Permission
            {
                Id = Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567902"),
                Name = "users.write",
                Description = "Create and update users",
                CreatedAt = DateTime.UtcNow
            },
            new Permission
            {
                Id = Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567903"),
                Name = "users.delete",
                Description = "Delete users",
                CreatedAt = DateTime.UtcNow
            },
            new Permission
            {
                Id = Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567904"),
                Name = "roles.manage",
                Description = "Manage roles and permissions",
                CreatedAt = DateTime.UtcNow
            },
            new Permission
            {
                Id = Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567905"),
                Name = "security.audit",
                Description = "View security audit logs",
                CreatedAt = DateTime.UtcNow
            },
            new Permission
            {
                Id = Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567906"),
                Name = "profile.read",
                Description = "View own profile",
                CreatedAt = DateTime.UtcNow
            },
            new Permission
            {
                Id = Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567907"),
                Name = "profile.write",
                Description = "Update own profile",
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var permission in permissions)
        {
            var exists = await context.Permissions.AnyAsync(p => p.Name == permission.Name);
            if (!exists)
            {
                await context.Permissions.AddAsync(permission);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context)
    {
        var adminRoleId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567801");
        var userRoleId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567802");

        // Admin gets all permissions
        var allPermissionIds = new[]
        {
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567901"),
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567902"),
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567903"),
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567904"),
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567905"),
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567906"),
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567907")
        };

        // User gets profile permissions only
        var userPermissionIds = new[]
        {
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567906"),
            Guid.Parse("b1b2c3d4-e5f6-7890-abcd-ef1234567907")
        };

        foreach (var permissionId in allPermissionIds)
        {
            var exists = await context.RolePermissions.AnyAsync(
                rp => rp.RoleId == adminRoleId && rp.PermissionId == permissionId);
            if (!exists)
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = adminRoleId,
                    PermissionId = permissionId
                });
            }
        }

        foreach (var permissionId in userPermissionIds)
        {
            var exists = await context.RolePermissions.AnyAsync(
                rp => rp.RoleId == userRoleId && rp.PermissionId == permissionId);
            if (!exists)
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = userRoleId,
                    PermissionId = permissionId
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext context, Application.Common.Interfaces.IPasswordHasher passwordHasher)
    {
        var adminEmail = "admin@identityauth.com";
        var normalizedEmail = adminEmail.ToUpperInvariant();

        var exists = await context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);
        if (exists)
        {
            return;
        }

        var adminUser = new User
        {
            Id = Guid.Parse("c1b2c3d4-e5f6-7890-abcd-ef1234567801"),
            Email = adminEmail,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword("Admin@123456"),
            FirstName = "System",
            LastName = "Administrator",
            IsEmailVerified = true,
            IsLocked = false,
            FailedLoginAttempts = 0,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(adminUser);

        // Assign Admin role
        var adminRoleId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567801");
        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRoleId
        });

        await context.SaveChangesAsync();
    }
}
