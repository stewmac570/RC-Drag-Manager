using System;
using System.Collections.Generic;

namespace RCDragManagerProd.Domain
{
    public class MultiClassEvent
    {
        public int Id { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public List<RaceSession> ClassSessions { get; set; } = new List<RaceSession>();

        /// <summary>Who has paid what at this event. Null on events saved before the
        /// money sheet existed.</summary>
        public EventMoneySheet MoneySheet { get; set; }
    }
}
