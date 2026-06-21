using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RCDragManagerProd.Logging;  // Assuming you have a logging utility
using RCDragManagerProd.RandomMode;  // Assuming this is where your domain models are defined

namespace RCDragManagerProd.Domain
{
    public class RaceSession
    {
        public int Id { get; set; }
        public Guid EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string RaceType { get; set; }
        public string ClassType { get; set; }
        public double? FixedDialIn { get; set; }

        // The mode the event STARTED in. Unlike RaceType (which mutates to
        // "Losers Bracket"/"Finals" during the event) this is set once and never
        // changes, so resume can regenerate the original bracket.
        public string OriginalRaceType { get; set; }

        // Bracket-structure + phase snapshot for resuming an interrupted event.
        // Null on pre-feature saves or before a bracket is generated.
        public ResumeSnapshot Resume { get; set; }

        // Durable display/reporting history. This is separate from Resume because
        // completed races still need their RR standings and elimination ladders.
        public RaceResultsArchive ResultsArchive { get; set; } = new RaceResultsArchive();

        // True once the operator has CLOSED the race (event finished / done with the
        // console). Distinct from a Save Progress checkpoint, which leaves the race open
        // and resumable. See issue #255 (Save Progress vs Close Race) and #294 (resume).
        public bool IsClosed { get; set; }

        // -----------------------------
        // Round Robin configuration
        // -----------------------------
        // "Standard" or "QMDRA"
        public string RoundRobinVariant { get; set; }

        // Required when RoundRobinVariant == "QMDRA"
        public int? RoundsToRun { get; set; }

        public List<RaceSessionDriverEntry> DriverEntries { get; set; }

        // Serializable backing store for PairingHistory (System.Text.Json cannot handle ValueTuple)
        public List<int[]> PairingHistoryRaw { get; set; } = new List<int[]>();

        [JsonIgnore]
        public HashSet<(int, int)> PairingHistory
        {
            get => new HashSet<(int, int)>(PairingHistoryRaw.Select(a => (a[0], a[1])));
            set => PairingHistoryRaw = value.Select(t => new[] { t.Item1, t.Item2 }).ToList();
        }
        public List<MatchResultSave> SavedResults { get; set; } = new List<MatchResultSave>();
        public List<string> SavedRevealedRounds { get; set; } = new List<string>();
        public List<RoundRobinMatch> RoundRobinMatches { get; set; } = new List<RoundRobinMatch>();
        public List<RandomMatch> Matches { get; set; } = new List<RandomMatch>();
        public List<Driver> BuybackDrivers { get; set; } = new();
        public List<Driver> TopDriversSnapshot { get; set; } = new();
        public List<Driver> Drivers { get; set; } = new();

        public RaceSession()
        {
            EventId = Guid.NewGuid();
            DriverEntries = new List<RaceSessionDriverEntry>();
            Logger.Log("[DEBUG] RaceSession ctor – Lists initialised.");

            // Defaults (keeps old behavior unless UI sets QMDRA + N)
            RoundRobinVariant = "Standard";
            RoundsToRun = null;

            Logger.Log("[DEBUG] RaceSession ctor – RR defaults set: Variant=Standard, RoundsToRun=null");

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


