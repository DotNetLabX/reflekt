using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.Teams;

namespace Reflekt.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.CreatorEmail).IsRequired().HasMaxLength(256);
        builder.HasIndex(t => t.CreatorEmail).IsUnique();
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
