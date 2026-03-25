using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// End-to-end test for the Standard Round Robin full lifecycle:
/// Bracket generation → all RR rounds → Buyback → Losers Bracket → Finals → Champion.
///
/// This test is the regression guard for the RR → LB transition that was broken
/// by the pre-reveal change in PR #123 (feature/rr-pre-reveal-rounds):
/// StartLosersBracket() was not clearing _activeRound, causing all
/// _activeRound-gated filters to look for LB matches with a stale RR round label.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceControllerRRStandardFlowTests
{
    [TestMethod]
    public void StandardRR_FullFlow_FromBracketToChampion()
    {
        // ── Setup ───────────────────────────────────────────────────────────────
        var drivers = TestDriverFactory.CreateRoundRobinPack(6);
        var session = new RaceSession
        {
            EventName  = "Standard RR Full Flow",
            EventDate  = new DateTime(2026, 3, 24),
            RaceType   = "Round Robin",
            ClassType  = "Open",
            RoundRobinVariant = "Standard"
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());

        var buybackSignals    = new List<bool>();
        var startFinalsSignals = new List<bool>();
        RaceController.RaceSummary completion = null;

        controller.CanOfferBuybackChanged  += v => buybackSignals.Add(v);
        controller.CanStartFinalsChanged   += v => startFinalsSignals.Add(v);
        controller.TournamentCompleted     += s => completion = s;

        // ── Phase 1: Bracket generation ─────────────────────────────────────────
        controller.GenerateBracket("Round Robin", drivers);

        Assert.AreEqual("Round Robin", session.RaceType);

        // All RR rounds must be immediately visible in the bracket (pre-reveal).
        var bracketRows = controller.BuildCurrentBracketRows();
        var headerLabels = bracketRows
            .Where(r => r.IsHeader)
            .Select(r => r.RoundLabel ?? "")
            .Distinct()
            .ToList();
        Assert.IsTrue(headerLabels.Count >= 2,
            $"Expected at least 2 round headers visible immediately after generation, got {headerLabels.Count}: [{string.Join(", ", headerLabels)}]");

        // Active round must be RR1.
        Assert.AreEqual("RR1", controller.GetActiveRoundLabel(),
            "GetActiveRoundLabel() must return 'RR1' immediately after GenerateBracket");

        // PeekUpcomingMatches must only return RR1 matches initially.
        var initialPeek = controller.PeekUpcomingMatches(20);
        Assert.IsTrue(initialPeek.Count > 0, "Must have at least one match in RR1");
        Assert.IsTrue(
            initialPeek.All(m => string.Equals(m.RoundLabel, "RR1", StringComparison.OrdinalIgnoreCase)),
            "PeekUpcomingMatches must only return active-round (RR1) matches initially");

        // ── Phase 2: Run all RR rounds ──────────────────────────────────────────
        // Collect the round labels from bracket headers in their displayed order.
        var rrRoundLabels = bracketRows
            .Where(r => r.IsHeader)
            .Select(r => r.RoundLabel ?? "")
            .Where(r => r.StartsWith("RR", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        Assert.IsTrue(rrRoundLabels.Count >= 2,
            $"Standard RR with 6 drivers must have >= 2 round labels, got {rrRoundLabels.Count}");

        for (int ri = 0; ri < rrRoundLabels.Count; ri++)
        {
            string expectedActiveLabel = rrRoundLabels[ri];

            Assert.AreEqual(expectedActiveLabel, controller.GetActiveRoundLabel(),
                $"Phase 2 iteration {ri}: expected active round '{expectedActiveLabel}'");

            // Resolve all matches in the active round.
            ResolveVisibleMatches(controller);

            if (ri < rrRoundLabels.Count - 1)
            {
                // Not the last round: the active round should not have advanced yet.
                Assert.AreEqual(expectedActiveLabel, controller.GetActiveRoundLabel(),
                    $"Active round must not change until AdvanceRound() is called (iteration {ri})");

                // After resolving the active round, PeekUpcomingMatches must return 0
                // (all active-round matches done, next round not yet opened).
                var peekAfterResolve = controller.PeekUpcomingMatches(20);
                Assert.AreEqual(0, peekAfterResolve.Count,
                    $"After resolving {expectedActiveLabel}, PeekUpcomingMatches should return 0 before AdvanceRound()");

                // Advance to next round.
                controller.AdvanceRound();

                string nextLabel = rrRoundLabels[ri + 1];
                Assert.AreEqual(nextLabel, controller.GetActiveRoundLabel(),
                    $"After AdvanceRound(), active round must be '{nextLabel}'");
            }
        }

        // ── Phase 3: Buyback / LB setup ─────────────────────────────────────────
        // After the final RR round resolves, CanOfferBuybackChanged(true) must fire.
        Assert.IsTrue(buybackSignals.Any(v => v),
            "CanOfferBuybackChanged(true) must fire after all RR rounds complete");
        Assert.AreEqual("Round Robin", session.RaceType,
            "Session must still be 'Round Robin' during the buyback offer (not yet transitioned)");

        var eligible = controller.GetEligibleBuybackDrivers();
        Assert.IsTrue(eligible.Count >= 2,
            $"Must have >= 2 eligible buyback drivers, got {eligible.Count}");

        // GenerateLosersBracket stores the selection and starts the LB.
        controller.GenerateLosersBracket(eligible);

        Assert.AreEqual("Losers Bracket", session.RaceType,
            "Session must be 'Losers Bracket' immediately after GenerateLosersBracket()");

        var lbMatchRows = controller.BuildCurrentBracketRows().Where(r => !r.IsHeader).ToList();
        Assert.IsTrue(lbMatchRows.Count > 0,
            "Losers Bracket bracket must contain at least one match row after generation");

        // PeekUpcomingMatches must return LB matches immediately — this is the regression
        // assertion that directly catches the _activeRound-not-cleared bug.
        var lbPeekImmediate = controller.PeekUpcomingMatches(20);
        Assert.IsTrue(lbPeekImmediate.Count > 0,
            "PeekUpcomingMatches must return LB matches immediately after GenerateLosersBracket() — " +
            "if this is 0, _activeRound was not cleared when the engine swapped to the LB adapter");

        // ── Phase 4: Run the Losers Bracket ─────────────────────────────────────
        // Keep resolving matches and advancing rounds until CanStartFinalsChanged fires.
        int lbMaxIterations = 30;
        while (!startFinalsSignals.Any(v => v) && lbMaxIterations-- > 0)
        {
            var lbMatches = controller.PeekUpcomingMatches(20).ToList();
            if (lbMatches.Count == 0)
                controller.AdvanceRound();
            else
                foreach (var m in lbMatches)
                    controller.SubmitWinner(m.MatchId, firstOption: true);
        }

        Assert.IsTrue(startFinalsSignals.Any(v => v),
            "CanStartFinalsChanged(true) must fire after Losers Bracket completes");

        // ── Phase 5: Finals ──────────────────────────────────────────────────────
        Assert.IsTrue(controller.IsFinalsPending,
            "IsFinalsPending must be true before StartFinals()");

        controller.StartFinals();

        Assert.AreEqual("Finals", session.RaceType,
            "Session must be 'Finals' after StartFinals()");

        var finalsBracketRows = controller.BuildCurrentBracketRows().Where(r => !r.IsHeader).ToList();
        Assert.IsTrue(finalsBracketRows.Count > 0,
            "Finals bracket must contain at least one match row after StartFinals()");

        // Run Finals to completion.
        int finalsMaxIterations = 20;
        while (completion == null && finalsMaxIterations-- > 0)
        {
            var finalsMatches = controller.PeekUpcomingMatches(20).ToList();
            if (finalsMatches.Count == 0)
                controller.AdvanceRound();
            else
                foreach (var m in finalsMatches)
                    controller.SubmitWinner(m.MatchId, firstOption: true);
        }

        Assert.IsNotNull(completion,
            "TournamentCompleted must fire after Finals are complete");
        Assert.AreEqual("Finals (Pro Ladder)", completion.Bracket);
        Assert.IsNotNull(completion.Winner,
            "RaceSummary must have a non-null Winner");
        Assert.IsFalse(string.IsNullOrWhiteSpace(completion.Winner.Name),
            "Winner must have a non-empty name");

        // ── Post-event: bracket display ordering assertion ───────────────────
        // After the tournament, BuildCurrentBracketRows must return headers in
        // chronological race order: RR rounds, then LB rounds, then Finals rounds.
        // This guards against the CompareKey bug that placed LB (key 400+) after
        // Finals (SF=290, F=299).
        var finalBracket = controller.BuildCurrentBracketRows();
        var finalHeaders = finalBracket
            .Where(r => r.IsHeader)
            .Select(r => r.RoundLabel ?? "")
            .Distinct()
            .ToList();

        // The last RR header must come before the first LB header.
        var rrHeaders  = finalHeaders.Where(h => h.StartsWith("RR", StringComparison.OrdinalIgnoreCase)).ToList();
        var lbHeaders  = finalHeaders.Where(h => h.StartsWith("LB", StringComparison.OrdinalIgnoreCase)).ToList();
        var sfHeaders  = finalHeaders.Where(h => string.Equals(h, "SF", StringComparison.OrdinalIgnoreCase)).ToList();
        var fHeaders   = finalHeaders.Where(h => string.Equals(h, "F",  StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.IsTrue(rrHeaders.Count >= 1, "Bracket must contain at least one RR header");
        Assert.IsTrue(lbHeaders.Count >= 1, "Bracket must contain at least one LB header");
        Assert.IsTrue(sfHeaders.Count + fHeaders.Count >= 1, "Bracket must contain at least one Finals header (SF or F)");

        int lastRrIndex   = finalHeaders.Select((h, i) => (h, i)).Last(x => x.h.StartsWith("RR", StringComparison.OrdinalIgnoreCase)).i;
        int firstLbIndex  = finalHeaders.Select((h, i) => (h, i)).First(x => x.h.StartsWith("LB", StringComparison.OrdinalIgnoreCase)).i;
        int firstSFOrFIdx = finalHeaders.Select((h, i) => (h, i))
            .First(x => string.Equals(x.h, "SF", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.h, "F",  StringComparison.OrdinalIgnoreCase)).i;

        Assert.IsTrue(lastRrIndex < firstLbIndex,
            $"All RR headers must precede all LB headers in bracket. Headers: [{string.Join(", ", finalHeaders)}]");
        Assert.IsTrue(firstLbIndex < firstSFOrFIdx,
            $"All LB headers must precede Finals headers (SF/F) in bracket. Headers: [{string.Join(", ", finalHeaders)}]");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static void ResolveVisibleMatches(RaceController controller)
    {
        var peek = controller.PeekUpcomingMatches(20).ToList();
        foreach (var match in peek)
            controller.SubmitWinner(match.MatchId, firstOption: true);
    }
}
