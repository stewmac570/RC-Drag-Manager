using System.Collections.Generic;

namespace RCDragManagerProd.ViewModels
{
    public sealed class ClassCompletionPresentation
    {
        public string EventName { get; set; }
        public string ClassName { get; set; }
        public string ChampionName { get; set; }
        public string RunnerUpName { get; set; }
        public string ThirdLabel { get; set; }
        public string ThirdName { get; set; }
        public bool HasThird { get; set; }
        public List<RaceResultsStandingRow> OtherFinishers { get; set; } =
            new List<RaceResultsStandingRow>();
        public List<RaceResultsListRow> FinalsResults { get; set; } =
            new List<RaceResultsListRow>();
    }
}
