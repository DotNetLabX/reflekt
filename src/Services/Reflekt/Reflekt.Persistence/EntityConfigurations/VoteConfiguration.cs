using BuildingBlocks.Persistence.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Participants;
using Reflekt.Domain.Votes;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class VoteConfiguration : EntityConfiguration<Vote>
{
    public override void Configure(EntityTypeBuilder<Vote> builder)
    {
        base.Configure(builder);
        builder.ToTable("Votes");
        builder.Property(v => v.CardId).IsRequired();
        builder.HasOne<Card>().WithMany().HasForeignKey(v => v.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(v => v.ParticipantId).IsRequired();
        builder.HasOne<Participant>().WithMany().HasForeignKey(v => v.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(v => v.CardId);
        builder.HasIndex(v => v.ParticipantId);
        builder.Property(v => v.CreatedAt).IsRequired();
    }
}
