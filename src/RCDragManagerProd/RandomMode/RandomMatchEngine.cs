using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;   // if it talks to core engine types
using RCDragManagerProd.Logging;



namespace RCDragManagerProd.RandomMode
{
    /// <summary>
    /// Handles match storage and result resolution for blind-draw brackets
    /// (pairing / BYE logic is generated elsewhere and loaded via <see cref="LoadMatches"/>).
    /// </summary>
    public class RandomMatchEngine
    {
        // ── internal state ────────────────────────────────────────────────────
        private List<RandomMatch> bracketMatches = new List<RandomMatch>();
        private readonly MatchResult results = new MatchResult();
        private List<Driver> drivers = new List<Driver>();
        


        // ── round-data I/O ────────────────────────────────────────────────────
        public void LoadMatches(List<RandomMatch> matches)
        {
            bracketMatches = matches ?? new List<RandomMatch>();
        }

        public IReadOnlyList<RandomMatch> GetMatches() => bracketMatches;

        // ── winner / loser helpers ────────────────────────────────────────────
        public void SetWinner(int matchId, Driver winner)
        {
            Driver loser = GetLoserFromMatch(matchId, winner);
            Logger.Log($"[RND] Loser resolution: matchId={matchId}, winnerId={(winner != null ? winner.Id.ToString() : "null")}, loserId={(loser != null ? loser.Id.ToString() : "null")}");
            results.SetWinner(matchId, winner, loser);

            // 🔧 Patch downstream matches to replace null seeds
            foreach (var m in bracketMatches)
            {
                if (m.FromMatch1 == matchId && m.Seed1 == null)
                    m.Seed1 = winner;

                if (m.FromMatch2 == matchId && m.Seed2 == null)
                    m.Seed2 = winner;
            }
        }


        private Driver GetLoserFromMatch(int matchId, Driver winner)
        {
            var match = bracketMatches.FirstOrDefault(m => m.MatchId == matchId);
            if (match == null) return null;

            if (winner != null && match.Seed1 != null && match.Seed1.Id == winner.Id) return match.Seed2;
            if (winner != null && match.Seed2 != null && match.Seed2.Id == winner.Id) return match.Seed1;
            return null;
        }

        public Driver GetWinner(int matchId) => results.GetWinner(matchId);

        public bool HasWinner(int matchId) => results.HasResult(matchId);

        public Driver GetLoser(int matchId)
        {
            var match = bracketMatches.FirstOrDefault(m => m.MatchId == matchId);
            if (match == null) return null;

            var winner = results.GetWinner(matchId);
            if (winner == null) return null;

            if (match.Seed1 != null && match.Seed1.Id == winner.Id) return match.Seed2;
            if (match.Seed2 != null && match.Seed2.Id == winner.Id) return match.Seed1;
            return null;
        }

        // ── driver resolution helpers ────────────────────────────────────────
        public (Driver Driver1, Driver Driver2) ResolveDrivers(RandomMatch match)
        {
            // ✅ Already resolved
            if (results.HasResult(match.MatchId))
            {
                return (results.GetWinner(match.MatchId),
                        results.GetLoser(match.MatchId));
            }

            // ✅ Resolve from seeds / upstream
            Driver d1 = match.Seed1 ?? ResolveFrom(match.FromMatch1);
            Driver d2 = match.Seed2 ?? ResolveFrom(match.FromMatch2);

            // ✅ Inject BYE placeholder
            if (d1 != null && d2 == null) return (d1, new Driver { Name = "BYE" });
            if (d2 != null && d1 == null) return (new Driver { Name = "BYE" }, d2);

            // Up-stream matches unresolved
            return (d1, d2);
        }

        private Driver ResolveFrom(int? fromMatchId) =>
            fromMatchId.HasValue && results.HasResult(fromMatchId.Value)
                ? results.GetWinner(fromMatchId.Value)
                : null;

        // ── tournament utilities ─────────────────────────────────────────────
        public bool IsTournamentComplete()
        {
            RandomMatch final = bracketMatches.LastOrDefault();
            return final != null && results.HasResult(final.MatchId);
        }

        public void RewindToMatch(int matchId) => results.ClearFromMatch(matchId);

        public void LoadDrivers(List<Driver> newDrivers)
        {
            drivers = new List<Driver>(newDrivers ?? new List<Driver>());
        }


        public void GenerateBracket()
        {
            bracketMatches.Clear();

            int totalDrivers = drivers.Count;
            if (totalDrivers < 2) return;

            // 1️⃣ Shuffle drivers randomly
            var rnd = new Random();
            var shuffled = drivers.OrderBy(_ => rnd.Next()).ToList();

            // 2️⃣ Pad with BYE if odd
            if (shuffled.Count % 2 != 0)
                shuffled.Add(null);

            // 3️⃣ Create R1 matches
            var currentRound = new List<RandomMatch>();
            int matchId = 1;
            for (int i = 0; i < shuffled.Count; i += 2)
            {
                currentRound.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = shuffled[i],
                    Seed2 = shuffled[i + 1],
                    RoundLabel = "R1"
                });
            }

            bracketMatches.AddRange(currentRound);

            int roundNum = 2;
            while (currentRound.Count > 1)
            {
                var nextRound = new List<RandomMatch>();

                for (int i = 0; i < currentRound.Count; i += 2)
                {
                    var m1 = currentRound[i];
                    var m2 = (i + 1 < currentRound.Count) ? currentRound[i + 1] : null;

                    nextRound.Add(new RandomMatch
                    {
                        MatchId = matchId++,
                        FromMatch1 = m1.MatchId,
                        FromMatch2 = m2?.MatchId,
                        RoundLabel = $"R{roundNum}"
                    });
                }

                bracketMatches.AddRange(nextRound);
                currentRound = nextRound;
                roundNum++;
            }

            // Optional: label the final as "F"
            if (bracketMatches.Count > 0)
            {
                var final = bracketMatches.Last();
                final.RoundLabel = "F";
            }
        }



        public void Reset()
        {
            bracketMatches.Clear();
            drivers.Clear();
            results.Clear();  // ✅ Only works if MatchResult has Clear() defined
        }


        public IReadOnlyList<string> GetRoundOrder()
        {
            return bracketMatches.Select(m => m.RoundLabel).Distinct().ToList();
        }

    }


}
