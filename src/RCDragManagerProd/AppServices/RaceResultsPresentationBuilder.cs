using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
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
            result.Standings = (archive.RoundRobinStandings ?? new List<RoundRobinStandingSnapshot>())
                .OrderBy(s => s.Rank)
                .Select(s => new RaceResultsStandingRow
                {
                    Rank = s.Rank,
                    Driver = s.DriverName,
                    Wins = s.Wins,
                    Losses = s.Losses,
                    Byes = byesByDriver.TryGetValue(s.DriverId, out var byes) ? byes : 0,
                    Points = s.Points.ToString("0.00"),
                    OpponentStrength = s.OpponentStrength.ToString("0.00")
                })
                .ToList();

            result.HasRoundRobinStandings = result.Standings.Count > 0;
            result.ScoringNote = ScoringNote;
            result.HasWinner = !string.IsNullOrWhiteSpace(archive.ChampionName);
            result.HasResults = result.Phases.Count > 0 || result.HasRoundRobinStandings;
            if (!result.HasResults && string.IsNullOrWhiteSpace(result.Summary))
                result.Summary = "No saved race results are available.";
            return result;
        }

        /// <summary>
        /// How the numbers in the standings table are made. Kept in step with
        /// <c>RoundRobinRanker</c>, which is the only thing that actually decides rank
        /// and the Finals seeding order.
        /// </summary>
        private const string ScoringNote =
            "Points: a win scores 4, a bye 2, a loss 1 — every round is worth the same. " +
            "Opponent strength is the total points of the drivers someone actually raced, " +
            "so it rewards a hard draw; byes are not counted in it. " +
            "Level on points, the higher place goes to more wins, then to the winner of " +
            "their head-to-head race, then to the stronger opponents.";

        /// <summary>
        /// Byes per driver, read back off the saved Round Robin matches — a bye is a
        /// match with only one driver in it, and the archive is the only place that
        /// survives to be counted later. The ranked standings carry the points a bye
        /// earned but not the tally itself.
        /// </summary>
        private static Dictionary<int, int> CountByes(RaceResultsArchive archive)
        {
            var byes = new Dictionary<int, int>();

            var roundRobinMatches = (archive.Phases ?? new List<RacePhaseResultSnapshot>())
                .Where(p => p != null &&
                            string.Equals(p.Phase, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => p.Matches ?? new List<RaceResultMatchSnapshot>())
                .Where(m => m != null);

            foreach (var match in roundRobinMatches)
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
