using Baytology.Domain.Entities;
using Baytology.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Baytology.Infrastructure.Data.Configurations;

public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AgentUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.AgentUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasIndex(x => x.AgentUserId);
        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => new { x.PropertyId, x.AgentUserId });
    }
}
