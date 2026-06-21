using System;
using System.Collections.Generic;

namespace RCDragManagerProd.Domain
{
    /// <summary>
    /// Durable historical results for a race. Unlike ResumeSnapshot, this model is
    /// presentation-oriented and remains useful after the race is closed.
    /// </summary>
    public sealed class RaceResultsArchive
    {
        public List<RacePhaseResultSnapshot> Phases { get; set; } = new List<RacePhaseResultSnapshot>();
        public List<RoundRobinStandingSnapshot> RoundRobinStandings { get; set; } =
            new List<RoundRobinStandingSnapshot>();
        public int? ChampionDriverId { get; set; }
        public string ChampionName { get; set; }
        public int? RunnerUpDriverId { get; set; }
        public string RunnerUpName { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public sealed class RacePhaseResultSnapshot
    {
        public string Phase { get; set; }
        public DateTime CapturedAt { get; set; }
        public List<RaceResultMatchSnapshot> Matches { get; set; } =
            new List<RaceResultMatchSnapshot>();
    }

    public sealed class RaceResultMatchSnapshot
    {
        public int MatchId { get; set; }
        public string RoundLabel { get; set; }
        public int? Driver1Id { get; set; }
        public string Driver1Name { get; set; }
        public int? Driver2Id { get; set; }
        public string Driver2Name { get; set; }
        public int? Driver1Seed { get; set; }
        public int? Driver2Seed { get; set; }
        public int? FromMatch1 { get; set; }
        public int? FromMatch2 { get; set; }
        public int? WinnerDriverId { get; set; }
        public string WinnerName { get; set; }
        public int? LoserDriverId { get; set; }
        public string LoserName { get; set; }
    }

    public sealed class RoundRobinStandingSnapshot
    {
        public int Rank { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }
}
