// RaceController.Results.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;            // still used for a couple of info popups

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.UI.Forms;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        public void SubmitWinner(int matchId, bool firstOption)
        {
            EnsureReady();

            var match = EngineGetMatches(_engine, matchId: matchId).FirstOrDefault(m => m.MatchId == matchId);
            if (match == null)
            {
                Logger.Log($"[WINNER] Reject — match {matchId} not found.");
                return;
            }

            // Gate: in RR active-round mode, only accept results for the active round.
            if (_activeRound != null &&
                !string.Equals(match.RoundLabel, _activeRound, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[WINNER] Reject — M{matchId} round '{match.RoundLabel}' is not the active round '{_activeRound}'.");
                return;
            }

            if (EngineHasWinner(_engine, matchId, match.RoundLabel))
            {
                Logger.Log($"[WINNER] Reject — match {matchId} already has a winner.");
                return;
            }

            var winner = firstOption ? match.Driver1 : match.Driver2;
            var loser = firstOption ? match.Driver2 : match.Driver1;

            // Universal block — no BYE as winner
            if (ByePolicy.IsBye(winner) || string.Equals(winner?.Name?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[WINNER] Reject — cannot select BYE as winner for M{matchId}.");
                return;
            }
            if (ByePolicy.IsBye(loser))
                Logger.Log($"[WINNER][BYE] Auto-advance M{matchId} -> {winner.Name}");

            Logger.Log($"[WINNER] M{matchId} {match.RoundLabel}: {winner.Name} over {(loser?.Name ?? "BYE")}");

            EngineSetWinner(_engine, matchId, winner, match.RoundLabel);
            _matchResult.SetWinner(matchId, winner, loser);

            _winners.Add(new WinnerRow
            {
                MatchId = matchId,
                RoundLabel = match.RoundLabel,
                Winner = winner.Name,
                Loser = loser?.Name ?? "BYE"
            });

            WinnersUpdated?.Invoke(_winners);

            // advance UI/state first
            PushNextMatch();
            PushAdvanceState();

            // Per-round RR scoring once a full RR round is resolved
            if (_engine is RoundRobinEngineAdapter rr)
            {
                Logger.Log("[CTRL][ENGINE-CAST] Using RoundRobinEngineAdapter for RR-only completed-round scorecard.");
                TryLogCompletedRound(rr);
            }
            QueueLiveUpdate("SubmitWinner");
        }

        public Driver GetWinner(int matchId) => _results.GetWinner(matchId);
        public Driver GetLoser(int matchId) => _results.GetLoser(matchId);

        public List<Driver> GetEligibleBuybackDrivers()
        {
            Logger.Log("?? Starting Round Robin buyback eligibility check...");

            if (_engine is not RoundRobinEngineAdapter rr)
            {
                Logger.Log("? Engine is not RoundRobinEngineAdapter — buyback not available.");
                return new List<Driver>();
            }
            Logger.Log("[CTRL][ENGINE-CAST] Using RoundRobinEngineAdapter for RR-only buyback ranking methods.");

            var rrMatches = EngineGetMatches(_engine) ?? new List<EngineMatch>();
            var allDrivers = rrMatches
                .SelectMany(m => new[] { m.Driver1, m.Driver2 })
                .Where(d => d != null)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .ToList();

            if (allDrivers.Count == 0 && _session?.Drivers != null)
                allDrivers = _session.Drivers.ToList();

            Logger.Log($"?? RR roster from matches: {allDrivers.Count} ? [{string.Join(", ", allDrivers.Select(d => d.Name))}]");

            Logger.Log("[EngineCall] " + _engine.GetType().Name + " GetTopRankedDrivers matchId=- round=-");
            var top3 = (_rrTop3 != null && _rrTop3.Count == 3) ? _rrTop3 : rr.GetTopRankedDrivers(3);
            Logger.Log($"?? Top-3: [{string.Join(", ", top3.Select(d => d.Name))}]");

            var top3Ids = new HashSet<int>(top3.Select(d => d.Id));

            var eligible = allDrivers.Where(d => !top3Ids.Contains(d.Id)).ToList();
            Logger.Log($"? Buyback-eligible count: {eligible.Count} ? [{string.Join(", ", eligible.Select(d => d.Name))}]");

            if (eligible.Count < 2)
                Logger.Log("?? Only 1 or 0 eligible drivers — Losers Bracket cannot be created.");

            return eligible;
        }

        public bool EditWinnerInActiveRound(int matchId, bool firstOption)
        {
            EnsureReady();

            var match = EngineGetMatches(_engine, matchId: matchId).FirstOrDefault(m => m.MatchId == matchId);
            if (match == null)
            {
                Logger.Log($"[CTRL][EDIT] M{matchId} not found.");
                return false;
            }

            var active = GetActiveRoundLabel();
            if (string.IsNullOrEmpty(active) ||
                !string.Equals(match.RoundLabel, active, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[CTRL][EDIT] Reject edit — M{matchId} is in '{match.RoundLabel}', active='{active}'.");
                return false;
            }

            var newWinner = firstOption ? match.Driver1 : match.Driver2;
            var newLoser = firstOption ? match.Driver2 : match.Driver1;

            if (ByePolicy.IsBye(newWinner) || string.Equals(newWinner?.Name?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[CTRL][EDIT] Reject edit — cannot set BYE as winner (M{matchId}).");
                return false;
            }

            EngineSetWinner(_engine, matchId, newWinner, match.RoundLabel);
            _matchResult.SetWinner(matchId, newWinner, newLoser);
            Logger.Log($"[CTRL][EDIT] SetWinner applied: M{matchId}, winner='{newWinner.Name}', loser='{newLoser?.Name ?? "BYE"}'");

            var row = _winners.FirstOrDefault(w => w.MatchId == matchId);
            if (row != null)
            {
                row.Winner = newWinner.Name;
                row.Loser = newLoser?.Name ?? "BYE";
            }
            else
            {
                _winners.Add(new WinnerRow
                {
                    MatchId = matchId,
                    RoundLabel = match.RoundLabel,
                    Winner = newWinner.Name,
                    Loser = newLoser?.Name ?? "BYE"
                });
            }

            Logger.Log($"[CTRL][EDIT] Override: M{matchId} ({match.RoundLabel}) ? {newWinner.Name} over {(newLoser?.Name ?? "BYE")}.");

            WinnersUpdated?.Invoke(_winners);
            PushNextMatch();
            PushAdvanceState();
            QueueLiveUpdate("EditWinner");
            return true;
        }
    }
}
