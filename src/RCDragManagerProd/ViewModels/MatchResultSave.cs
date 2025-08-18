namespace RCDragManagerProd.ViewModels
{
    // summary>
    /// Represents the result of a match, including the winning and losing drivers.
    public class MatchResultSave
    {
        public int MatchId { get; set; } // Engine MatchId; –1 when this row is a header.
        public int WinnerDriverId { get; set; } // Id of the winning driver
        public int LoserDriverId { get; set; } // Id of the losing driver
    }

}
