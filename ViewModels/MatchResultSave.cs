namespace RCDragManagerProd.ViewModels
{
    public class MatchResultSave
    {
        public int MatchId { get; set; }
        public int WinnerDriverId { get; set; }  // change from Guid to int
        public int LoserDriverId { get; set; }   // change from Guid to int
    }

}
