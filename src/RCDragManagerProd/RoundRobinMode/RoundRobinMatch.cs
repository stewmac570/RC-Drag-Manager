using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.Domain; // Assuming Driver is defined here  

public class RoundRobinMatch
{
    public int MatchId { get; set; }
    public Driver Driver1 { get; set; }
    public Driver Driver2 { get; set; }
    public string RoundLabel { get; set; } // "R1", "R2", etc.
}
