using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.AppServices
{
    public static class ClassCompletionPresentationBuilder
    {
        public static ClassCompletionPresentation Build(RaceSession session)
        {
            var archive = session?.ResultsArchive ?? new RaceResultsArchive();
            var standings = (archive.RoundRobinStandings ?? new List<RoundRobinStandingSnapshot>())
                .OrderBy(s => s.Rank)
                .ToList();

            var result = new ClassCompletionPresentation
            {
                EventName = string.IsNullOrWhiteSpace(session?.EventName) ? "Race complete" : session.EventName,
                ClassName = string.IsNullOrWhiteSpace(session?.ClassType) ? "Class results" : session.ClassType,
                ChampionName = archive.ChampionName ?? "Winner not recorded",
                RunnerUpName = archive.RunnerUpName ?? "Runner-up not recorded"
            };

            var podiumIds = new HashSet<int>();
            if (archive.ChampionDriverId.HasValue) podiumIds.Add(archive.ChampionDriverId.Value);
            if (archive.RunnerUpDriverId.HasValue) podiumIds.Add(archive.RunnerUpDriverId.Value);

            var third = standings.FirstOrDefault(s => !podiumIds.Contains(s.DriverId));
            if (third != null)
            {
                result.ThirdLabel = "3rd place";
                result.ThirdName = third.DriverName;
                result.HasThird = true;
                podiumIds.Add(third.DriverId);
            }
            else
            {
                var semiFinalists = FindSemiFinalLosers(archive)
                    .Where(n => !string.Equals(n, archive.ChampionName, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(n, archive.RunnerUpName, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (semiFinalists.Count > 0)
                {
                    result.ThirdLabel = semiFinalists.Count == 1 ? "3rd place" : "Semi-finalists";
                    result.ThirdName = string.Join("  •  ", semiFinalists);
                    result.HasThird = true;
                }
            }

            if (!result.HasThird)
            {
                result.ThirdLabel = "Next finisher";
                result.ThirdName = "Not recorded";
            }

            result.OtherFinishers = standings
                .Where(s => !podiumIds.Contains(s.DriverId))
                .Select(s => new RaceResultsStandingRow
                {
                    Rank = s.Rank,
                    Driver = s.DriverName,
                    Wins = s.Wins,
                    Losses = s.Losses,
                    Points = s.Points.ToString("0.00"),
                    OpponentStrength = s.OpponentStrength.ToString("0.00")
                })
                .ToList();

            result.FinalsResults = RaceResultsPresentationBuilder.Build(session).ResultRows
                .Where(r => string.Equals(r.Phase, "Finals — Pro Ladder", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return result;
        }

        private static IEnumerable<string> FindSemiFinalLosers(RaceResultsArchive archive) =>
            (archive.Phases ?? new List<RacePhaseResultSnapshot>())
                .Where(p => string.Equals(p?.Phase, RaceTypes.Finals, StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => p.Matches ?? new List<RaceResultMatchSnapshot>())
                .Where(m => string.Equals(RoundLabels.Normalize(m.RoundLabel), "SF", StringComparison.OrdinalIgnoreCase))
                .Select(m => m.LoserName)
                .Where(n => !string.IsNullOrWhiteSpace(n));
    }
}
