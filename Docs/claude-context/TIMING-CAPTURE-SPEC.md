# Timing capture spec — Portatree times on the results form

Status: **design agreed, not built.** Written 2026-09-07.

The goal is that every race in the results form shows the times the two drivers
actually ran, captured from the track's Portatree Eliminator, saved with the
rest of the event, and pushed to the live site so drivers can see their own
times.

Related work lives in the `Portatree connection` repo
(`C:\Users\Stewart McMillan\source\repos\Portatree connection`), which owns the
serial link to the box. This spec covers the RC Drag Manager side and the seam
between the two.

---

## 1. The blocker, stated up front

**No real run time has ever been captured from the box.** That is fact A in
`Portatree connection/docs/ESTABLISHED_FACTS.md`: three track days, zero
measurement packets. The wiring, baud rate, framing, checksum and the dial-in
field layout of the LED sign line are all solved and verified on hardware. The
post-race frame that carries reaction time, 60ft, ET and MPH has not been seen
by anyone, so its byte layout is unknown.

Consequences for this spec:

- The parser that turns bytes into times **cannot be written yet**. It needs one
  real pass at the track.
- Everything downstream of the parser can be built now, tested against a fake
  feed, and will work unchanged when the parser lands.
- Nothing in this spec should be read as a claim that times are available today.

---

## 2. Why RC Drag Manager has to be the one that owns this

The timing box knows lanes. It reports a left value and a right value. It has no
idea who is racing.

RC Drag Manager is the only thing that knows both. It already assigns lanes per
race through `LaneFairnessManager` and exposes the result as
`RaceController.GetLaneAdjustedNames` and `IsLaneSwapped`, which the console
uses to label the two winner buttons. That existing lane decision is what turns
"left 1.031, right 1.104" into "Smith 1.031, Jones 1.104".

So the join is:

```
Portatree           RC Drag Manager
left / right   +    lane assignment for this race   =   driver / time
```

---

## 3. When a time attaches to a race

**Agreed with Stewart, 2026-09-07: automatic, no confirmation prompt.**

The app has no concept of a race being "underway". The console shows a round of
pairings and the operator clicks a winner when the race is over. That click is
the only reliable "this race just happened" signal in the app, and it arrives at
`RaceController.SubmitWinner(matchId, firstOption)`
(`Controllers/RaceController.Results.cs`).

So the rule is:

1. A completed pass arrives from the timing feed and is **held** as the pending
   time, with its own timestamp.
2. When the operator clicks a winner, the held time is attached to that race,
   mapped onto the two drivers by the lane assignment for that race.
3. The held slot is then cleared, so one pass can never attach to two races.

This needs no new "start race" button and no change to how the operator works.

Guard rails:

- A held time older than a configurable age (default: 5 minutes) is discarded
  rather than attached, so a stale pass cannot land on the wrong race.
- If no time is held when a winner is clicked, the race saves exactly as it does
  today with no times. Missing times are normal, not an error.
- Times never influence who won. The operator's click is the result. Times are
  recorded data only.

---

## 4. Where the times are stored

The whole `RaceSession` is serialized to one JSON blob in the `SessionData`
column (`Repositories/RaceSessionRepository.cs`), so this needs **no database
migration**. New properties simply appear in the blob, and older saves
deserialize with them empty.

### New domain type

Added to `Domain/RaceResults.cs`:

```csharp
public sealed class RaceTimingResult
{
    public int MatchId { get; set; }
    public string RoundLabel { get; set; }
    public DateTime CapturedAt { get; set; }

    public int? Driver1Id { get; set; }
    public RaceTimingValues Driver1 { get; set; }
    public int? Driver2Id { get; set; }
    public RaceTimingValues Driver2 { get; set; }

    /// <summary>The raw line the feed produced, kept so a bad parse can be
    /// re-read later without another track day.</summary>
    public string SourceRaw { get; set; }
}

public sealed class RaceTimingValues
{
    public double? ReactionTime { get; set; }
    public double? SixtyFoot { get; set; }
    public double? ElapsedTime { get; set; }
    public double? Speed { get; set; }
}
```

### Where it hangs

On `RaceSession`, as `List<RaceTimingResult> TimingResults`.

**Not** on `RaceResultMatchSnapshot`. `CapturePhaseSnapshot`
(`Controllers/RaceController.ResultSnapshots.cs`) rebuilds a phase snapshot from
engine state and replaces the existing one every time a winner is submitted.
Anything stored on the snapshot rows would be wiped on the next capture. Keeping
times in their own durable list on the session avoids that entirely.

Timing rows are keyed by `RoundLabel` + `MatchId`, which is unique within a
session across all three phases.

Every field is nullable on purpose. The box only sends 60ft, mid ET and MPH if
those beams are enabled in Track Configuration, so a partial row is a normal
result, not a failure.

---

## 5. Where the times are shown

### Results form (primary)

`Dialogs/RaceResultsWindow.xaml` and `AppServices/RaceResultsPresentationBuilder.cs`.

The presentation builder joins `TimingResults` onto the match rows by
`RoundLabel` + `MatchId` and exposes the values on the existing row view models.
The results list gains time columns; the ladder gains a compact time under each
driver's name where one exists.

Two changes to how the window behaves:

- The window is built once in its constructor
  (`RaceResultsPresentationBuilder.Build(session)`). It needs a refresh so it
  updates while left open on a second screen as races are run.
- Rows with no time render blank, not zero.

**Not on the race console.** There is no room for it there, and the console's job
is running the race.

### Editing

The results form gets an edit path for a timing row, matching the existing
`EditResultDialog` pattern: clear a time that landed on the wrong race, or fix a
value the box garbled. This is the safety net that makes auto-attach safe.

### Live site

`Integration/LiveRaceUpdateDto.cs` already carries per-match lane and dial-in
data to the live server, populated in `RaceController.LiveUpdate.cs`. Times
become extra nullable fields on `LiveMatchDto`, and the live scoreboard shows
them on the match cards next to the dial-in badge.

The live server side (`RCDragLiveServer`) needs its DTO widened to match. That
is a separate change in that repo, tracked separately.

---

## 6. How the two apps talk

**The sniffer keeps sole ownership of the serial port and writes a file. RC Drag
Manager watches that file.**

The reasons:

- Only one process can hold a serial port
  (`ESTABLISHED_FACTS.md` #6). A second listener steals the port from the first
  and then reports the silence it caused. A file drop makes that impossible.
- The two apps are on different runtimes. RC Drag Manager is .NET Framework 4.8;
  `Portatree.Probe` is .NET 8. They cannot share a process or an assembly.
- The sniffer already writes capture logs, so replaying a recorded track day
  through the whole chain costs nothing and needs no hardware.

### The contract

The sniffer appends one JSON line per **completed pass** to a file in a folder
both apps can see. One object per line, newline delimited:

```json
{
  "capturedAt": "2026-09-07T13:42:10.412+10:00",
  "left":  { "reactionTime": 0.512, "sixtyFoot": 1.031, "elapsedTime": 7.482, "speed": 61.2 },
  "right": { "reactionTime": 0.488, "sixtyFoot": 1.009, "elapsedTime": 7.311, "speed": 62.8 },
  "raw": "<the source frame, verbatim>"
}
```

- Any field may be absent or null.
- `left` and `right` are the box's own lanes. The sniffer does not know driver
  names and must never guess them.
- `raw` is mandatory. It is what lets a bad parse be fixed after the fact.

RC Drag Manager reads with a `FileSystemWatcher` plus a tail read, holds the
newest line as the pending time, and never writes to the file.

The folder path is a setting on the Settings window, defaulting to a
`Timing` folder beside the app's database. Feed off by default: an event with no
timing box configured behaves exactly as it does today.

---

## 7. What gets built, and in what order

| # | Work | Depends on a track day? |
|---|---|---|
| 1 | `RaceTimingResult` on `RaceSession`, plus tests | No |
| 2 | Feed reader: watch folder, hold newest pass, expire stale | No |
| 3 | Attach on `SubmitWinner`, mapped by lane | No |
| 4 | Results form columns and live refresh | No |
| 5 | Edit and clear a timing row | No |
| 6 | Times on `LiveMatchDto` and the live scoreboard | No |
| 7 | **The sniffer's post-race parser** | **Yes** |

Items 1 to 6 are testable end to end today by writing lines into the folder by
hand. Item 7 is the only part that needs a car to go down the track.

---

## 8. Still open

- **The post-race frame layout.** Fact A in the Portatree repo. Needs one real
  pass.
- **Whether both lanes always arrive in one frame.** The sign protocol writes
  each alternating value to its own text slot (`A0`/`A1`/`A2`/`A3`), so the
  sniffer may have to assemble a pass from several frames before it emits a
  line. That is the sniffer's problem, not this app's: the contract above is one
  line per completed pass either way.
- **Which feed wins.** The LED sign line is live and does not take the tree from
  the operator. The printer line is after the fact but carries a full slip. If
  both work, the printer slip is the richer record and the sign line is the
  faster one. Decide once there is data from either.
