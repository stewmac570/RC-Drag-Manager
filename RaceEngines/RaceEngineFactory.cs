// ==========================================================================
// RaceEngineFactory.cs
// RC Drag Manager  –  maps the user-selected race type string to the correct
// IRaceEngine adapter.  Keeps creation logic out of the controller/UI.
// ==========================================================================

using System;

namespace RCDragManagerProd.RaceEngines
{
    /// <summary>
    /// Simple switchboard that chooses the correct <see cref="IRaceEngine"/>
    /// implementation based on a human-friendly race-type string supplied by
    /// the UI or a config file.
    /// </summary>
    public static class RaceEngineFactory
    {
        /// <param name="raceType">
        /// For example: “Pro Ladder”, “Round Robin”, “Random”.
        /// Case-insensitive; trimmed; null/empty not allowed.
        /// </param>
        public static IRaceEngine Create(string raceType)
        {
            if (string.IsNullOrWhiteSpace(raceType))
                throw new ArgumentException("Race type cannot be blank.", nameof(raceType));

            switch (raceType.Trim().ToLowerInvariant())
            {
                case "pro ladder":
                case "nhra pro ladder":
                    return new ProLadderEngineAdapter();

                // The other adapters will arrive in the next steps.
                // For now we fall through to the default error.
                // case "round robin":
                //     return new RoundRobinEngineAdapter();
                // case "random":
                //     return new RandomEngineAdapter();

                default:
                    throw new ArgumentException(
                        $"Unknown race type “{raceType}”. " +
                        "Currently only “Pro Ladder” is implemented.",
                        nameof(raceType));
            }
        }
    }
}
