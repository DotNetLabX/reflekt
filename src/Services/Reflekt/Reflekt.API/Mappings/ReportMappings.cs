using Reflekt.API.Features.Reports;
using Reflekt.Domain.Cards;

namespace Reflekt.API.Mappings;

public class ReportMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EvidenceLink, ReportEvidenceLinkResponse>()
            .Map(d => d.Source, s => s.Source.ToString());

        config.NewConfig<Card, ReportCardResponse>();
        config.NewConfig<Card, ReportActionItemResponse>();
    }
}
