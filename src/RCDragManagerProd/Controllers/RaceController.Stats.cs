// RaceController.Stats.cs
using System;
using System.Collections.Generic;

using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Controllers
{
    public sealed partial class RaceController
    {
        public void PersistTournamentStats(RaceSummary summary, IEnumerable<Driver> participants, string connectionString)
        {
            try
            {
                var repo = new DriverRepository(connectionString);

                if (participants != null)
                {
                    foreach (var d in participants)
                    {
                        if (d?.Id > 0)
                        {
                            var db = repo.GetDriverById(d.Id);
                            if (db != null)
                            {
                                db.EventsEntered += 1;
                                repo.UpdateDriver(db);
                                Logger.Log($"[STATS] +EventsEntered → #{db.Id} {db.Name}: {db.EventsEntered}");
                            }
                            else
                            {
                                Logger.Log($"[STATS] +EventsEntered skipped — driver Id={d.Id} ('{d.Name}') not found in DB (quick/local session)");
                            }
                        }
                    }
                }

                var winnerId = summary.Winner?.Id ?? 0;
                if (winnerId > 0)
                {
                    var wdb = repo.GetDriverById(winnerId);
                    if (wdb != null)
                    {
                        wdb.EventsWon += 1;
                        repo.UpdateDriver(wdb);
                        Logger.Log($"[STATS] +EventsWon → #{wdb.Id} {wdb.Name}: {wdb.EventsWon}");
                    }
                    else
                    {
                        Logger.Log($"[STATS] +EventsWon skipped — winner Id={winnerId} ('{summary.Winner?.Name}') not found in DB (quick/local session)");
                    }
                }

                if (summary.MatchResults != null)
                {
                    foreach (var (wId, lId) in summary.MatchResults)
                    {
                        repo.IncrementWinsAndLosses(wId, lId);
                        Logger.Log($"[STATS] +TotalWins/TotalLosses → winner={wId}, loser={lId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] Failed to bump event stats: {ex}");
            }
        }

        public void RecomputeEventsWon(IEnumerable<Driver> participants, string connectionString)
        {
            try
            {
                var repo = new DriverRepository(connectionString);
                if (participants != null)
                {
                    foreach (var d in participants)
                    {
                        if (d?.Id > 0)
                        {
                            var db = repo.GetDriverById(d.Id);
                            if (db != null)
                            {
                                db.EventsWon = repo.ComputeEventsWonFromSavedSessions(d.Id);
                                repo.UpdateDriver(db);
                                Logger.Log($"[STATS] Recompute EventsWon → #{db.Id} {db.Name}: {db.EventsWon}");
                            }
                            else
                            {
                                Logger.Log($"[STATS] Recompute EventsWon skipped — driver Id={d.Id} ('{d.Name}') not found in DB (quick/local session)");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] Recompute EventsWon failed: {ex}");
            }
        }

        public void PersistMatchStats(Driver winner, Driver loser, string connectionString)
        {
            try
            {
                if (winner == null || loser == null)
                {
                    Logger.Log("[STATS] Skip: winner/loser null.");
                    return;
                }

                var repo = new DriverRepository(connectionString);
                var wdb = repo.GetDriverById(winner.Id);
                var ldb = repo.GetDriverById(loser.Id);

                if (wdb == null || ldb == null)
                {
                    Logger.Log($"[STATS] Skip: DB lookup failed (winnerId={winner.Id}->{(wdb != null)}, loserId={loser.Id}->{(ldb != null)}).");
                    return;
                }

                wdb.TotalWins += 1;
                ldb.TotalLosses += 1;
                repo.UpdateDriver(wdb);
                repo.UpdateDriver(ldb);

                Logger.Log($"[STATS] +Win {wdb.Name} / +Loss {ldb.Name}  (W={wdb.TotalWins}, L={ldb.TotalLosses})");
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] PersistMatchStats failed: {ex}");
            }
        }

        public void PersistEventWon(Driver winner, string connectionString)
        {
            try
            {
                if (winner == null) return;

                var repo = new DriverRepository(connectionString);
                var db = repo.GetDriverById(winner.Id);

                if (db != null)
                {
                    db.EventsWon += 1;
                    repo.UpdateDriver(db);
                    Logger.Log($"[STATS] +EventsWon (Final) → #{db.Id} {db.Name}: {db.EventsWon}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] PersistEventWon failed: {ex}");
            }
        }
    }
}
