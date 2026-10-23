using System.Net;
using System.Net.Sockets;

namespace Reflekt.API.OpenGraph;

public static class OpenGraphAddressPolicy
{
    public static bool IsAllowed(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => !IsRejectedIpv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => !IsRejectedIpv6(address.GetAddressBytes()),
            _ => false,
        };
    }

    private static bool IsRejectedIpv4(byte[] b) =>
        b[0] == 0
        || b[0] == 10
        || (b[0] == 100 && (b[1] & 0xC0) == 64)
        || b[0] == 127
        || (b[0] == 169 && b[1] == 254)
        || (b[0] == 172 && (b[1] & 0xF0) == 16)
        || (b[0] == 192 && b[1] == 168)
        || (b[0] & 0xF0) == 224
        || b.All(octet => octet == 255);

    private static bool IsRejectedIpv6(byte[] b) =>
        IsUnspecifiedOrLoopback(b)
        || (b[0] == 0xFE && (b[1] & 0xC0) == 0x80)
        || (b[0] & 0xFE) == 0xFC
        || b[0] == 0xFF;

    private static bool IsUnspecifiedOrLoopback(byte[] b) =>
        b.Take(15).All(octet => octet == 0) && b[15] is 0 or 1;
}
