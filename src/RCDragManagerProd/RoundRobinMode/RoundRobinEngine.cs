using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RCDragManagerProd.Domain;
using LadderMatch = RCDragManagerProd.Domain.ProLadder.LadderMatch;

namespace RCDragManagerProd.RoundRobinMode
{
    public class RoundRobinEngine
    {
        private MatchResult results = new MatchResult();
        private int matchIdCounter = 1;
        private List<Driver> drivers = new List<Driver>();
        private readonly List<(Driver Driver1, Driver Driver2, string RoundLabel, int MatchId)> matches = new();

        // Thread-safe RNG for shuffle/offset
        private static readonly object _rngLock = new object();
        private static readonly Random _rng = new Random(unchecked(Environment.TickCount * 397 ^ System.Threading.Thread.CurrentThread.ManagedThreadId));

        // --------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------
        public void LoadDrivers(List<Driver> inputDrivers)
        {
            // RR should not rely on Seed; keep the user's picked order
            drivers = (inputDrivers ?? new List<Driver>()).Where(d => d != null).ToList();
        }

        /// <summary>
        /// Round Robin (circle method), capped at 3 rounds.
        /// - Shuffles roster each run so the first-picked driver does NOT always get the Round-1 BYE.
        /// - Odd N: add BYE (null) and apply a random pre-rotation so BYE recipient and match position move.
        /// - Logs roster (picked), roster after shuffle, pre-rotation, R1 snapshot, per-round pairings/BYEs, and summary.
        /// </summary>
        public void GenerateMatches()
        {
            matches.Clear();
            results = new MatchResult();
            matchIdCounter = 1;

            if (drivers.Count == 0) return;

            Log("================================= [RR] Build =================================");
            Log($"[RR] Picked order ({drivers.Count}): " + string.Join(" | ", drivers.Select(SafeName)));

            // Work copy
            var roster = new List<Driver>(drivers);

            // Shuffle roster every run (better randomness than "first picked gets BYE")
            Shuffle(roster);
            Log("[RR] Shuffled: " + string.Join(" | ", roster.Select(SafeName)));

            bool isOdd = (roster.Count % 2 != 0);
            if (isOdd)
            {
                // Null marks BYE; UI uses Driver2=null
                roster.Add(null);
            }

            int n = roster.Count;                 // even
            int totalRounds = Math.Min(3, n - 1); // hard cap at 3
            int pairingsPerRound = n / 2;

            // Random pre-rotation to move BYE position (and match order) in Round 1
            int applied = 0;
            if (n > 2)
            {
                applied = NextInt(n - 1); // 0..(n-2)
                for (int i = 0; i < applied; i++)
                    RotateRightKeepingPivot(roster);
            }
            Log($"[RR] Pre-rotation applied: {applied} (span={Math.Max(0, n - 1)})");
            Log("[RR] R1 ring snapshot: " + string.Join(" | ", roster.Select(SafeName)));
            Log($"[RR] Rounds={totalRounds}, Pairs/Round={pairingsPerRound}, Odd={(isOdd ? "YES" : "NO")}");

            // Tracking
            var driver1Count = new Dictionary<int, int>();
            var byeCount = new Dictionary<int, int>();
            foreach (var d in drivers)
            {
                driver1Count[d.Id] = 0;
                if (isOdd) byeCount[d.Id] = 0;
            }

            var seenPairs = new HashSet<(int, int)>(); // sanity check within 3 rounds

            for (int round = 1; round <= totalRounds; round++)
            {
                Log($"[RR] ---- Round {round} ----");

                var roundPairs = new List<(Driver A, Driver B)>(pairingsPerRound);
                for (int i = 0; i < pairingsPerRound; i++)
                {
                    var d1 = roster[i];
                    var d2 = roster[n - 1 - i];
                    roundPairs.Add((d1, d2));
                }

                // Balance Driver1 appearances
                bool flipForFairness = (round % 2 == 1);
                if (flipForFairness)
                {
                    for (int i = 0; i < roundPairs.Count; i++)
                        roundPairs[i] = (roundPairs[i].B, roundPairs[i].A);
                }

                int matchNoInRound = 1;
                foreach (var (A, B) in roundPairs)
                {
                    bool isByePair = (A == null || B == null);

                    if (isByePair)
                    {
                        var real = A ?? B;
                        matches.Add((real, null, $"R{round}", matchIdCounter++));
                        if (real != null && isOdd && byeCount.ContainsKey(real.Id))
                            byeCount[real.Id] = byeCount[real.Id] + 1;

                        Log($"[RR]   M{matchIdCounter - 1:000}: {SafeName(real)} vs BYE");
                        Log($"[RR][BYE] Round {round}: {SafeName(real)}");
                    }
                    else
                    {
                        var d1Out = A;
                        var d2Out = B;

                        if (d1Out != null && driver1Count.ContainsKey(d1Out.Id))
                            driver1Count[d1Out.Id] = driver1Count[d1Out.Id] + 1;

                        int a = d1Out.Id;
                        int b = d2Out.Id;
                        var key = a < b ? (a, b) : (b, a);
                        if (!seenPairs.Add(key))
                            Log("[RR][WARN] Duplicate head-to-head within capped schedule: " + SafeName(d1Out) + " vs " + SafeName(d2Out));

                        matches.Add((d1Out, d2Out, $"R{round}", matchIdCounter++));
                        Log($"[RR]   M{matchIdCounter - 1:000}: {SafeName(d1Out)} vs {SafeName(d2Out)}");
                    }

                    matchNoInRound++;
                }

                // Next round rotation
                RotateRightKeepingPivot(roster);
            }

            if (isOdd)
            {
                Log("[RR] ---- BYE distribution (capped 3 rounds) ----");
                foreach (var d in drivers)
                    Log($"[RR][BYE] {SafeName(d)} → {byeCount[d.Id]} BYE(s)");
            }

            Log("[RR] ---- Driver1 distribution ----");
            foreach (var d in drivers)
                Log($"[RR][D1]  {SafeName(d)} → {driver1Count[d.Id]}");

            Log("================================ [RR] Build Done ================================");
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

        public List<RoundRobinMatch> GetResults()
        {
            return matches
                .Where(m => results.HasResult(m.MatchId))
                .Select(m => new RoundRobinMatch
                {
                    MatchId = m.MatchId,
                    RoundLabel = m.RoundLabel,
                    Driver1 = m.Driver1,
                    Driver2 = m.Driver2
                })
                .ToList();
        }

        public void Reset()
        {
            matches.Clear();
            results = new MatchResult();
            matchIdCounter = 1;
            drivers.Clear();
        }

        public IReadOnlyList<string> GetRoundLabels()
        {
            return matches.Select(m => m.RoundLabel)
                          .Distinct()
                          .ToList();
        }

        public List<(Driver Driver, int Wins)> GetStandings()
        {
            return matches
                .Where(m => results.HasResult(m.MatchId))
                .Select(m => results.GetWinner(m.MatchId))
                .GroupBy(d => d)
                .Select(g => (Driver: g.Key, Wins: g.Count()))
                .OrderByDescending(x => x.Wins)
                .ToList();
        }

        public List<Driver> GetAllDrivers()
        {
            return new List<Driver>(drivers);
        }

        public List<Driver> GetTopN(int count)
        {
            return GetStandings()
                .OrderByDescending(s => s.Wins)
                .Take(count)
                .Select(s => s.Driver)
                .ToList();
        }

        public List<Driver> GetTopRankedDrivers(int count)
        {
            var ranker = new RoundRobinRanker();
            var ranked = ranker.Rank(GetResults(), drivers, results);

            return ranked.Take(count)
                         .Select(r => drivers.First(d => d.Id == r.DriverId))
                         .ToList();
        }

        // --------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------
        private static void RotateRightKeepingPivot(List<Driver> ring)
        {
            if (ring == null || ring.Count <= 2) return;
            int last = ring.Count - 1;
            var temp = ring[last];
            for (int i = last; i >= 2; i--)
                ring[i] = ring[i - 1];
            ring[1] = temp;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            if (list == null || list.Count < 2) return;
            lock (_rngLock)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    var tmp = list[i];
                    list[i] = list[j];
                    list[j] = tmp;
                }
            }
        }

        private static int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            lock (_rngLock)
            {
                return _rng.Next(maxExclusive); // 0..maxExclusive-1
            }
        }

        private static string SafeName(Driver d)
        {
            if (d == null) return "BYE";
            if (!string.IsNullOrWhiteSpace(d.Name)) return d.Name;
            return d.Id.ToString();
        }

        private static void Log(string message)
        {
            try
            {
                RCDragManagerProd.Logging.Logger.Log(message);
            }
            catch
            {
                try { Debug.WriteLine(message); } catch { /* ignore */ }
            }
        }
    }
}
