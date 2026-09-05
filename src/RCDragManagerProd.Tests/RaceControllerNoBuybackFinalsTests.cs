using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Guards the "nobody bought back" route to the Finals.
///
/// Round Robin used to finish, offer the buyback, and then dead-end if no one
/// entered: GenerateLosersBracket() rejected an empty selection and the Finals
/// gate was never raised, so the event had no way through to the Final. The
/// wildcard slot now falls to 4th on Round Robin ranking, the same rule the
/// "not enough drivers to buy back" path already used.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceControllerNoBuybackFinalsTests
{
    [TestMethod]
    public void NoBuybackEntries_PromotesFourthAsWildcard_AndOpensFinals()
    {
        var controller = RunRoundRobinToBuybackOffer(out var startFinalsSignals, out var session);

        // The scenario that used to dead-end: plenty of drivers *could* buy back,
        // so the offer is made and the flow waits for entries that never come.
        var eligible = controller.GetEligibleBuybackDrivers();
        Assert.IsTrue(eligible.Count >= 2,
            $"Test needs >= 2 eligible buyback drivers to reproduce the stuck case, got {eligible.Count}");
        Assert.IsFalse(startFinalsSignals.Any(v => v),
            "Finals gate must still be down while the buyback offer is open");

        var expectedWildcard = controller.ResolveWildcardFinalist();
        Assert.IsNotNull(expectedWildcard, "A wildcard finalist must be resolvable from the standings");

        Assert.IsTrue(controller.SkipBuybacksToFinals(),
            "SkipBuybacksToFinals() must succeed when the Top-3 snapshot and standings are present");

        Assert.IsTrue(startFinalsSignals.Any(v => v),
            "CanStartFinalsChanged(true) must fire so the RD can start the Finals");
        Assert.AreEqual(RaceController.FinalsReasonBuybackSkipped, controller.FinalsPendingReason,
            "Pending reason must record that the buyback was skipped");
        Assert.AreEqual(expectedWildcard.Name, controller.FinalsPendingWildcardName,
            "The promoted wildcard must be the one the shared rule picked");
        Assert.AreEqual("Round Robin", session.RaceType,
            "Race type must not change until the RD actually starts the Finals");
    }

    /// <summary>The wildcard comes from outside the Top-3 - it is the next driver
    /// down, not someone already holding a Finals place.</summary>
    [TestMethod]
    public void NoBuybackEntries_WildcardComesFromOutsideTheTopThree()
    {
        var controller = RunRoundRobinToBuybackOffer(out _, out _);

        var eligible = controller.GetEligibleBuybackDrivers();
        var wildcard = controller.ResolveWildcardFinalist();

        Assert.IsNotNull(wildcard);
        Assert.IsTrue(eligible.Any(d => d.Id == wildcard.Id),
            $"Wildcard '{wildcard.Name}' must be a buyback-eligible driver (outside the Top-3). " +
            $"Eligible: [{string.Join(", ", eligible.Select(d => d.Name))}]");
    }

    [TestMethod]
    public void NoBuybackEntries_StartingFinals_FieldsTopThreePlusWildcard()
    {
        var controller = RunRoundRobinToBuybackOffer(out _, out var session);

        var wildcard = controller.ResolveWildcardFinalist();
        Assert.IsTrue(controller.SkipBuybacksToFinals());

        controller.InjectFinal4Bracket();

        Assert.AreEqual(RaceTypes.Finals, session.RaceType,
            "Starting the Finals must switch the session race type");

        var finalists = controller.BuildCurrentBracketRows()
            .Where(r => !r.IsHeader)
            .SelectMany(r => new[] { r.Driver1, r.Driver2 })
            .Where(n => !string.IsNullOrWhiteSpace(n) &&
                        !string.Equals(n, "BYE", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.IsTrue(finalists.Contains(wildcard.Name, StringComparer.OrdinalIgnoreCase),
            $"The promoted wildcard '{wildcard.Name}' must appear in the Finals bracket. " +
            $"Got: [{string.Join(", ", finalists)}]");
    }

    // ── Shared setup: run a Standard RR right through to the buyback offer ──────
    private static RaceController RunRoundRobinToBuybackOffer(
        out List<bool> startFinalsSignals,
        out RaceSession session)
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(6);
        session = new RaceSession
        {
            EventName = "No Buyback Finals",
            EventDate = new DateTime(2026, 9, 5),
            RaceType = "Round Robin",
            ClassType = "Open",
            RoundRobinVariant = "Standard"
        };

        var controller = new RaceController(session, new NoOpStandingsDialogService());

        var buybackSignals = new List<bool>();
        var finalsSignals = new List<bool>();
        controller.CanOfferBuybackChanged += v => buybackSignals.Add(v);
        controller.CanStartFinalsChanged += v => finalsSignals.Add(v);

        controller.GenerateBracket("Round Robin", drivers);

        var rrRoundLabels = controller.BuildCurrentBracketRows()
            .Where(r => r.IsHeader)
            .Select(r => r.RoundLabel ?? "")
            .Where(r => r.StartsWith("RR", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        for (int ri = 0; ri < rrRoundLabels.Count; ri++)
        {
            foreach (var match in controller.PeekUpcomingMatches(20).ToList())
                controller.SubmitWinner(match.MatchId, firstOption: true);

            if (ri < rrRoundLabels.Count - 1)
                controller.AdvanceRound();
        }

        Assert.IsTrue(buybackSignals.Any(v => v),
            "CanOfferBuybackChanged(true) must fire once all RR rounds are complete");

        startFinalsSignals = finalsSignals;
        return controller;
    }
}
