using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SillyPutty;

public static class NetworkPolicy
{
    public static bool IsPrivateIpv4(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168);
    }

    public static bool TryParsePrivate24(string value, out string prefix)
    {
        prefix = ""; var match = Regex.Match(value, @"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.\d{1,3}/24$");
        if (!match.Success) return false;
        var numbers = match.Groups.Cast<Group>().Skip(1).Select(g => int.TryParse(g.Value, out var number) ? number : 999).ToArray();
        if (numbers.Any(n => n > 255)) return false;
        var ip = IPAddress.Parse($"{numbers[0]}.{numbers[1]}.{numbers[2]}.0"); if (!IsPrivateIpv4(ip)) return false;
        prefix = $"{numbers[0]}.{numbers[1]}.{numbers[2]}"; return true;
    }
}
