using System.Collections.Generic;

namespace RCDragManagerProd.Integration
{
    public class LiveRaceUpdateDto
    {
        public string EventName { get; set; }
        public string EventDate { get; set; }
        public string ClassType { get; set; }
        public string RaceType { get; set; }
        public string CurrentRound { get; set; }
        public string NextUp { get; set; }
        public string RRStandings { get; set; }
        public List<LiveMatchDto> Matches { get; set; }
        public List<LiveWinnerDto> Winners { get; set; }
    }

    public class LiveMatchDto
    {
        public string RoundLabel { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public string WinnerName { get; set; }
    }

    public class LiveWinnerDto
    {
        public string RoundLabel { get; set; }
        public string WinnerName { get; set; }
        public string LoserName { get; set; }
    }
}
