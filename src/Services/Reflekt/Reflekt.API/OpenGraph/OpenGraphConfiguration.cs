using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Reflekt.API.OpenGraph;

public static class OpenGraphConfiguration
{
    public static IServiceCollection AddReflektOpenGraph(this IServiceCollection services)
    {
        var channel = Channel.CreateBounded<FetchJob>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        services.AddSingleton(channel.Reader);
        services.AddSingleton(channel.Writer);

        services.AddHttpClient("opengraph", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            ConnectCallback = ConnectToAllowedAddressAsync
        });

        services.AddHostedService<OpenGraphBackgroundService>();
        services.AddSingleton<OpenGraphFetcher>();

        return services;
    }

    private static async ValueTask<Stream> ConnectToAllowedAddressAsync(
        SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, ct);
        if (addresses.Length == 0 || !addresses.All(OpenGraphAddressPolicy.IsAllowed))
            throw new HttpRequestException($"Connection to '{host}' refused: address not allowed.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
