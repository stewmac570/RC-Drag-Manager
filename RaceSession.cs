using System;
using System.Collections.Generic;

namespace RCDragManagerProd
{
    public class RaceSession
    {
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string RaceType { get; set; }      // Pro Ladder, Random Draw, etc
        public string ClassType { get; set; }     // Heads Up, Bracket Class, Dial-In
        public double? FixedDialIn { get; set; }  // Only used for Bracket Class
        public List<RaceSessionDriverEntry> DriverEntries { get; set; }
    }

    public class RaceSessionDriverEntry
    {
        public int DriverID { get; set; }
        public string DriverName { get; set; }
        public int CarID { get; set; }
        public string CarName { get; set; }
        public string ClassType { get; set; }
        public double? DialIn { get; set; }
        public double? QualifyingTime { get; set; }
        public int? Seed { get; set; }
    }
}
