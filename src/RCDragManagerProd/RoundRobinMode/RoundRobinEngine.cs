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
        // Requested RR rounds (default = 3). Engine will clamp to max possible (n - 1).
        public int RoundsToRunRequested { get; set; } = 3;


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
        /// Round Robin (circle method), variable rounds (clamped to n - 1).
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

            int n = roster.Count; // even (after BYE insertion if needed)

            int maxPossibleRounds = Math.Max(0, n - 1);
            int requested = RoundsToRunRequested;
            if (requested <= 0) requested = 3;

            // QMDRA requirement: run exactly N rounds, even if N > (n - 1).
            // After (n - 1) rounds, rematches are unavoidable and expected.
            int totalRounds = requested;

            int pairingsPerRound = n / 2;

            Log($"[RR] Rounds requested={requested}, maxPossible={maxPossibleRounds}, totalRounds={totalRounds}");



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
            Log($"[RR] Rounds requested={requested}, totalRounds={totalRounds}, maxPossible={maxPossibleRounds}, Pairs/Round={pairingsPerRound}, Odd={(isOdd ? "YES" : "NO")}");
            if (totalRounds > maxPossibleRounds)
                Log($"[RR][QMDRA] Overschedule active → N={totalRounds} exceeds max unique rounds {maxPossibleRounds}. Rematches will occur after round {maxPossibleRounds}.");


            // Tracking
            var driver1Count = new Dictionary<int, int>();
            var byeCount = new Dictionary<int, int>();
            foreach (var d in drivers)
            {
                driver1Count[d.Id] = 0;
                if (isOdd) byeCount[d.Id] = 0;
            }

            var seenPairs = new HashSet<(int, int)>(); // sanity check within a single full RR cycle


            for (int round = 1; round <= totalRounds; round++)
            {
                Log($"[RR] ---- Round {round} ----");

                // After one full cycle (n-1 rounds), reset seenPairs because rematches are now expected (QMDRA overschedule).
                if (maxPossibleRounds > 0 && round > 1 && ((round - 1) % maxPossibleRounds) == 0)
                {
                    seenPairs.Clear();
                    Log($"[RR][QMDRA] New cycle starting at round {round} → resetting duplicate-pair warnings.");
                }


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

                foreach (var (A, B) in roundPairs)
                {
                    bool isByePair = (A == null || B == null);

                    if (isByePair)
                    {
                        var real = A ?? B;
                        matches.Add((real, null, $"RR{round}", matchIdCounter++));
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
                            Log("[RR][WARN] Duplicate head-to-head within current cycle: " + SafeName(d1Out) + " vs " + SafeName(d2Out));


                        matches.Add((d1Out, d2Out, $"RR{round}", matchIdCounter++));
                        Log($"[RR]   M{matchIdCounter - 1:000}: {SafeName(d1Out)} vs {SafeName(d2Out)}");
                    }
                }

                // Next round rotation
                RotateRightKeepingPivot(roster);
            }

            if (isOdd)
            {
                Log($"[RR] ---- BYE distribution (rounds={totalRounds}) ----");

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

        /// <summary>
        /// Replaces the generated schedule with an explicit one. Required for resume:
        /// <see cref="GenerateMatches"/> shuffles and random-rotates the roster, so it
        /// cannot reproduce a previously-saved schedule — the saved pairings must be
        /// re-injected verbatim instead.
        /// </summary>
        public void InjectMatches(IEnumerable<(int MatchId, Driver D1, Driver D2, string RoundLabel)> injected)
        {
            matches.Clear();
            results = new MatchResult();

            int maxId = 0;
            foreach (var m in injected ?? Enumerable.Empty<(int, Driver, Driver, string)>())
            {
                matches.Add((m.D1, m.D2, m.RoundLabel, m.MatchId));
                if (m.MatchId > maxId) maxId = m.MatchId;
            }

            matchIdCounter = maxId + 1;
            Log($"[RR] InjectMatches: loaded {matches.Count} match(es); next id={matchIdCounter}.");
        }

        public bool TryGetMatch(int matchId, out Driver driver1, out Driver driver2, out string roundLabel)
        {
            var match = matches.FirstOrDefault(m => m.MatchId == matchId);
            if (match.MatchId == 0)
            {
                driver1 = null;
                driver2 = null;
                roundLabel = null;
                return false;
            }

            driver1 = match.Driver1;
            driver2 = match.Driver2;
            roundLabel = match.RoundLabel;
            return true;
        }

        public void SetWinner(int matchId, Driver winner, Driver loser = null)
        {
            if (winner == null)
            {
                results.SetWinner(matchId, winner, loser);
                return;
            }

            if (TryGetMatch(matchId, out var left, out var right, out _))
            {
                if (loser == null)
                {
                    if (left != null && left.Id == winner.Id) loser = right;
                    else if (right != null && right.Id == winner.Id) loser = left;
                }
            }

            results.SetWinner(matchId, winner, loser);
        }

        public bool HasWinner(int matchId) => results.HasResult(matchId);
        public Driver GetWinner(int matchId) => results.GetWinner(matchId);
        public Driver GetLoser(int matchId) => results.GetLoser(matchId);

        public (Driver, Driver) ResolveDrivers(LadderMatch match)
        {
            var d1 = drivers.FirstOrDefault(d => d.Seed == match.Seed1);
            var d2 = drivers.FirstOrDefault(d => d.Seed == match.Seed2);

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
                          .OrderBy(x => RoundLabels.CompareKey(x))
                          .ToList();
        }

        public List<(Driver Driver, int Wins)> GetStandings()
        {
            return matches
                .Where(m => results.HasResult(m.MatchId))
                .Select(m => results.GetWinner(m.MatchId))
                .Where(d => d != null)
                .GroupBy(d => d.Id)
                .Select(g => (Driver: g.First(), Wins: g.Count()))
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
            var ranked = GetRankedStandings();

            return ranked.Take(count)
                         .Select(r => drivers.FirstOrDefault(d => d.Id == r.DriverId))
                         .Where(d => d != null)
                         .ToList();
        }

        public List<DriverRankResult> GetRankedStandings()
        {
            var ranker = new RoundRobinRanker();
            return ranker.Rank(GetResults(), drivers, results);
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
