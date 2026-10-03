namespace Hotdrop
{
    /// <summary>
    /// Spec section 14 generated aliases (AmberFox-4821) for offline play. Uses
    /// the same word lists as the backend AliasGenerator; online aliases always
    /// come from the server.
    /// </summary>
    public static class OfflineAlias
    {
        static readonly string[] Adjectives =
        {
            "Amber", "Swift", "Brisk", "Cobalt", "Copper", "Dusty", "Gentle", "Golden", "Lucky", "Mellow",
            "Nimble", "Quiet", "Rapid", "Silver", "Steady", "Sunny", "Tidal", "Vivid", "Windy", "Zesty",
        };

        static readonly string[] Animals =
        {
            "Fox", "Otter", "Heron", "Lynx", "Falcon", "Badger", "Gecko", "Marten", "Puffin", "Ibex",
            "Kestrel", "Wombat", "Tapir", "Crane", "Hare", "Raven", "Newt", "Bison", "Koala", "Swift",
        };

        public static string Next(int seed)
        {
            var random = new System.Random(seed);
            return $"{Adjectives[random.Next(Adjectives.Length)]}{Animals[random.Next(Animals.Length)]}-{random.Next(1000, 10000)}";
        }
    }
}
