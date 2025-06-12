namespace RCDragManagerProd
{
    public class RandomMatch
    {
        public int MatchId { get; set; }
        public Driver Seed1 { get; set; }
        public Driver Seed2 { get; set; }
        public int? FromMatch1 { get; set; }
        public int? FromMatch2 { get; set; }
        public string RoundLabel { get; set; }
    }
}
