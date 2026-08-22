using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityAuth.Infrastructure.Persistence.Configurations;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");

        builder.HasKey(se => se.Id);

        builder.HasIndex(se => se.UserId);
        builder.HasIndex(se => se.EventType);
        builder.HasIndex(se => se.CreatedAt);

        builder.Property(se => se.IpAddress)
            .HasMaxLength(45);

        builder.Property(se => se.UserAgent)
            .HasMaxLength(500);

        builder.Property(se => se.Metadata)
            .HasMaxLength(2000);

        // Store enum as string for readability
        builder.Property(se => se.EventType)
            .HasConversion<string>()
            .HasMaxLength(100);
    }
}
