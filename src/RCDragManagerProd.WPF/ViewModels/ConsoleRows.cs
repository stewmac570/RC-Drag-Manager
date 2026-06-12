namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>A row in the pairings (bracket) list — either a round header or a match.</summary>
    public sealed class PairingDisplayRow
    {
        public bool IsHeader { get; set; }
        public string HeaderText { get; set; }
        public string MatchLabel { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public bool Bye1 { get; set; }
        public bool Bye2 { get; set; }
    }

    /// <summary>A row in the winners (results) list — either a round header or a result.</summary>
    public sealed class WinnerDisplayRow
    {
        public bool IsHeader { get; set; }
        public string HeaderText { get; set; }
        public string MatchLabel { get; set; }
        public string Winner { get; set; }
        public string Loser { get; set; }
        public int MatchId { get; set; }
    }

    /// <summary>A row in the console driver list.</summary>
    public sealed class ConsoleDriverRow
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string QualText { get; set; }
        public string DialInText { get; set; }
    }
}
