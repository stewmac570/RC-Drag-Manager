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
        public double Points { get; set; }
        public double OpponentStrength { get; set; }

        /// <summary>Bonus banked for beating drivers level on points. Added 2026-09-05;
        /// events saved before that reload as 0.</summary>
        public double HeadToHeadBonus { get; set; }

        /// <summary>
        /// The number the placing is sorted on: points + head-to-head bonus + the
        /// beaten-drivers score scaled down. Added 2026-09-05; older saves reload as 0,
        /// so fall back to Points when it is missing.
        /// </summary>
        public double TotalScore { get; set; }
    }
}
