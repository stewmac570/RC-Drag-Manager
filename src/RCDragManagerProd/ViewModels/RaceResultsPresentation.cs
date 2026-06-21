using System.Collections.Generic;

namespace RCDragManagerProd.ViewModels
{
    public sealed class RaceResultsPresentation
    {
        public string EventName { get; set; }
        public string Summary { get; set; }
        public bool HasResults { get; set; }
        public bool HasRoundRobinStandings { get; set; }
        public List<RaceResultsPhaseView> Phases { get; set; } = new List<RaceResultsPhaseView>();
        public List<RaceResultsListRow> ResultRows { get; set; } = new List<RaceResultsListRow>();
        public List<RaceResultsStandingRow> Standings { get; set; } = new List<RaceResultsStandingRow>();
    }

    public sealed class RaceResultsPhaseView
    {
        public string Phase { get; set; }
        public List<RaceResultsRoundView> Rounds { get; set; } = new List<RaceResultsRoundView>();
    }

    public sealed class RaceResultsRoundView
    {
        public string RoundLabel { get; set; }
        public List<RaceResultsMatchCard> Matches { get; set; } = new List<RaceResultsMatchCard>();
    }

    public sealed class RaceResultsMatchCard
    {
        public string MatchLabel { get; set; }
        public string Driver1 { get; set; }
        public string Driver2 { get; set; }
        public string Winner { get; set; }
        public bool IsComplete { get; set; }
    }

    public sealed class RaceResultsListRow
    {
        public string Phase { get; set; }
        public string Round { get; set; }
        public string Match { get; set; }
        public string Winner { get; set; }
        public string Loser { get; set; }
        public string Pairing { get; set; }
    }

    public sealed class RaceResultsStandingRow
    {
        public int Rank { get; set; }
        public string Driver { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public string Points { get; set; }
        public string OpponentStrength { get; set; }
    }
}
