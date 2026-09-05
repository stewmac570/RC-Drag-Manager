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
        /// What decides the class, in one sentence: most points, and how a level
        /// finish is settled.
        /// </summary>
        public string ScoringNote { get; set; }

        /// <summary>
        /// A legend giving each result its value <em>and the reason for it</em>. Saying
        /// "loss 1" on its own reads as a mistake — you lost, so why score anything?
        /// The reason column is what makes the table stop looking arbitrary.
        /// </summary>
        public List<ScoringLegendRow> ScoringLegend { get; set; } = new List<ScoringLegendRow>();


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

    /// <summary>One line of the scoring legend: the result, its value, and why.</summary>
    public sealed class ScoringLegendRow
    {
        public string Result { get; set; }
        public string Points { get; set; }
        public string Why { get; set; }
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
        /// What the beaten-drivers score contributes to the total — already divided
        /// down. The column shows the contribution, not the raw sum, so the row adds up
        /// on the page: Points + H2H + Beaten = TOTAL, exactly.
        /// </summary>
        public string Beaten { get; set; }

        /// <summary>The head-to-head contribution. Shown as 0.0 rather than blank for
        /// the same reason: the row has to add up.</summary>
        public string HeadToHead { get; set; }

        /// <summary>
        /// The three parts added together. This is what the order is sorted on, so it
        /// is the last column and the only one that decides anything.
        /// </summary>
        public string Total { get; set; }
    }
}
