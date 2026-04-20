using BuildingBlocks.Persistence.EntityConfigurations;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class TeamConfiguration : EntityConfiguration<Team>
{
    public override void Configure(EntityTypeBuilder<Team> builder)
    {
        base.Configure(builder);
        builder.ToTable("Teams");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.CreatorEmail).IsRequired().HasMaxLength(256);
        builder.HasIndex(t => t.CreatorEmail).IsUnique();
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
