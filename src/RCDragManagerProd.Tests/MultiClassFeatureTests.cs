using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Tests.Helpers;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.Tests
{
    // =========================================================================
    // MultiClassEventRepositoryTests
    // Tests for MultiClassEventRepository save / load / delete behaviour.
    // Uses an in-memory SQLite database — no external setup required.
    // =========================================================================

    [TestClass]
    public class MultiClassEventRepositoryTests
    {
        private TemporarySqliteDb _db;
        private MultiClassEventRepository _repo;

        [TestInitialize]
        public void Setup()
        {
            _db = new TemporarySqliteDb();
            DatabaseInitializer.InitializeDatabase(_db.ConnectionString);
            _repo = new MultiClassEventRepository(_db.ConnectionString);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _db?.Dispose();
        }

        // ---------------------------------------------------------------------

        [TestMethod]
        public void SaveEvent_SetsIdOnObject()
        {
            var evt = BuildSampleEvent(classCount: 1);
            Assert.AreEqual(0, evt.Id, "Id should be 0 before save");

            _repo.SaveEvent(evt);

            Assert.AreNotEqual(0, evt.Id, "Id should be set after save");
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip_PreservesEventName()
        {
            var evt = BuildSampleEvent(classCount: 1);
            evt.EventName = "Test Event Alpha";

            _repo.SaveEvent(evt);
            var loaded = _repo.LoadEvent(evt.Id);

            Assert.AreEqual("Test Event Alpha", loaded.EventName);
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip_PreservesEventDate()
        {
            var date = new DateTime(2026, 3, 15);
            var evt = BuildSampleEvent(classCount: 1);
            evt.EventDate = date;

            _repo.SaveEvent(evt);
            var loaded = _repo.LoadEvent(evt.Id);

            Assert.AreEqual(date.Date, loaded.EventDate.Date);
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip_PreservesClassCount()
        {
            var evt = BuildSampleEvent(classCount: 3);

            _repo.SaveEvent(evt);
            var loaded = _repo.LoadEvent(evt.Id);

            Assert.AreEqual(3, loaded.ClassSessions.Count);
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip_PreservesClassNames()
        {
            var evt = BuildSampleEvent(classCount: 2);
            evt.ClassSessions[0].ClassType = "Open";
            evt.ClassSessions[1].ClassType = "Stock 2WD";

            _repo.SaveEvent(evt);
            var loaded = _repo.LoadEvent(evt.Id);

            Assert.AreEqual("Open", loaded.ClassSessions[0].ClassType);
            Assert.AreEqual("Stock 2WD", loaded.ClassSessions[1].ClassType);
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip_PreservesDriverEntries()
        {
            var evt = BuildSampleEvent(classCount: 1);
            evt.ClassSessions[0].DriverEntries = new List<RaceSessionDriverEntry>
            {
                new RaceSessionDriverEntry { DriverID = 1, DriverName = "Alice" },
                new RaceSessionDriverEntry { DriverID = 2, DriverName = "Bob" }
            };

            _repo.SaveEvent(evt);
            var loaded = _repo.LoadEvent(evt.Id);

            Assert.AreEqual(2, loaded.ClassSessions[0].DriverEntries.Count);
            Assert.AreEqual("Alice", loaded.ClassSessions[0].DriverEntries[0].DriverName);
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip_PreservesRrVariant()
        {
            var evt = BuildSampleEvent(classCount: 1);
            evt.ClassSessions[0].RoundRobinVariant = "QMDRA";
            evt.ClassSessions[0].RoundsToRun = 5;

            _repo.SaveEvent(evt);
            var loaded = _repo.LoadEvent(evt.Id);

            Assert.AreEqual("QMDRA", loaded.ClassSessions[0].RoundRobinVariant);
            Assert.AreEqual(5, loaded.ClassSessions[0].RoundsToRun);
        }

        [TestMethod]
        public void SaveTwice_CreatesTwoRows()
        {
            var evt = BuildSampleEvent(classCount: 1);

            _repo.SaveEvent(evt);
            _repo.SaveEvent(evt); // second save — append-only, creates new row

            var all = _repo.GetAllEvents();
            Assert.AreEqual(2, all.Count);
        }

        [TestMethod]
        public void DeleteEvent_RemovesFromGetAllEvents()
        {
            var evt = BuildSampleEvent(classCount: 1);
            _repo.SaveEvent(evt);

            _repo.DeleteEvent(evt.Id);

            var all = _repo.GetAllEvents();
            Assert.AreEqual(0, all.Count);
        }

        [TestMethod]
        public void GetAllEvents_ReturnsCorrectClassCount()
        {
            var evt = BuildSampleEvent(classCount: 3);
            _repo.SaveEvent(evt);

            var all = _repo.GetAllEvents();

            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(3, all[0].ClassCount);
        }

        [TestMethod]
        public void GetAllEvents_ReturnsCorrectEventName()
        {
            var evt = BuildSampleEvent(classCount: 1);
            evt.EventName = "Club Race May";
            _repo.SaveEvent(evt);

            var all = _repo.GetAllEvents();

            Assert.AreEqual("Club Race May", all[0].EventName);
        }

        [TestMethod]
        public void GetAllEvents_EmptyWhenNothingSaved()
        {
            var all = _repo.GetAllEvents();
            Assert.AreEqual(0, all.Count);
        }

        // ---------------------------------------------------------------------

        private static MultiClassEvent BuildSampleEvent(int classCount)
        {
            var evt = new MultiClassEvent
            {
                EventName = "Sample Event",
                EventDate = DateTime.Today,
                ClassSessions = new List<RaceSession>()
            };

            for (int i = 0; i < classCount; i++)
            {
                evt.ClassSessions.Add(new RaceSession
                {
                    EventName = evt.EventName,
                    EventDate = evt.EventDate,
                    RaceType = "Round Robin",
                    ClassType = $"Class {i + 1}",
                    RoundRobinVariant = "Standard",
                    DriverEntries = new List<RaceSessionDriverEntry>()
                });
            }

            return evt;
        }
    }


    // =========================================================================
    // RaceControllerMultiClassTests
    // Tests for the two new RaceController methods added for multi-class support:
    //   HasPendingMatchesInCurrentRound()
    //   IsRrComplete()
    // =========================================================================

    [TestClass]
    public class RaceControllerMultiClassTests
    {
        // Helper: build a minimal RR session with N drivers
        private static (RaceController controller, List<Driver> drivers) 
            BuildRrController(int driverCount, string variant = "Standard", int? roundsToRun = null)
        {
            var drivers = Enumerable.Range(1, driverCount)
                .Select(i => new Driver { Name = $"Driver{i}" })
                .ToList();

            // Assign IDs manually (mirrors how TestDriverFactory works)
            for (int i = 0; i < drivers.Count; i++)
                drivers[i] = new Driver { Name = drivers[i].Name };

            var session = new RaceSession
            {
                EventName = "Test",
                EventDate = DateTime.Today,
                RaceType = "Round Robin",
                ClassType = "Open",
                RoundRobinVariant = variant,
                RoundsToRun = roundsToRun,
                DriverEntries = drivers.Select(d => new RaceSessionDriverEntry
                {
                    DriverID = d.Id,
                    DriverName = d.Name
                }).ToList()
            };

            var controller = new RaceController(session, new NoOpStandingsDialogService());

            return (controller, drivers);
        }

        // ---------------------------------------------------------------------

        [TestMethod]
        public void HasPendingMatchesInCurrentRound_ReturnsTrueBeforeAnyWinnersSubmitted()
        {
            var (controller, drivers) = BuildRrController(4);
            controller.GenerateBracket("Round Robin", drivers);

            bool pending = controller.HasPendingMatchesInCurrentRound();

            Assert.IsTrue(pending, 
                "Should report pending matches when no winners have been submitted");
        }

        [TestMethod]
        public void HasPendingMatchesInCurrentRound_ReturnsFalseWhenAllRoundOneMatchesResolved()
        {
            var (controller, drivers) = BuildRrController(4);
            controller.GenerateBracket("Round Robin", drivers);

            // Submit winners for all matches in the first visible round
            var matches = controller.PeekUpcomingMatches(20).ToList();
            foreach (var match in matches)
                controller.SubmitWinner(match.MatchId, firstOption: true);

            bool pending = controller.HasPendingMatchesInCurrentRound();

            Assert.IsFalse(pending,
                "Should not report pending matches when all round matches are resolved");
        }

        [TestMethod]
        public void IsRrComplete_ReturnsFalseWhenMatchesRemain()
        {
            var (controller, drivers) = BuildRrController(4);
            controller.GenerateBracket("Round Robin", drivers);

            bool complete = controller.IsRrComplete();

            Assert.IsFalse(complete,
                "RR should not be complete when matches are unresolved");
        }

        [TestMethod]
        public void IsRrComplete_ReturnsTrueWhenSessionIsLosersPhase()
        {
            var (controller, drivers) = BuildRrController(4);
            controller.GenerateBracket("Round Robin", drivers);

            // Manually set session race type to simulate post-RR phase
            // (mirrors what happens after buyback/LB transition)
            var sessionField = typeof(RaceController)
                .GetField("_session", System.Reflection.BindingFlags.NonPublic | 
                                      System.Reflection.BindingFlags.Instance);
            var session = (RaceSession)sessionField.GetValue(controller);
            session.RaceType = "Losers Bracket";

            bool complete = controller.IsRrComplete();

            Assert.IsTrue(complete,
                "IsRrComplete should return true when session is past the RR phase");
        }

        [TestMethod]
        public void IsRrComplete_ReturnsTrueWhenSessionIsFinalsPhase()
        {
            var (controller, drivers) = BuildRrController(4);
            controller.GenerateBracket("Round Robin", drivers);

            var sessionField = typeof(RaceController)
                .GetField("_session", System.Reflection.BindingFlags.NonPublic |
                                      System.Reflection.BindingFlags.Instance);
            var session = (RaceSession)sessionField.GetValue(controller);
            session.RaceType = "Finals";

            Assert.IsTrue(controller.IsRrComplete());
        }
    }


    // =========================================================================
    // MultiClassSetupValidationTests
    // Tests for MultiClassSetupForm business logic — duplicate class name
    // validation and zero-driver class blocking.
    // These test the logic layer directly, not the WinForms UI.
    // =========================================================================

    [TestClass]
    public class MultiClassSetupValidationTests
    {
        // NOTE: These tests depend on MultiClassSetupForm exposing its validation
        // logic as internal/public methods, or via a separate validator class.
        // Recommended: extract validation into a static
        // MultiClassSetupValidator.cs so it can be tested without a Form instance.
        //
        // Expected static validator API:
        //   MultiClassSetupValidator.ValidateClassName(string name, IList<string> existing)
        //     → returns null (valid) or an error string
        //   MultiClassSetupValidator.ValidateCanStart(IList<ClassConfig> classes)
        //     → returns null (valid) or an error string

        [TestMethod]
        public void ValidateClassName_ReturnsError_WhenNameIsEmpty()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }

        [TestMethod]
        public void ValidateClassName_ReturnsError_WhenNameIsWhitespace()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }

        [TestMethod]
        public void ValidateClassName_ReturnsNull_WhenNameIsUniqueAndNonEmpty()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }

        [TestMethod]
        public void ValidateClassName_ReturnsError_WhenNameDuplicatesExisting_ExactMatch()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }

        [TestMethod]
        public void ValidateClassName_ReturnsError_WhenNameDuplicatesExisting_CaseInsensitive()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }

        [TestMethod]
        public void ValidateCanStart_ReturnsError_WhenClassHasZeroDrivers()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }

        [TestMethod]
        public void ValidateCanStart_ReturnsNull_WhenAllClassesHaveDrivers()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }

        [TestMethod]
        public void ValidateCanStart_ReturnsNull_ForSingleClassEvent()
        {
            Assert.Inconclusive("Implement after MultiClassSetupValidator exists");
        }
    }


    // =========================================================================
    // MultiClassLbGateTests
    // Tests for the LB gate logic — all classes must complete RR before
    // any class can transition to the Losers Bracket phase.
    // These test the gate evaluation logic in isolation.
    // =========================================================================

    [TestClass]
    public class MultiClassLbGateTests
    {
        // NOTE: These tests depend on the gate logic being extractable or
        // testable via MultiClassRaceForm in a headless mode, OR via a
        // separate MultiClassGateEvaluator class.
        //
        // Recommended: extract gate logic into MultiClassGateEvaluator:
        //   bool IsGateOpen(IList<RaceController> controllers)
        //     → true if all controllers report IsRrComplete() == true

        [TestMethod]
        public void Gate_IsNotOpen_WhenOneClassIsIncomplete()
        {
            // Two controllers — one complete, one not
            // Gate should be closed
            Assert.Inconclusive("Implement after MultiClassGateEvaluator exists");
        }

        [TestMethod]
        public void Gate_IsOpen_WhenAllClassesComplete()
        {
            // Two controllers — both complete
            // Gate should be open
            Assert.Inconclusive("Implement after MultiClassGateEvaluator exists");
        }

        [TestMethod]
        public void Gate_IsOpen_WhenAbandonedClassIsExcluded()
        {
            // One class has all matches resolved (IsRrComplete = true)
            // but was never explicitly advanced. Should still open gate.
            Assert.Inconclusive("Implement after MultiClassGateEvaluator exists");
        }
    }

    // =========================================================================
    // TemporarySqliteDb — helper used by repository tests
    // Creates a real file-based SQLite database in the temp folder and
    // deletes it on dispose. File-based connections share state across
    // multiple open connections, unlike :memory: connections.
    // =========================================================================

    internal sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try
            {
                if (File.Exists(DatabasePath))
                    File.Delete(DatabasePath);
            }
            catch { /* best-effort cleanup */ }
        }
    }
}
