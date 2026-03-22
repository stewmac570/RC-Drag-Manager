using System.Collections.Generic;

namespace RCDragManagerProd.Integration
{
    public class LiveRaceUpdateDto
    {
        public string EventName { get; set; }
        public string EventDate { get; set; }
        public string CurrentRound { get; set; }
        public string NextUp { get; set; }
        public List<LiveMatchDto> Matches { get; set; }
    }

    public class LiveMatchDto
    {
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
    }
}
