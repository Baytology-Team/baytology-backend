using Baytology.Domain.Entities;
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

        builder.HasIndex(x => x.AgentUserId);
        builder.HasIndex(x => x.PropertyId);
    }
}
