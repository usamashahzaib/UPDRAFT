using System.Security.Cryptography;

namespace HotdropBackend.Services;

/// <summary>Spec section 14: generated public aliases such as AmberFox-4821. No free-text names.</summary>
public static class AliasGenerator
{
    static readonly string[] Adjectives =
    [
        "Amber", "Swift", "Brisk", "Cobalt", "Copper", "Dusty", "Gentle", "Golden", "Lucky", "Mellow",
        "Nimble", "Quiet", "Rapid", "Silver", "Steady", "Sunny", "Tidal", "Vivid", "Windy", "Zesty",
    ];

    static readonly string[] Animals =
    [
        "Fox", "Otter", "Heron", "Lynx", "Falcon", "Badger", "Gecko", "Marten", "Puffin", "Ibex",
        "Kestrel", "Wombat", "Tapir", "Crane", "Hare", "Raven", "Newt", "Bison", "Koala", "Swift",
    ];

    public static string Next() =>
        $"{Adjectives[RandomNumberGenerator.GetInt32(Adjectives.Length)]}{Animals[RandomNumberGenerator.GetInt32(Animals.Length)]}-{RandomNumberGenerator.GetInt32(1000, 10000)}";
}

