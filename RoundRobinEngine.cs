using System;
using System.Collections.Generic;
using System.Linq;
using static RCDragManagerProd.ProLadder;

namespace RCDragManagerProd
{
    public class RoundRobinEngine
    {
        private MatchResult results = new MatchResult();
        private int matchIdCounter = 1;
        private List<Driver> drivers = new List<Driver>();
        private readonly List<(Driver Driver1, Driver Driver2, string RoundLabel, int MatchId)> matches = new();

        // --------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------
        public void LoadDrivers(List<Driver> inputDrivers)
        {
            drivers = inputDrivers.OrderBy(d => d.Seed).ToList();
        }

        /// <summary>
        /// Generates 3 rounds of pairings with
        ///   • no rematches,
        ///   • at most one BYE per round.
        /// Uses the classic “circle method” so every driver
        /// appears exactly once each round.
        /// </summary>
        public void GenerateMatches()
        {
            matches.Clear();
            results = new MatchResult();
            matchIdCounter = 1;

            int desiredRounds = 3;
            if (drivers.Count == 0) return;

            // Copy list so we can rotate it
            var roster = new List<Driver>(drivers);

            // If odd, add null placeholder to create BYEs
            if (roster.Count % 2 != 0) roster.Add(null);

            int n = roster.Count;        // even count now
            int totalRounds = Math.Min(desiredRounds, n - 1);

            for (int round = 0; round < totalRounds; round++)
            {
                // First element stays fixed; rotate the rest
                for (int i = 0; i < n / 2; i++)
                {
                    var d1 = roster[i];
                    var d2 = roster[n - 1 - i];

                    // Handle BYE (null) slot
                    if (d1 == null || d2 == null)
                    {
                        var realDriver = d1 ?? d2;          // whichever is not null
                        matches.Add((realDriver, null, $"R{round + 1}", matchIdCounter++));
                    }
                    else
                    {
                        matches.Add((d1, d2, $"R{round + 1}", matchIdCounter++));
                    }
                }

                // Rotate (keep index 0 fixed)
                var last = roster[n - 1];
                roster.RemoveAt(n - 1);
                roster.Insert(1, last);
            }
        }

        public List<(int MatchId, Driver D1, Driver D2, string RoundLabel)> GetMatches()
        {
            return matches.Select(m => (m.MatchId, m.Driver1, m.Driver2, m.RoundLabel))
                          .ToList();
        }

        public void SetWinner(int matchId, Driver winner, Driver loser = null)
            => results.SetWinner(matchId, winner, loser);

        public bool HasWinner(int matchId) => results.HasResult(matchId);
        public Driver GetWinner(int matchId) => results.GetWinner(matchId);
        public Driver GetLoser(int matchId) => results.GetLoser(matchId);

        public (Driver, Driver) ResolveDrivers(LadderMatch match)
        {
            var d1 = drivers.FirstOrDefault(d => d.Seed == match.Seed1);
            var d2 = drivers.FirstOrDefault(d => d.Seed == match.Seed2);

            if (d1 != null && d2 == null) d2 = new Driver { Name = "BYE" };
            if (d2 != null && d1 == null) d1 = new Driver { Name = "BYE" };

            return (d1, d2);
        }

        public bool IsTournamentComplete()
        {
            return matches.All(m => results.HasResult(m.MatchId));
        }
    }
}
