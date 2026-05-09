using Blocks.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Sprints;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class ParticipantConfiguration : EntityConfiguration<Participant>
{
    public override void Configure(EntityTypeBuilder<Participant> builder)
    {
        base.Configure(builder);
        builder.ToTable("Participants");
        builder.Property(p => p.SprintId).IsRequired();
        builder.HasOne<Sprint>().WithMany().HasForeignKey(p => p.SprintId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(50);
        builder.Property(p => p.GuestToken).IsRequired();
        builder.HasIndex(p => p.GuestToken).IsUnique();
        builder.HasIndex(p => p.SprintId);
        builder.Property(p => p.JoinedAt).IsRequired();
    }
}
