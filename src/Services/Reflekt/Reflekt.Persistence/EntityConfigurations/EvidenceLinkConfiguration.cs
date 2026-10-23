using Blocks.Persistence.EntityConfigurations;
using Reflekt.Domain.Cards;

namespace Reflekt.Persistence.EntityConfigurations;

internal sealed class EvidenceLinkConfiguration : EntityConfiguration<EvidenceLink>
{
    public override void Configure(EntityTypeBuilder<EvidenceLink> builder)
    {
        base.Configure(builder);
        builder.ToTable("EvidenceLinks");
        builder.Property(e => e.CardId).IsRequired();
        builder.Property(e => e.Url).IsRequired().HasMaxLength(EvidenceLink.UrlMaxLength);
        builder.Property(e => e.Source).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(EvidenceLink.TitleMaxLength);
        builder.Property(e => e.Description).HasMaxLength(EvidenceLink.DescriptionMaxLength);
        builder.Property(e => e.ExternalKey).HasMaxLength(EvidenceLink.ExternalKeyMaxLength);
        builder.Property(e => e.FetchStatus).IsRequired();
        builder.HasIndex(e => e.CardId);
    }
}
