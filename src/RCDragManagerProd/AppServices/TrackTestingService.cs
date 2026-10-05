using System;
using System.Collections.Generic;
using RCDragManagerProd.Integration;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Track testing: the track is open, the stream is on, and there is no event.
    /// The stream overlay takes its times straight from the timing system but its
    /// names from RC Drag Manager's local feed, and it trusts that feed for 12
    /// hours. On race day (2026-10-03) that meant last night's test event, so a
    /// dummy event had to be created just to get clean times on the stream.
    ///
    /// Starting testing empties the local feed and writes one "Track testing"
    /// state with no races, which the overlay shows as times with no names.
    /// Stopping removes it. Nothing is saved and no driver is touched.
    /// </summary>
    public sealed class TrackTestingService
    {
        public const string TestingEventId = "track-testing";
        public const string TestingRaceType = "Testing";

        private readonly LiveLocalFeed _feed;

        public TrackTestingService(LiveLocalFeed feed = null)
        {
            _feed = feed ?? LiveLocalFeed.Default;
        }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            _feed.Clear();
            _feed.Update(BuildState(DateTime.Now));
            IsRunning = true;
            Logger.Log("[TESTING] Track testing started: stream feed cleared, times only.");
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _feed.Reset(TestingEventId, null);
            IsRunning = false;
            Logger.Log("[TESTING] Track testing stopped.");
        }

        public static LiveRaceUpdateDto BuildState(DateTime now) => new LiveRaceUpdateDto
        {
            EventId = TestingEventId,
            EventName = "Track testing",
            EventDate = now.ToString("yyyy-MM-dd"),
            ClassType = "Testing",
            RaceType = TestingRaceType,
            CurrentRound = "TEST",
            NextUp = string.Empty,
            Drivers = new List<LiveDriverEntryDto>(),
            Matches = new List<LiveMatchDto>(),
            Winners = new List<LiveWinnerDto>(),
            PublishedAtUtc = now.ToUniversalTime().ToString("o")
        };
    }
}
