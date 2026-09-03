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
                        Points = s.Points.ToString("0.00"),
                        PointsWorking = DescribePoints(s.Wins, byes, s.Losses),
                        OpponentStrength = s.OpponentStrength.ToString("0.00")
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
            "Most points wins the class. Level on points? Most wins takes it, then " +
            "whoever won when those two raced each other.";

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
                    Result = "Win",
                    Points = Plain(pts.Win),
                    Why = "you beat the driver in the other lane"
                },
                new ScoringLegendRow
                {
                    Result = "Bye",
                    Points = Plain(pts.Bye),
                    Why = "the draw left you with nobody to race — worth more than a loss " +
                          "because it was not your doing, less than a win because you beat nobody"
                },
                new ScoringLegendRow
                {
                    Result = "Loss",
                    Points = Plain(pts.Loss),
                    Why = "you raced and lost — still scores, because you ran the round"
                }
            };
        }

        /// <summary>Writes a driver's points out as a sum: "2 wins (8) + 1 bye (2)".</summary>
        private static string DescribePoints(int wins, int byes, int losses)
        {
            var pts = RoundRobinRanker.PointsForRound("RR1");
            var parts = new List<string>();

            if (wins > 0) parts.Add($"{wins} {Plural(wins, "win")} ({Plain(wins * pts.Win)})");
            if (byes > 0) parts.Add($"{byes} {Plural(byes, "bye")} ({Plain(byes * pts.Bye)})");
            if (losses > 0) parts.Add($"{losses} {Plural(losses, "loss", "losses")} ({Plain(losses * pts.Loss)})");

            return parts.Count == 0 ? "No races yet" : string.Join(" + ", parts);
        }

        /// <summary>
        /// A sentence for each pair of drivers who finished level on points, naming the
        /// rule that separated them. This is the only place opponent strength appears —
        /// as a bare number in a column it told a race director nothing.
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
                        notes.Add($"{lead} — {ahead.DriverName} placed higher for racing the stronger field " +
                                  $"(opponents totalling {Plain(ahead.OpponentStrength)} against " +
                                  $"{Plain(behind.OpponentStrength)}).");
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
