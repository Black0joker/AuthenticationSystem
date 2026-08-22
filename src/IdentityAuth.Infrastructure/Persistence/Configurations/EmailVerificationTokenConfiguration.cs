using IdentityAuth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityAuth.Infrastructure.Persistence.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationTokens");

        builder.HasKey(evt => evt.Id);

        builder.Property(evt => evt.TokenHash)
            .IsRequired();

        builder.HasIndex(evt => evt.TokenHash)
            .IsUnique();

        builder.HasIndex(evt => evt.UserId);

        // Ignore computed properties
        builder.Ignore(evt => evt.IsExpired);
        builder.Ignore(evt => evt.IsUsed);
    }
}
