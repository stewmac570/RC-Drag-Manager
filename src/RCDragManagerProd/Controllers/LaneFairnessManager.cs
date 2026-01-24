using System;
using System.Collections.Generic;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    internal sealed class LaneFairnessManager
    {
        private enum Lane
        {
            Left = 0,
            Right = 1
        }

        private sealed class DriverLaneStats
        {
            public int LeftCount;
            public int RightCount;
            public Lane? LastLane;
        }

        private sealed class MatchLaneDecision
        {
            public bool Swap;          // true = show driver2 on left, driver1 on right
            public int Driver1Id;
            public int Driver2Id;
            public bool Applied;       // lane counts applied once
        }

        private readonly Dictionary<int, DriverLaneStats> statsByDriverId;
        private readonly Dictionary<string, MatchLaneDecision> decisionByKey;

        public LaneFairnessManager()
        {
            statsByDriverId = new Dictionary<int, DriverLaneStats>();
            decisionByKey = new Dictionary<string, MatchLaneDecision>();
        }

        public void Reset()
        {
            Logger.Log("[LANE] Reset lane fairness stats.");
            statsByDriverId.Clear();
            decisionByKey.Clear();
        }

        // key MUST be unique for the pairing instance (round+matchId+drivers)
        // counts are applied ONCE the first time this key is seen
        public bool ShouldSwap(string key, int driver1Id, int driver2Id)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "NO_KEY_" + driver1Id + "_" + driver2Id;
            }

            MatchLaneDecision existing;

            if (decisionByKey.TryGetValue(key, out existing))
            {
                return existing.Swap;
            }

            DriverLaneStats s1;
            DriverLaneStats s2;

            s1 = GetOrCreate(driver1Id);
            s2 = GetOrCreate(driver2Id);

            // OPTION A (no swap): d1=Left, d2=Right
            int scoreNoSwap;
            scoreNoSwap = ScoreAssignment(s1, Lane.Left) + ScoreAssignment(s2, Lane.Right);

            // OPTION B (swap): d2=Left, d1=Right
            int scoreSwap;
            scoreSwap = ScoreAssignment(s2, Lane.Left) + ScoreAssignment(s1, Lane.Right);

            bool swap;

            if (scoreSwap < scoreNoSwap)
            {
                swap = true;
            }
            else if (scoreNoSwap < scoreSwap)
            {
                swap = false;
            }
            else
            {
                // deterministic tie-breaker (stable)
                // this prevents flickering and keeps results consistent
                swap = (driver2Id < driver1Id);
            }

            MatchLaneDecision d;

            d = new MatchLaneDecision();
            d.Swap = swap;
            d.Driver1Id = driver1Id;
            d.Driver2Id = driver2Id;
            d.Applied = false;

            decisionByKey[key] = d;

            ApplyOnce(key);

            Logger.Log(
                "[LANE] Key='" + key + "' swap=" + swap +
                " (D1=" + driver1Id + ", D2=" + driver2Id + ")" +
                " scoreNoSwap=" + scoreNoSwap + " scoreSwap=" + scoreSwap
            );

            return swap;
        }

        // Lower score is better.
        // Penalize:
        //  - repeating the last lane (big penalty)
        //  - increasing imbalance (small penalty)
        private int ScoreAssignment(DriverLaneStats s, Lane laneAssigned)
        {
            int score;

            score = 0;

            // Big penalty if they'd repeat same lane as last time
            if (s.LastLane.HasValue && s.LastLane.Value == laneAssigned)
            {
                score += 100;
            }

            // Predict new counts after assignment
            int newLeft;
            int newRight;

            newLeft = s.LeftCount;
            newRight = s.RightCount;

            if (laneAssigned == Lane.Left)
            {
                newLeft = newLeft + 1;
            }
            else
            {
                newRight = newRight + 1;
            }

            // Small penalty for imbalance (absolute difference)
            int diff;
            diff = newLeft - newRight;
            if (diff < 0) diff = -diff;

            score += diff;

            return score;
        }

        private void ApplyOnce(string key)
        {
            MatchLaneDecision d;

            if (!decisionByKey.TryGetValue(key, out d))
            {
                return;
            }

            if (d.Applied)
            {
                return;
            }

            if (!d.Swap)
            {
                ApplyLaneUsage(d.Driver1Id, Lane.Left);
                ApplyLaneUsage(d.Driver2Id, Lane.Right);
            }
            else
            {
                ApplyLaneUsage(d.Driver2Id, Lane.Left);
                ApplyLaneUsage(d.Driver1Id, Lane.Right);
            }

            d.Applied = true;
            decisionByKey[key] = d;
        }

        private DriverLaneStats GetOrCreate(int driverId)
        {
            DriverLaneStats s;

            if (!statsByDriverId.TryGetValue(driverId, out s))
            {
                s = new DriverLaneStats();
                statsByDriverId[driverId] = s;
            }

            return s;
        }

        private void ApplyLaneUsage(int driverId, Lane lane)
        {
            DriverLaneStats s;

            s = GetOrCreate(driverId);

            if (lane == Lane.Left)
            {
                s.LeftCount++;
                s.LastLane = Lane.Left;
            }
            else
            {
                s.RightCount++;
                s.LastLane = Lane.Right;
            }

            Logger.Log(
                "[LANE] DriverId=" + driverId + " → " + lane +
                " (Left=" + s.LeftCount + ", Right=" + s.RightCount + ")"
            );
        }
    }
}
