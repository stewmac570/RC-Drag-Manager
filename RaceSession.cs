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
        public HashSet<(int, int)> PairingHistory { get; set; } = new HashSet<(int, int)>();
        public List<MatchResultSave> SavedResults { get; set; } = new List<MatchResultSave>();
        public List<string> SavedRevealedRounds { get; set; } = new List<string>();
        public List<RoundRobinMatch> RoundRobinMatches { get; set; } = new List<RoundRobinMatch>();
        public List<RandomMatch> Matches { get; set; } = new List<RandomMatch>();
        public List<Driver> BuybackDrivers { get; set; } = new();
        public List<Driver> TopDriversSnapshot { get; set; } = new();
        public List<Driver> Drivers { get; set; } = new();

        public RaceSession()
        {
            DriverEntries = new List<RaceSessionDriverEntry>();
            Logger.Log("[DEBUG] RaceSession ctor – Lists initialised.");   // <-- keeps init noise in one spot
        }

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
        public int WinnerDriverId { get; set; }     // ✅ fixed
        public int LoserDriverId { get; set; }      // ✅ fixed
    }

}


