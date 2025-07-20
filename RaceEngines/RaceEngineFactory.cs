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
    // RaceEngineFactory.cs
    public static class RaceEngineFactory
    {
        public static IRaceEngine Create(string raceType)
        {
            if (string.IsNullOrWhiteSpace(raceType))
                throw new ArgumentException("Race type cannot be blank.", nameof(raceType));

            switch (raceType.Trim().ToLowerInvariant())
            {
                case "pro ladder":
                case "nhra pro ladder":
                    return new ProLadderEngineAdapter();

                case "round robin":
                    return new RoundRobinEngineAdapter();

                case "random":
                case "randomized":
                case "random draw":
                    return new RandomEngineAdapter();

                default:
                    throw new ArgumentException(
                        $"Unknown race type “{raceType}”.",
                        nameof(raceType));
            }
        }
    }

}
