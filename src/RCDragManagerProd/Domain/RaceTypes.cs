namespace RCDragManagerProd.Domain
{
    public static class RaceTypes
    {
        public const string RoundRobin = "Round Robin";
        public const string MultiCarRoundRobin = "Multi-Car Round Robin";
        public const string LosersBracket = "Losers Bracket";
        public const string Finals = "Finals";

        public static bool IsRoundRobinFormat(string raceType) =>
            string.Equals(raceType?.Trim(), RoundRobin, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raceType?.Trim(), MultiCarRoundRobin, System.StringComparison.OrdinalIgnoreCase);
    }
}
