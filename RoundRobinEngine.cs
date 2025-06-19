// ============================================================================
// RoundRobinEngine.cs
// RC Drag Manager — Round-Robin Race Engine  (MVP v1.0)
// ============================================================================
//
// Responsibilities
// • Build three rounds of random pairings (one BYE maximum per round)
// • Guarantee no repeat match-ups across all rounds
// • Track every pairing and result via DriverMatchResult records
// • Expose a clean, unit-testable API for driving the race flow from Form1
//
// NOTE:  This file is 100 % standalone and does not touch any UI code.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public class RoundRobinEngine
    {
        // ---------- Public DTOs ------------------------------------------------

        public class MatchPair
        {
            public Guid? DriverAId { get; init; }
            public Guid? DriverBId { get; init; }          // null = BYE
            public Guid? WinnerId { get; set; }            // null until decided
        }

        public class DriverMatchResult
        {
            public int Round { get; init; }
            public Guid DriverAId { get; init; }
            public Guid? DriverBId { get; init; }          // null = BYE
            public Guid? WinnerId { get; set; }
            public bool IsBye => DriverBId == null;
        }

        // ---------- State ------------------------------------------------------

        private readonly List<Guid> _allDrivers;
        private readonly Random _rng = new();
        private readonly HashSet<(Guid, Guid)> _pairingHistory = new();
        private readonly List<List<MatchPair>> _rounds = new();
        private readonly List<DriverMatchResult> _results = new();
        private int _currentRoundIndex = -1;

        // ----------- Ctor ------------------------------------------------------

        public RoundRobinEngine(IEnumerable<Guid> driverIds)
        {
            _allDrivers = driverIds?.Distinct().ToList()
                          ?? throw new ArgumentNullException(nameof(driverIds));

            if (_allDrivers.Count < 3)
                throw new InvalidOperationException("Round-robin requires at least 3 drivers.");
        }

        // ----------- Public API ------------------------------------------------

        public int CurrentRound => _currentRoundIndex + 1;               // 0-based -> human-based
        public IReadOnlyList<DriverMatchResult> AllResults => _results;

        public IReadOnlyList<MatchPair> GetCurrentRoundMatches()
        {
            EnsureRoundGenerated();
            return _rounds[_currentRoundIndex];
        }

        public void SetWinner(Guid matchIdDriverA, Guid? matchIdDriverB, Guid winnerId)
        {
            var roundMatches = GetCurrentRoundMatches();
            var match = roundMatches
                        .FirstOrDefault(m => m.DriverAId == matchIdDriverA &&
                                             m.DriverBId == matchIdDriverB);

            if (match == null)
                throw new InvalidOperationException("Match not found in current round.");

            if (match.WinnerId != null)
                throw new InvalidOperationException("Winner already set.");

            match.WinnerId = winnerId;

            // persist result
            _results.First(r =>
                   r.Round == CurrentRound &&
                   r.DriverAId == matchIdDriverA &&
                   r.DriverBId == matchIdDriverB)
                   .WinnerId = winnerId;
        }

        public bool IsCurrentRoundComplete() =>
            GetCurrentRoundMatches().All(m => m.WinnerId != null || m.IsBye);

        public void AdvanceRound()
        {
            if (!IsCurrentRoundComplete())
                throw new InvalidOperationException("Cannot advance: round not finished.");

            if (_currentRoundIndex == 2)
                return; // three rounds max

            GenerateNextRound();
        }

        public bool IsTournamentComplete() =>
            _currentRoundIndex == 2 && IsCurrentRoundComplete();

        // ----------- Round Generation -----------------------------------------

        private void EnsureRoundGenerated()
        {
            if (_currentRoundIndex == -1)
                GenerateNextRound();
        }

        private void GenerateNextRound()
        {
            _currentRoundIndex++;

            var activeDrivers = _allDrivers.ToList();

            // mark BYE driver if we have odd count
            Guid? byeDriver = null;
            if (activeDrivers.Count % 2 == 1)
            {
                byeDriver = activeDrivers[_rng.Next(activeDrivers.Count)];
                activeDrivers.Remove(byeDriver.Value);
            }

            // shuffle to random order to start pairing
            activeDrivers = activeDrivers.OrderBy(_ => _rng.Next()).ToList();

            var roundPairings = new List<MatchPair>();

            // simple greedy pairing avoiding rematches
            while (activeDrivers.Any())
            {
                var a = activeDrivers[0];
                activeDrivers.RemoveAt(0);

                Guid opponent = default;
                int opponentIndex = -1;

                for (int i = 0; i < activeDrivers.Count; i++)
                {
                    var b = activeDrivers[i];
                    if (!_pairingHistory.Contains(NormalizePair(a, b)))
                    {
                        opponent = b;
                        opponentIndex = i;
                        break;
                    }
                }

                // if every remaining opponent is a repeat, take first (forced repeat)
                if (opponentIndex == -1)
                {
                    opponentIndex = 0;
                    opponent = activeDrivers[0];
                }

                activeDrivers.RemoveAt(opponentIndex);

                roundPairings.Add(new MatchPair
                {
                    DriverAId = a,
                    DriverBId = opponent
                });

                _pairingHistory.Add(NormalizePair(a, opponent));
            }

            // add BYE match (DriverB == null)
            if (byeDriver != null)
            {
                roundPairings.Add(new MatchPair
                {
                    DriverAId = byeDriver,
                    DriverBId = null,
                    WinnerId = byeDriver                // auto-advance
                });
            }

            _rounds.Add(roundPairings);

            // prime result list
            foreach (var p in roundPairings)
            {
                _results.Add(new DriverMatchResult
                {
                    Round = CurrentRound,
                    DriverAId = p.DriverAId!.Value,
                    DriverBId = p.DriverBId,
                    WinnerId = p.WinnerId
                });
            }
        }

        private static (Guid, Guid) NormalizePair(Guid a, Guid b) =>
            a.CompareTo(b) < 0 ? (a, b) : (b, a);
    }
}
