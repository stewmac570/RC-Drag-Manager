using System;
using System.Collections.Generic;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// UI-independent saved-race loading operations for the Load Session screen (issue #288).
    /// Wraps <see cref="RaceSessionRepository"/> and <see cref="MultiClassEventRepository"/> so
    /// the form holds no repositories and performs no persistence or domain assembly itself.
    /// No WinForms references — the same operations can back a future UI and are covered by
    /// headless tests against an in-memory database.
    /// </summary>
    public sealed class LoadSessionService
    {
        private readonly RaceSessionRepository _sessionRepo;
        private readonly MultiClassEventRepository _multiClassRepo;

        public LoadSessionService(RaceSessionRepository sessionRepo, MultiClassEventRepository multiClassRepo)
        {
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _multiClassRepo = multiClassRepo ?? throw new ArgumentNullException(nameof(multiClassRepo));
        }

        // ── List ─────────────────────────────────────────────────────────────────

        public List<RaceSessionSummary> ListSessions()
        {
            Logger.Log("[SVC][LoadSession] ListSessions()");
            return _sessionRepo.GetAllSessions() ?? new List<RaceSessionSummary>();
        }

        public List<MultiClassEventSummary> ListMultiClassEvents()
        {
            Logger.Log("[SVC][LoadSession] ListMultiClassEvents()");
            return _multiClassRepo.GetAllEvents() ?? new List<MultiClassEventSummary>();
        }

        // ── Load ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads a single-class session by id and wraps it into a one-class
        /// <see cref="MultiClassEvent"/> ready to open in <c>MultiClassRaceForm</c>.
        /// </summary>
        public LoadResult LoadEvent(int id, bool isMultiClass) =>
            isMultiClass ? LoadMultiClassEvent(id) : LoadSingleClassSession(id);

        public LoadResult LoadSingleClassSession(int id)
        {
            Logger.Log($"[SVC][LoadSession] LoadSingleClassSession(id={id})");
            var repositoryResult = _sessionRepo.TryLoadSession(id);
            if (!repositoryResult.Success)
            {
                Logger.Log(
                    $"[SVC][LoadSession][WARN] Session load failed with status " +
                    $"{repositoryResult.Status}");
                return repositoryResult.Status == RaceSessionLoadStatus.NotFound
                    ? LoadResult.Fail("This saved race no longer exists.")
                    : LoadResult.Fail(
                        "This saved race is damaged or from an unsupported older version. " +
                        "It has not been deleted.");
            }

            var session = repositoryResult.Session;
            var wrapped = new MultiClassEvent
            {
                EventName = session.EventName,
                EventDate = session.EventDate != default ? session.EventDate : DateTime.Now,
                ClassSessions = new List<RaceSession> { session }
            };

            Logger.Log($"[SVC][LoadSession] Wrapped '{wrapped.EventName}' (raceType='{session.RaceType}') into MultiClassEvent");
            return LoadResult.Ok(wrapped);
        }

        /// <summary>Loads a multi-class event by id.</summary>
        public LoadResult LoadMultiClassEvent(int id)
        {
            Logger.Log($"[SVC][LoadSession] LoadMultiClassEvent(id={id})");
            var evt = _multiClassRepo.LoadEvent(id);
            if (evt == null)
            {
                Logger.Log("[SVC][LoadSession][WARN] MultiClassEventRepository returned null");
                return LoadResult.Fail("Unable to load the selected event.");
            }

            return LoadResult.Ok(evt);
        }

        // ── Delete ────────────────────────────────────────────────────────────────

        public void DeleteSession(int id)
        {
            Logger.Log($"[SVC][LoadSession] DeleteSession(id={id})");
            _sessionRepo.DeleteSession(id);
        }

        public void DeleteMultiClassEvent(int id)
        {
            Logger.Log($"[SVC][LoadSession] DeleteMultiClassEvent(id={id})");
            _multiClassRepo.DeleteEvent(id);
        }
    }

    /// <summary>Result returned by load operations — carries the loaded event or an error message.</summary>
    public sealed class LoadResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        public MultiClassEvent Event { get; }

        private LoadResult(bool success, string errorMessage, MultiClassEvent evt)
        {
            Success = success;
            ErrorMessage = errorMessage;
            Event = evt;
        }

        public static LoadResult Ok(MultiClassEvent evt) =>
            new LoadResult(true, null, evt ?? throw new ArgumentNullException(nameof(evt)));

        public static LoadResult Fail(string message) =>
            new LoadResult(false, message ?? "Unknown error.", null);
    }
}
