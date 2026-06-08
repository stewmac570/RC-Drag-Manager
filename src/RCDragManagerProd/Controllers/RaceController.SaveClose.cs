// RaceController.SaveClose.cs
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        // Save Progress and Close Race are DISTINCT operator intents (issue #255). They
        // are deliberately NOT interchangeable, and the split exists so an operator can
        // always tell whether an interrupted event is safely resumable:
        //
        //   • SaveProgress() captures a resumable checkpoint of an in-progress event so it
        //     can be reopened later — rain delay, power loss, a paused meet. The race stays
        //     OPEN: IsClosed is cleared and a full resume snapshot is written (see
        //     SaveSession / CaptureResumeSnapshot, issue #294).
        //
        //   • CloseRace() records that the event is FINISHED and the operator is done with
        //     this console. Per #255 it does not, by itself, imply saving a resumable
        //     checkpoint — it only marks completion (IsClosed = true).

        /// <summary>
        /// Persists a resumable checkpoint of the current in-progress event. The race
        /// remains open and can be restored later via <see cref="RestoreFromSave"/>.
        /// </summary>
        public void SaveProgress()
        {
            SaveSession();
            if (_session != null) _session.IsClosed = false;
            Logger.Log("[SAVE] SaveProgress — resumable checkpoint captured (race remains open).");
        }

        /// <summary>
        /// Marks the event finished (operator is done with this console). Unlike
        /// <see cref="SaveProgress"/> this does not capture a resumable in-progress
        /// checkpoint; it only records completion.
        /// </summary>
        public void CloseRace()
        {
            if (_session == null)
            {
                Logger.Log("[CLOSE] CloseRace — no active session.");
                return;
            }

            _session.IsClosed = true;
            Logger.Log("[CLOSE] CloseRace — event marked complete (not a resumable checkpoint).");
        }
    }
}
