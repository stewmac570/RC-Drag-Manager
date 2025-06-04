using RCDragManagerCleanDemo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManager
{
    public class MatchEngine
    {
        private List<Driver> allDrivers;
        private List<ProLadder.LadderMatch> bracketMatches;
        private Dictionary<int, Driver> seedMap;
        private Dictionary<int, Driver> matchWinners;
        private Dictionary<int, ProLadder.LadderMatch> matchMap;

        public MatchResult Results { get; private set; } = new MatchResult();

        public void Initialize(List<Driver> drivers)
        {
            allDrivers = drivers.OrderBy(d => d.QualTime).ToList();

            for (int i = 0; i < allDrivers.Count; i++)
            {
                allDrivers[i].Seed = i + 1;
            }

            bracketMatches = ProLadder.GetLadder(allDrivers.Count);
            matchMap = bracketMatches.ToDictionary(m => m.MatchId);
            seedMap = allDrivers
                .Where(d => d.Seed.HasValue)
                .ToDictionary(d => d.Seed.Value, d => d);

            matchWinners = new Dictionary<int, Driver>();

            RefreshBracketState();
        }

        public void RefreshBracketState()
        {
            foreach (var match in bracketMatches)
            {
                if (!Results.HasResult(match.MatchId))
                {
                    var (d1, d2) = ResolveDriversForMatch(match);
                    if (d1 != null && d2 != null)
                    {
                        if (d1.Name == "BYE" && d2.Name != "BYE")
                        {
                            Results.SetWinner(match.MatchId, d2);
                        }
                        else if (d2.Name == "BYE" && d1.Name != "BYE")
                        {
                            Results.SetWinner(match.MatchId, d1);
                        }
                    }
                }
            }
        }

        public void SetWinner(int matchId, Driver winner)
        {
            Results.SetWinner(matchId, winner);
            RefreshBracketState();
        }

        public IReadOnlyList<ProLadder.LadderMatch> GetBracketMatches()
        {
            return bracketMatches;
        }

        public IReadOnlyList<ProLadder.LadderMatch> GetCurrentRoundMatches()
        {
            string currentRound = GetNextPlayableRound();
            return bracketMatches.Where(m => m.RoundLabel == currentRound).ToList();
        }

        public bool IsCurrentRoundComplete()
        {
            var round = GetCurrentRoundMatches();
            return round.All(m => Results.IsMatchResolved(m.MatchId));
        }

        public bool IsTournamentComplete()
        {
            return Results.IsTournamentComplete(bracketMatches);
        }

        public void AdvanceToNextRound()
        {
            RefreshBracketState();
        }

        public string GetNextPlayableRound()
        {
            var roundOrder = new[] { "R1", "QF", "SF", "F" };

            foreach (var round in roundOrder)
            {
                var matchesInRound = bracketMatches.Where(m => m.RoundLabel == round).ToList();

                bool priorRoundsComplete = roundOrder
                    .TakeWhile(r => r != round)
                    .All(prior => bracketMatches
                        .Where(m => m.RoundLabel == prior)
                        .All(m => Results.IsMatchResolved(m.MatchId)));

                if (priorRoundsComplete)
                {
                    if (matchesInRound.Any(m => !Results.IsMatchResolved(m.MatchId)))
                        return round;
                }
            }

            return "COMPLETE";
        }

        public (Driver, Driver) ResolveDriversForMatch(ProLadder.LadderMatch match)
        {
            var d1 = ResolveDriver(match.Seed1, match.FromMatch1);
            var d2 = ResolveDriver(match.Seed2, match.FromMatch2);
            return (d1 ?? new Driver { Name = "TBD" }, d2 ?? new Driver { Name = "TBD" });
        }

        private Driver ResolveDriver(int? seed, int? fromMatch)
        {
            if (seed.HasValue && seed.Value > 0)
                return seedMap.ContainsKey(seed.Value) ? seedMap[seed.Value] : null;

            if (fromMatch.HasValue && Results.HasResult(fromMatch.Value))
                return Results.GetWinner(fromMatch.Value);

            return null;
        }

        public void RewindToMatchRound(int matchId)
        {
            Results.ClearFromMatch(matchId);
            RefreshBracketState();
        }
    }
}
