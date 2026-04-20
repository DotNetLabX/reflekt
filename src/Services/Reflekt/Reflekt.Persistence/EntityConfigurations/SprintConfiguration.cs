using BuildingBlocks.Persistence.EntityConfigurations;
using Reflekt.Domain.Sprints;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class SprintConfiguration : EntityConfiguration<Sprint>
{
    public override void Configure(EntityTypeBuilder<Sprint> builder)
    {
        base.Configure(builder);
        builder.ToTable("Sprints");
        builder.Property(s => s.TeamId).IsRequired();
        builder.HasOne<Team>().WithMany().HasForeignKey(s => s.TeamId);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.State).IsRequired();
        builder.Property(s => s.CurrentPhase).IsRequired();
        builder.Property(s => s.VotesPerParticipant).IsRequired().HasDefaultValue(3);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.HasIndex(s => s.TeamId);
    }
}
