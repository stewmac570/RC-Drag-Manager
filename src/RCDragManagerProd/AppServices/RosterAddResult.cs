using RCDragManagerProd.Domain;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Outcome of one <see cref="SessionRosterService.AddOrUpdate"/> call. The
    /// add-driver dialog (#417) keeps itself open for bulk entry, so it needs to
    /// tell "rejected, show the message and stay put" from "accepted, clear the
    /// fields and take the next one".
    /// </summary>
    public sealed class RosterAddResult
    {
        private RosterAddResult(bool success, string error, Driver driver, bool wasExisting)
        {
            Success = success;
            Error = error;
            Driver = driver;
            WasExisting = wasExisting;
        }

        public bool Success { get; }

        /// <summary>Operator-facing reason the entry was rejected; null on success.</summary>
        public string Error { get; }

        /// <summary>The driver added, or the existing one that was updated.</summary>
        public Driver Driver { get; }

        /// <summary>True when the name matched a driver already on the roster.</summary>
        public bool WasExisting { get; }

        public static RosterAddResult Failed(string error) =>
            new RosterAddResult(false, error, null, false);

        public static RosterAddResult Added(Driver driver) =>
            new RosterAddResult(true, null, driver, false);

        public static RosterAddResult Updated(Driver driver) =>
            new RosterAddResult(true, null, driver, true);
    }
}
