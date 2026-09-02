using System.Collections.Generic;

namespace RCDragManagerProd.ViewModels
{
    /// <summary>
    /// The end-of-event board: one row per class, each with its champion and
    /// runner-up. Replaces the ASCII text block the multi-class window used to dump
    /// into a plain text dialog.
    /// </summary>
    public sealed class EventCompletionPresentation
    {
        public string EventName { get; set; }

        /// <summary>Date and class count, e.g. "Wed 2 Sep 2026 · 3 classes".</summary>
        public string SubHeading { get; set; }

        public List<EventCompletionClassRow> Classes { get; set; } = new List<EventCompletionClassRow>();

        /// <summary>
        /// A plain-text rendering of the same results, for the Copy button — the one
        /// genuinely useful thing about the old text dialog was pasting results into a
        /// club post.
        /// </summary>
        public string CopyText { get; set; }
    }

    /// <summary>One class's result on the end-of-event board.</summary>
    public sealed class EventCompletionClassRow
    {
        public string ClassName { get; set; }
        public string ChampionName { get; set; }
        public string RunnerUpName { get; set; }

        /// <summary>False when the class finished without a recorded champion.</summary>
        public bool HasResult { get; set; }
    }
}
