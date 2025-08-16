using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;    // only if returning VM rows
using RCDragManagerProd.Logging;      // Logger


namespace RCDragManagerProd.RaceEngines
{
    public static class RaceEngineFactory
    {
        public static IRaceEngine Create(string raceType)
        {
            if (string.IsNullOrWhiteSpace(raceType))
                throw new ArgumentException("Race type cannot be blank.", nameof(raceType));

            string cleanType = raceType.Trim().ToLowerInvariant();
            Logger.Log($"[ENGINE FACTORY] Requested race type: '{cleanType}'");

            switch (cleanType)
            {
                case "pro ladder":
                case "nhra pro ladder":
                    Logger.Log("[ENGINE FACTORY] Creating ProLadderEngineAdapter");
                    return new ProLadderEngineAdapter();

                case "round robin":
                    Logger.Log("[ENGINE FACTORY] Creating RoundRobinEngineAdapter");
                    return new RoundRobinEngineAdapter();

                case "random":
                case "randomized":
                case "random draw":
                    Logger.Log("[ENGINE FACTORY] Creating RandomEngineAdapter");
                    return new RandomEngineAdapter();

                default:
                    Logger.Log($"[ENGINE FACTORY] Unknown race type '{cleanType}'");
                    throw new ArgumentException($"Unknown race type “{raceType}”.", nameof(raceType));
            }
        }
    }
}
