using System;
using System.Collections.Generic;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RaceEngines;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        private static string EngineTypeName(IRaceEngine engine)
            => engine?.GetType().Name ?? "null";

        private static string NormalizeRoundForLog(string round)
            => string.IsNullOrWhiteSpace(round) ? "-" : RoundLabels.Normalize(round);

        private void LogEngineCall(IRaceEngine engine, string method, int? matchId = null, string round = null)
        {
            Logger.Log(
                "[EngineCall] " + EngineTypeName(engine) + " " + method +
                " matchId=" + (matchId?.ToString() ?? "-") +
                " round=" + NormalizeRoundForLog(round));
        }

        private IReadOnlyList<EngineMatch> EngineGetMatches(IRaceEngine engine, string round = null, int? matchId = null)
        {
            LogEngineCall(engine, "GetMatches", matchId, round);
            return engine?.GetMatches() ?? Array.Empty<EngineMatch>();
        }

        private IReadOnlyList<string> EngineGetRoundOrder(IRaceEngine engine, string round = null, int? matchId = null)
        {
            LogEngineCall(engine, "GetRoundOrder", matchId, round);
            return engine?.GetRoundOrder() ?? Array.Empty<string>();
        }

        private bool EngineHasWinner(IRaceEngine engine, int matchId, string round = null)
        {
            LogEngineCall(engine, "HasWinner", matchId, round);
            return engine != null && engine.HasWinner(matchId);
        }

        private void EngineSetWinner(IRaceEngine engine, int matchId, Driver winner, string round = null)
        {
            LogEngineCall(engine, "SetWinner", matchId, round);
            engine?.SetWinner(matchId, winner);
        }

        private void EngineLoadDrivers(IRaceEngine engine, List<Driver> drivers, string round = null)
        {
            LogEngineCall(engine, "LoadDrivers", null, round);
            engine?.LoadDrivers(drivers);
        }

        private void EngineGenerateBracket(IRaceEngine engine, string round = null)
        {
            LogEngineCall(engine, "GenerateBracket", null, round);
            engine?.GenerateBracket();
        }
    }
}
