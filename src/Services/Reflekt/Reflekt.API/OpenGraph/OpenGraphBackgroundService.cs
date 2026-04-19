using System.Threading.Channels;

namespace Reflekt.API.OpenGraph;

public sealed class OpenGraphBackgroundService(
    ChannelReader<FetchJob> reader,
    OpenGraphFetcher fetcher,
    ILogger<OpenGraphBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in reader.ReadAllAsync(stoppingToken))
        {
            var result = await fetcher.FetchAsync(job.Url, stoppingToken);
            // TODO(feature=EvidenceLinks): Replace this log with a DB update on the EvidenceLink entity.
            logger.LogInformation(
                "OpenGraph fetch for EvidenceLink {Id}: Ok={Ok}, Title={Title}",
                job.EvidenceLinkId, result.Ok, result.Title);
        }
    }
}
