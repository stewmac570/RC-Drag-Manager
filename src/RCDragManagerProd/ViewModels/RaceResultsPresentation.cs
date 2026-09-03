using System.Collections.Generic;

namespace RCDragManagerProd.ViewModels
{
    public sealed class RaceResultsPresentation
    {
        public string EventName { get; set; }
        public string Summary { get; set; }
        public bool HasResults { get; set; }
        public bool HasRoundRobinStandings { get; set; }

        /// <summary>True once a champion is recorded, so the Winner tab has something
        /// to show. The winner board is reachable from here for the life of the class,
        /// not just in the moment the last race is called.</summary>
        public bool HasWinner { get; set; }

        /// <summary>
        /// One line saying what a win, bye and loss are worth. Everything else about
        /// the scoring is shown as arithmetic on the rows themselves rather than
        /// explained in prose.
        /// </summary>
        public string ScoringNote { get; set; }

        /// <summary>
        /// Plain sentences for the places that were level on points, naming the rule
        /// that separated them. Empty when nothing was tied — which is the point: the
        /// tiebreak machinery only shows up when it actually decided something.
        /// </summary>
        public List<string> TieNotes { get; set; } = new List<string>();

        public bool HasTieNotes => TieNotes != null && TieNotes.Count > 0;
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

        /// <summary>Byes received. Shown because they carry points: without the column
        /// a one-win, one-loss driver on 7 points looks like a mistake.</summary>
        public int Byes { get; set; }

        public string Points { get; set; }

        /// <summary>
        /// The sum written out — "2 wins (8) + 1 bye (2)" — so the points column can
        /// be checked at a glance instead of taken on trust.
        /// </summary>
        public string PointsWorking { get; set; }

        /// <summary>
        /// Kept for the record, but no longer a column: "opponent strength 20.00" told
        /// a race director nothing. It now appears only inside a tie note, and only
        /// when it is what actually settled a placing.
        /// </summary>
        public string OpponentStrength { get; set; }
    }
}
