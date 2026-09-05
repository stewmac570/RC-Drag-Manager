using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.AppServices
{
    public static class RaceResultsPresentationBuilder
    {
        public static RaceResultsPresentation Build(RaceSession session)
        {
            var result = new RaceResultsPresentation
            {
                EventName = string.IsNullOrWhiteSpace(session?.EventName) ? "Race results" : session.EventName
            };

            var archive = session?.ResultsArchive;
            if (archive == null)
            {
                result.Summary = "No saved race results are available.";
                return result;
            }

            result.Summary = BuildSummary(archive);

            foreach (var phase in (archive.Phases ?? new List<RacePhaseResultSnapshot>())
                         .Where(p => p != null && p.Matches != null && p.Matches.Count > 0))
            {
                var phaseView = new RaceResultsPhaseView { Phase = DisplayPhase(phase.Phase) };
                foreach (var roundGroup in phase.Matches
                             .OrderBy(m => RoundLabels.CompareKey(m.RoundLabel ?? string.Empty))
                             .ThenBy(m => m.MatchId)
                             .GroupBy(m => RoundLabels.Normalize(m.RoundLabel ?? string.Empty)))
                {
                    var round = new RaceResultsRoundView { RoundLabel = DisplayRound(roundGroup.Key) };
                    foreach (var match in roundGroup)
                    {
                        round.Matches.Add(new RaceResultsMatchCard
                        {
                            MatchLabel = $"M{match.MatchId}",
                            Driver1 = SeededName(match.Driver1Seed, match.Driver1Name),
                            Driver2 = SeededName(match.Driver2Seed, match.Driver2Name),
                            Winner = string.IsNullOrWhiteSpace(match.WinnerName)
                                ? "Pending"
                                : $"Winner: {match.WinnerName}",
                            IsComplete = !string.IsNullOrWhiteSpace(match.WinnerName)
                        });

                        result.ResultRows.Add(new RaceResultsListRow
                        {
                            Phase = DisplayPhase(phase.Phase),
                            Round = DisplayRound(roundGroup.Key),
                            Match = $"M{match.MatchId}",
                            Pairing = $"{match.Driver1Name ?? "BYE"} vs {match.Driver2Name ?? "BYE"}",
                            Winner = match.WinnerName ?? "Pending",
                            Loser = match.LoserName ?? ""
                        });
                    }
                    phaseView.Rounds.Add(round);
                }
                result.Phases.Add(phaseView);
            }

            var byesByDriver = CountByes(archive);
            var ranked = (archive.RoundRobinStandings ?? new List<RoundRobinStandingSnapshot>())
                .OrderBy(s => s.Rank)
                .ToList();

            result.Standings = ranked
                .Select(s =>
                {
                    var byes = byesByDriver.TryGetValue(s.DriverId, out var b) ? b : 0;
                    return new RaceResultsStandingRow
                    {
                        Rank = s.Rank,
                        Driver = s.DriverName,
                        Wins = s.Wins,
                        Losses = s.Losses,
                        Byes = byes,
                        Points = Plain(s.Points),
                        // Both tiebreak columns show what they add, so Points + H2H +
                        // Beaten is TOTAL on the page rather than a claim to trust.
                        HeadToHead = s.HeadToHeadBonus.ToString("0.0"),
                        Beaten = (s.OpponentStrength * RoundRobinRanker.BeatenDriversWeight)
                            .ToString("0.000"),
                        // Added from the parts on the row rather than read from the saved
                        // TotalScore. Same arithmetic the ranker used, and it keeps the
                        // row adding up on events saved before TotalScore existed, where
                        // the stored value is 0.
                        Total = (s.Points +
                                 s.HeadToHeadBonus +
                                 s.OpponentStrength * RoundRobinRanker.BeatenDriversWeight)
                            .ToString("0.000")
                    };
                })
                .ToList();

            result.HasRoundRobinStandings = result.Standings.Count > 0;
            result.ScoringNote = ScoringNote;
            result.ScoringLegend = BuildScoringLegend();
            result.TieNotes = DescribeTies(ranked, archive);
            result.HasWinner = !string.IsNullOrWhiteSpace(archive.ChampionName);
            result.HasResults = result.Phases.Count > 0 || result.HasRoundRobinStandings;
            if (!result.HasResults && string.IsNullOrWhiteSpace(result.Summary))
                result.Summary = "No saved race results are available.";
            return result;
        }

        /// <summary>What actually decides the class.</summary>
        private const string ScoringNote =
            "Highest TOTAL wins the class. Each part is small enough that it can only " +
            "separate drivers the part before it left level — so a driver never passes " +
            "someone who scored more points than them.";

        /// <summary>
        /// Each result with its value and the reason for that value, read out of
        /// <c>RoundRobinRanker</c> so the numbers cannot drift from the ones that
        /// decide rank and Finals seeding.
        ///
        /// The reasons are the point of this. "Loss 1" on its own reads as a mistake —
        /// you lost, so why score at all? Because you turned up and raced, and a driver
        /// who races and loses has done more than one who never left the pits.
        /// </summary>
        private static List<ScoringLegendRow> BuildScoringLegend()
        {
            // Scoring is constant across rounds, so any round label gives the same answer.
            var pts = RoundRobinRanker.PointsForRound("RR1");

            return new List<ScoringLegendRow>
            {
                new ScoringLegendRow
                {
                    Result = "Points",
                    Points = $"{Plain(pts.Win)} / {Plain(pts.Bye)} / {Plain(pts.Loss)}",
                    Why = $"a win scores {Plain(pts.Win)}, a bye {Plain(pts.Bye)}, a loss " +
                          $"{Plain(pts.Loss)}. Every round is worth the same"
                },
                new ScoringLegendRow
                {
                    Result = "H2H",
                    Points = $"+{RoundRobinRanker.HeadToHeadBonus:0.0}",
                    Why = "beat a driver you finished level with on points and you take the " +
                          "place. Too small to pass anyone who scored more than you"
                },
                new ScoringLegendRow
                {
                    Result = "Beaten",
                    Points = "÷1000",
                    Why = "the points of the drivers you beat, added up. Splits drivers still " +
                          "level after H2H — usually two who never raced each other"
                },
                new ScoringLegendRow
                {
                    Result = "TOTAL",
                    Points = "=",
                    Why = "the three added together. The class is ordered on this and nothing else"
                }
            };
        }

        /// <summary>
        /// A sentence for each pair of drivers who finished level on points, naming the
        /// rule that separated them.
        /// </summary>
        private static List<string> DescribeTies(
            List<RoundRobinStandingSnapshot> ranked, RaceResultsArchive archive)
        {
            var notes = new List<string>();
            var headToHead = BuildHeadToHead(archive);

            foreach (var tied in ranked.GroupBy(s => s.Points).Where(g => g.Count() > 1))
            {
                var inOrder = tied.OrderBy(s => s.Rank).ToList();
                for (int i = 0; i + 1 < inOrder.Count; i++)
                {
                    var ahead = inOrder[i];
                    var behind = inOrder[i + 1];
                    var lead = $"{ahead.DriverName} and {behind.DriverName} both finished on {Plain(ahead.Points)}";

                    if (ahead.Wins != behind.Wins)
                    {
                        notes.Add($"{lead} — {ahead.DriverName} placed higher on more wins " +
                                  $"({ahead.Wins} to {behind.Wins}).");
                    }
                    else if (headToHead.TryGetValue(Pair(ahead.DriverId, behind.DriverId), out var decider) &&
                             decider.WinnerId == ahead.DriverId)
                    {
                        notes.Add($"{lead} — {ahead.DriverName} placed higher for winning their " +
                                  $"{decider.Round} race against {behind.DriverName}.");
                    }
                    else if (ahead.OpponentStrength > behind.OpponentStrength)
                    {
                        notes.Add($"{lead} — {ahead.DriverName} placed higher for beating the stronger drivers " +
                                  $"(the drivers they beat scored {Plain(ahead.OpponentStrength)} between them, " +
                                  $"against {Plain(behind.OpponentStrength)}).");
                    }
                    else
                    {
                        notes.Add($"{lead} — nothing separated them, so they are listed in entry order.");
                    }
                }
            }

            return notes;
        }

        /// <summary>Who beat whom, and in which round, for the head-to-head tiebreak.</summary>
        private static Dictionary<(int, int), (int WinnerId, string Round)> BuildHeadToHead(
            RaceResultsArchive archive)
        {
            var map = new Dictionary<(int, int), (int, string)>();

            foreach (var m in RoundRobinMatches(archive))
            {
                if (IsBye(m) || m.WinnerDriverId == null || m.LoserDriverId == null) continue;
                map[Pair(m.WinnerDriverId.Value, m.LoserDriverId.Value)] =
                    (m.WinnerDriverId.Value, DisplayRound(RoundLabels.Normalize(m.RoundLabel ?? "")));
            }

            return map;
        }

        private static (int, int) Pair(int a, int b) => a < b ? (a, b) : (b, a);

        /// <summary>Every saved Round Robin match, ignoring the Finals and Losers phases.</summary>
        private static IEnumerable<RaceResultMatchSnapshot> RoundRobinMatches(RaceResultsArchive archive) =>
            (archive.Phases ?? new List<RacePhaseResultSnapshot>())
                .Where(p => p != null &&
                            string.Equals(p.Phase, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => p.Matches ?? new List<RaceResultMatchSnapshot>())
                .Where(m => m != null);

        /// <summary>Drops a trailing ".00" — nobody reads "4.00" as four.</summary>
        private static string Plain(double value) =>
            value == Math.Floor(value) ? value.ToString("0") : value.ToString("0.##");

        private static string Plural(int count, string one, string many = null) =>
            count == 1 ? one : (many ?? one + "s");

        /// <summary>
        /// Byes per driver, read back off the saved Round Robin matches — a bye is a
        /// match with only one driver in it, and the archive is the only place that
        /// survives to be counted later. The ranked standings carry the points a bye
        /// earned but not the tally itself.
        /// </summary>
        private static Dictionary<int, int> CountByes(RaceResultsArchive archive)
        {
            var byes = new Dictionary<int, int>();

            foreach (var match in RoundRobinMatches(archive))
            {
                if (!IsBye(match) || match.WinnerDriverId == null) continue;

                var id = match.WinnerDriverId.Value;
                byes[id] = byes.TryGetValue(id, out var n) ? n + 1 : 1;
            }

            return byes;
        }

        /// <summary>A match with nobody on one side of it.</summary>
        private static bool IsBye(RaceResultMatchSnapshot match) =>
            match.Driver1Id == null || match.Driver2Id == null ||
            IsByeName(match.Driver1Name) || IsByeName(match.Driver2Name);

        private static bool IsByeName(string name) =>
            string.IsNullOrWhiteSpace(name) ||
            string.Equals(name.Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        private static string BuildSummary(RaceResultsArchive archive)
        {
            if (!string.IsNullOrWhiteSpace(archive.ChampionName))
            {
                var runner = string.IsNullOrWhiteSpace(archive.RunnerUpName)
                    ? ""
                    : $"  •  Runner-up: {archive.RunnerUpName}";
                return $"Champion: {archive.ChampionName}{runner}";
            }
            return "Race in progress — saved results shown below.";
        }

        private static string SeededName(int? seed, string name)
        {
            var display = string.IsNullOrWhiteSpace(name) ? "BYE" : name;
            return seed.HasValue ? $"{seed.Value}. {display}" : display;
        }

        private static string DisplayPhase(string phase)
        {
            if (string.Equals(phase, RaceTypes.Finals, StringComparison.OrdinalIgnoreCase))
                return "Finals — Pro Ladder";
            return string.IsNullOrWhiteSpace(phase) ? "Race" : phase;
        }

        private static string DisplayRound(string round)
        {
            var normalized = RoundLabels.Normalize(round);
            if (string.Equals(normalized, "SF", StringComparison.OrdinalIgnoreCase))
                return "Semi-Final";
            if (string.Equals(normalized, "F", StringComparison.OrdinalIgnoreCase))
                return "Final";
            if (string.Equals(normalized, "LB-F", StringComparison.OrdinalIgnoreCase))
                return "Losers Bracket Final";
            if (normalized.StartsWith("RR", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(normalized.Substring(2), out var rr))
                return $"Round Robin {rr}";
            if (normalized.StartsWith("LB-R", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(normalized.Substring(4), out var lb))
                return $"Losers Bracket Round {lb}";
            if (normalized.StartsWith("R", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(normalized.Substring(1), out var r))
                return $"Round {r}";
            return normalized;
        }
    }
}
