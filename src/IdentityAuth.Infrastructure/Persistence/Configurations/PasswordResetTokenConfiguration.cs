using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityAuth.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(prt => prt.Id);

        builder.Property(prt => prt.TokenHash)
            .IsRequired();

        builder.HasIndex(prt => prt.TokenHash)
            .IsUnique();

        builder.HasIndex(prt => prt.UserId);

        // Ignore computed properties
        builder.Ignore(prt => prt.IsExpired);
        builder.Ignore(prt => prt.IsUsed);
    }
}
