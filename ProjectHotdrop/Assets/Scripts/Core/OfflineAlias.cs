namespace Hotdrop
{
    /// <summary>
    /// Readable player names for practice opponents and server-filled seats.
    /// Human online aliases still come from the server.
    /// </summary>
    public static class OfflineAlias
    {
        static readonly string[] GivenNames =
        {
            "Aisha", "Ali", "Amara", "Arman", "Bilal", "Daniyal", "Emaan", "Farah", "Hana", "Haris",
            "Imran", "Jamal", "Kiran", "Laiba", "Maya", "Nadia", "Omar", "Rafi", "Sana", "Usman",
        };

        static readonly string[] FamilyNames =
        {
            "Abbasi", "Ahmed", "Akhtar", "Ali", "Bashir", "Farooq", "Hameed", "Hussain", "Iqbal", "Khan",
            "Malik", "Mir", "Nawaz", "Qureshi", "Raza", "Saeed", "Shah", "Siddiqui", "Tariq", "Yousaf",
        };

        public static string Next(int seed)
        {
            var random = new System.Random(seed);
            return $"{GivenNames[random.Next(GivenNames.Length)]} {FamilyNames[random.Next(FamilyNames.Length)]}";
        }
    }
}
