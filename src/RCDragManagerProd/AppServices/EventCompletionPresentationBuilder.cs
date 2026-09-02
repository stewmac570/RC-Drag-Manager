using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Builds the end-of-event board from what the classes actually saved.
    ///
    /// Reads each class's <see cref="RaceResultsArchive"/> rather than the live
    /// completion summaries the window happened to collect, so the board is correct
    /// for a resumed event where some classes finished in an earlier sitting.
    /// </summary>
    public static class EventCompletionPresentationBuilder
    {
        private const string NotRecorded = "Not recorded";

        public static EventCompletionPresentation Build(MultiClassEvent multiEvent)
        {
            var sessions = (multiEvent?.ClassSessions ?? new List<RaceSession>())
                .Where(s => s != null)
                .ToList();

            var result = new EventCompletionPresentation
            {
                EventName = string.IsNullOrWhiteSpace(multiEvent?.EventName)
                    ? "Event complete"
                    : multiEvent.EventName,
                SubHeading = SubHeading(multiEvent?.EventDate, sessions.Count),
                Classes = sessions.Select(ToRow).ToList()
            };

            result.CopyText = BuildCopyText(result);
            return result;
        }

        private static EventCompletionClassRow ToRow(RaceSession session, int index)
        {
            var archive = session.ResultsArchive;
            var champion = archive?.ChampionName;

            return new EventCompletionClassRow
            {
                ClassName = string.IsNullOrWhiteSpace(session.ClassType)
                    ? $"Class {index + 1}"
                    : session.ClassType,
                ChampionName = string.IsNullOrWhiteSpace(champion) ? NotRecorded : champion,
                RunnerUpName = string.IsNullOrWhiteSpace(archive?.RunnerUpName)
                    ? NotRecorded
                    : archive.RunnerUpName,
                HasResult = !string.IsNullOrWhiteSpace(champion)
            };
        }

        private static string SubHeading(DateTime? date, int classCount)
        {
            var classes = classCount == 1 ? "1 class" : $"{classCount} classes";
            return date.HasValue ? $"{date.Value:ddd d MMM yyyy} · {classes}" : classes;
        }

        /// <summary>Plain text for the Copy button — results as you would post them.</summary>
        private static string BuildCopyText(EventCompletionPresentation view)
        {
            var sb = new StringBuilder();
            sb.AppendLine(view.EventName);
            sb.AppendLine(view.SubHeading);
            sb.AppendLine();

            foreach (var c in view.Classes)
            {
                sb.AppendLine(c.ClassName);
                if (c.HasResult)
                {
                    sb.AppendLine($"  Champion:  {c.ChampionName}");
                    sb.AppendLine($"  Runner-up: {c.RunnerUpName}");
                }
                else sb.AppendLine("  No result recorded");
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}
