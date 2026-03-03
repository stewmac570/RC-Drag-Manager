using System;
using System.Text.RegularExpressions;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Domain
{
    public static class RoundLabels
    {
        private static readonly Regex WinnersRoundRx = new Regex(@"^R(\d+)$", RegexOptions.IgnoreCase);
        private static readonly Regex LosersRoundRx = new Regex(@"^LB-R(\d+)$", RegexOptions.IgnoreCase);
        private static readonly Regex RoundRobinRoundRx = new Regex(@"^RR(\d+)$", RegexOptions.IgnoreCase);
        private static readonly Regex LegacyLosersRoundRx = new Regex(@"^LOSERS BRACKET R(\d+)$", RegexOptions.IgnoreCase);

        public static string Normalize(string label)
        {
            var raw = (label ?? string.Empty).Trim();
            if (raw.Length == 0) return raw;

            var mRr = RoundRobinRoundRx.Match(raw);
            if (mRr.Success)
            {
                var canonical = "RR" + mRr.Groups[1].Value;
                LogConversion(raw, canonical);
                return canonical;
            }

            var mLb = LosersRoundRx.Match(raw);
            if (mLb.Success)
            {
                var canonical = "LB-R" + mLb.Groups[1].Value;
                LogConversion(raw, canonical);
                return canonical;
            }

            var mLegacyLb = LegacyLosersRoundRx.Match(raw);
            if (mLegacyLb.Success)
            {
                var canonical = "LB-R" + mLegacyLb.Groups[1].Value;
                LogConversion(raw, canonical);
                return canonical;
            }

            if (string.Equals(raw, "LOSERS BRACKET FINAL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "LBF", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "LB-F", StringComparison.OrdinalIgnoreCase))
            {
                LogConversion(raw, "LB-F");
                return "LB-F";
            }

            if (string.Equals(raw, "SF", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("SEMI", StringComparison.OrdinalIgnoreCase))
            {
                LogConversion(raw, "SF");
                return "SF";
            }

            if (string.Equals(raw, "F", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "FINAL", StringComparison.OrdinalIgnoreCase))
            {
                LogConversion(raw, "F");
                return "F";
            }

            var mW = WinnersRoundRx.Match(raw);
            if (mW.Success)
            {
                var canonical = "R" + mW.Groups[1].Value;
                LogConversion(raw, canonical);
                return canonical;
            }

            Logger.Log($"[RoundLabels][WARN] Unknown round label '{raw}'");
            return raw;
        }

        public static int Compare(string a, string b)
        {
            var na = Normalize(a);
            var nb = Normalize(b);

            int ka = SortKey(na);
            int kb = SortKey(nb);
            if (ka != kb) return ka.CompareTo(kb);

            return string.Compare(na, nb, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryParse(string label, out (bool IsLosers, int? RoundNumber, bool IsSemiFinal, bool IsFinal) parsed)
        {
            var n = Normalize(label);

            var mLb = LosersRoundRx.Match(n);
            if (mLb.Success)
            {
                parsed = (true, ParseInt(mLb.Groups[1].Value), false, false);
                return true;
            }

            var mRr = RoundRobinRoundRx.Match(n);
            if (mRr.Success)
            {
                parsed = (false, ParseInt(mRr.Groups[1].Value), false, false);
                return true;
            }

            var mW = WinnersRoundRx.Match(n);
            if (mW.Success)
            {
                parsed = (false, ParseInt(mW.Groups[1].Value), false, false);
                return true;
            }

            if (string.Equals(n, "SF", StringComparison.OrdinalIgnoreCase))
            {
                parsed = (false, null, true, false);
                return true;
            }

            if (string.Equals(n, "F", StringComparison.OrdinalIgnoreCase))
            {
                parsed = (false, null, false, true);
                return true;
            }

            if (string.Equals(n, "LB-F", StringComparison.OrdinalIgnoreCase))
            {
                parsed = (true, null, false, true);
                return true;
            }

            parsed = default;
            return false;
        }

        private static int SortKey(string normalized)
        {
            if (normalized == null) return 10000;

            var mRr = RoundRobinRoundRx.Match(normalized);
            if (mRr.Success) return 100 + ParseInt(mRr.Groups[1].Value);

            var mW = WinnersRoundRx.Match(normalized);
            if (mW.Success) return 200 + ParseInt(mW.Groups[1].Value);

            if (string.Equals(normalized, "SF", StringComparison.OrdinalIgnoreCase)) return 290;
            if (string.Equals(normalized, "F", StringComparison.OrdinalIgnoreCase)) return 299;

            var mLb = LosersRoundRx.Match(normalized);
            if (mLb.Success) return 400 + ParseInt(mLb.Groups[1].Value);

            if (string.Equals(normalized, "LB-F", StringComparison.OrdinalIgnoreCase)) return 499;

            return 10000;
        }

        private static int ParseInt(string s)
        {
            int.TryParse(s, out var n);
            return n;
        }

        private static void LogConversion(string original, string canonical)
        {
            if (!string.Equals(original, canonical, StringComparison.Ordinal))
                Logger.Log($"[RoundLabels] Normalize '{original}' -> '{canonical}'");
        }
    }
}
