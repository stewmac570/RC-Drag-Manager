using System;
using System.Collections.Generic;

namespace RCDragManagerProd
{
    public class RaceSession
    {
        public int Id { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string RaceType { get; set; }
        public string ClassType { get; set; }
        public double? FixedDialIn { get; set; }
        public List<RaceSessionDriverEntry> DriverEntries { get; set; }

        // 🔥 NEW: Save Results & Rounds
        public List<MatchResultSave> SavedResults { get; set; } = new List<MatchResultSave>();
        public List<string> SavedRevealedRounds { get; set; } = new List<string>();
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

    public class MatchResultSave
    {
        public int MatchId { get; set; }
        public int WinnerDriverId { get; set; }
        public int LoserDriverId { get; set; }  // ✅ NEW
    }

}
