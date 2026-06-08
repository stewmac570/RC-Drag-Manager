using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests.Helpers;

/// <summary>
/// Reusable builders for <see cref="RaceSession"/> and <see cref="MultiClassEvent"/>
/// so service/controller tests can construct realistic sessions without re-deriving
/// the same inline boilerplate (issue #290). Pairs with <see cref="TestDriverFactory"/>,
/// which supplies driver packs. All builders are headless — no WinForms, runnable under
/// <c>dotnet test</c>.
/// </summary>
internal static class TestSessionFactory
{
    private static readonly DateTime DefaultDate = new DateTime(2026, 3, 24, 9, 0, 0);

    /// <summary>
    /// A Pro Ladder (seeded knockout) session. The first arg passed to
    /// <c>RaceController.GenerateBracket</c> is the engine key "Pro Ladder";
    /// <see cref="RaceSession.ClassType"/> defaults to "Heads Up" to match the
    /// existing flow-test sessions.
    /// </summary>
    public static RaceSession ProLadder(string classType = "Heads Up", string eventName = "QA Pro Ladder")
    {
        return new RaceSession
        {
            EventName = eventName,
            EventDate = DefaultDate,
            RaceType = "Pro Ladder",
            ClassType = classType
        };
    }

    /// <summary>
    /// A Round Robin session. <paramref name="variant"/> is "Standard" or "QMDRA";
    /// <paramref name="roundsToRun"/> is only meaningful for QMDRA.
    /// </summary>
    public static RaceSession RoundRobin(
        string variant = "Standard",
        int? roundsToRun = null,
        string classType = "Open",
        string eventName = "QA Round Robin")
    {
        return new RaceSession
        {
            EventName = eventName,
            EventDate = DefaultDate,
            RaceType = RaceTypes.RoundRobin,
            ClassType = classType,
            RoundRobinVariant = variant,
            RoundsToRun = roundsToRun
        };
    }

    /// <summary>
    /// Populates <see cref="RaceSession.DriverEntries"/> from a driver pack, seeding
    /// each entry's dial-in from the driver's qualifying time when present. Returns the
    /// same session for fluent chaining. Needed by tests that exercise the dial-in
    /// commands, which read/write <c>DriverEntries</c>.
    /// </summary>
    public static RaceSession WithDriverEntries(this RaceSession session, IEnumerable<Driver> drivers)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (drivers == null) throw new ArgumentNullException(nameof(drivers));

        session.DriverEntries = drivers.Select(d => new RaceSessionDriverEntry
        {
            DriverID = d.Id,
            DriverName = d.Name,
            ClassType = session.ClassType,
            QualifyingTime = d.QualTime,
            DialIn = d.QualTime
        }).ToList();

        return session;
    }

    /// <summary>
    /// A multi-class parent event wrapping the supplied class sessions. Each class
    /// session inherits the event name/date so the parent and children stay consistent.
    /// </summary>
    public static MultiClassEvent MultiClassEvent(string eventName, params RaceSession[] classSessions)
    {
        var evt = new MultiClassEvent
        {
            EventName = eventName,
            EventDate = DefaultDate,
            ClassSessions = (classSessions ?? Array.Empty<RaceSession>()).ToList()
        };

        foreach (var session in evt.ClassSessions)
        {
            session.EventName = eventName;
            session.EventDate = evt.EventDate;
        }

        return evt;
    }
}
