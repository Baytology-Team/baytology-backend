using Baytology.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baytology.Infrastructure.Data.Configurations;

public class UserPropertyPreferenceConfiguration : IEntityTypeConfiguration<UserPropertyPreference>
{
    public void Configure(EntityTypeBuilder<UserPropertyPreference> builder)
    {
        builder.ToTable("UserPropertyPreferences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.City)
            .HasMaxLength(100);

        builder.Property(x => x.District)
            .HasMaxLength(100);

        builder.Property(x => x.MinPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MaxPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinArea)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MaxArea)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Create index on UserId for faster lookups
        builder.HasIndex(x => x.UserId);

        // Create composite index for active preferences
        builder.HasIndex(x => new { x.UserId, x.IsActive });
    }
}
