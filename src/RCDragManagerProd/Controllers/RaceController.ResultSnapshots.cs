using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        private void CaptureCurrentResultSnapshot()
        {
            if (_session == null || _engine == null) return;

            string phase;
            if (string.Equals(_session.RaceType, RaceTypes.Finals, StringComparison.OrdinalIgnoreCase))
                phase = RaceTypes.Finals;
            else if (_inLosersPhase ||
                     string.Equals(_session.RaceType, RaceTypes.LosersBracket, StringComparison.OrdinalIgnoreCase))
                phase = RaceTypes.LosersBracket;
            else if (_engine is RoundRobinEngineAdapter)
                phase = RaceTypes.RoundRobin;
            else
                phase = string.IsNullOrWhiteSpace(_session.OriginalRaceType)
                    ? (_session.RaceType ?? "Main")
                    : _session.OriginalRaceType;

            CapturePhaseSnapshot(phase, EngineGetMatches(_engine));
        }

        private void CaptureRoundRobinResultSnapshot(RoundRobinEngineAdapter rr)
        {
            if (rr == null) return;

            var matches = EngineGetMatches(rr).ToList();
            CapturePhaseSnapshot(RaceTypes.RoundRobin, matches);

            var standings = rr.GetRankedStandings();
            var driverById = (_session.Drivers ?? new List<Driver>())
                .Where(d => d != null)
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First());

            EnsureResultsArchive();
            _session.ResultsArchive.RoundRobinStandings = standings
                .Select(row => new RoundRobinStandingSnapshot
                {
                    Rank = row.Rank,
                    DriverId = row.DriverId,
                    DriverName = driverById.TryGetValue(row.DriverId, out var driver)
                        ? driver.Name
                        : row.DriverId.ToString(),
                    Wins = row.Wins,
                    Losses = row.Losses,
                    Points = row.Points,
                    OpponentStrength = row.OpponentStrength
                })
                .ToList();
        }

        private void CapturePhaseSnapshot(string phase, IEnumerable<EngineMatch> matches)
        {
            if (_session == null || matches == null || string.IsNullOrWhiteSpace(phase)) return;

            EnsureResultsArchive();
            var entryById = (_session.DriverEntries ?? new List<RaceSessionDriverEntry>())
                .Where(e => e != null)
                .GroupBy(e => e.DriverID)
                .ToDictionary(g => g.Key, g => g.First());

            var snapshot = new RacePhaseResultSnapshot
            {
                Phase = phase,
                CapturedAt = DateTime.Now,
                Matches = matches
                    .OrderBy(m => RoundLabels.CompareKey(m.RoundLabel ?? string.Empty))
                    .ThenBy(m => m.MatchId)
                    .Select(m =>
                    {
                        var winner = m.HasResult ? _matchResult.GetWinner(m.MatchId) : null;
                        var loser = m.HasResult ? _matchResult.GetLoser(m.MatchId) : null;
                        return new RaceResultMatchSnapshot
                        {
                            MatchId = m.MatchId,
                            RoundLabel = m.RoundLabel,
                            Driver1Id = m.Driver1?.Id,
                            Driver1Name = m.Driver1?.Name ?? "BYE",
                            Driver2Id = m.Driver2?.Id,
                            Driver2Name = m.Driver2?.Name ?? "BYE",
                            Driver1Seed = SeedFor(m.Driver1, entryById),
                            Driver2Seed = SeedFor(m.Driver2, entryById),
                            FromMatch1 = m.FromMatch1,
                            FromMatch2 = m.FromMatch2,
                            WinnerDriverId = winner?.Id,
                            WinnerName = winner?.Name,
                            LoserDriverId = loser?.Id,
                            LoserName = loser?.Name
                        };
                    })
                    .ToList()
            };

            int existingIndex = _session.ResultsArchive.Phases.FindIndex(p =>
                string.Equals(p?.Phase, phase, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
                _session.ResultsArchive.Phases[existingIndex] = snapshot;
            else
                _session.ResultsArchive.Phases.Add(snapshot);
        }

        private void CaptureCompletedResult(Driver champion, Driver runnerUp, DateTime completedAt)
        {
            EnsureResultsArchive();
            _session.ResultsArchive.ChampionDriverId = champion?.Id;
            _session.ResultsArchive.ChampionName = champion?.Name;
            _session.ResultsArchive.RunnerUpDriverId = runnerUp?.Id;
            _session.ResultsArchive.RunnerUpName = runnerUp?.Name;
            _session.ResultsArchive.CompletedAt = completedAt;
        }

        private void EnsureResultsArchive()
        {
            if (_session.ResultsArchive == null)
                _session.ResultsArchive = new RaceResultsArchive();
            if (_session.ResultsArchive.Phases == null)
                _session.ResultsArchive.Phases = new List<RacePhaseResultSnapshot>();
            if (_session.ResultsArchive.RoundRobinStandings == null)
                _session.ResultsArchive.RoundRobinStandings = new List<RoundRobinStandingSnapshot>();
        }

        private static int? SeedFor(
            Driver driver,
            IReadOnlyDictionary<int, RaceSessionDriverEntry> entryById)
        {
            if (driver == null) return null;
            return entryById.TryGetValue(driver.Id, out var entry) ? entry.Seed : null;
        }
    }
}
