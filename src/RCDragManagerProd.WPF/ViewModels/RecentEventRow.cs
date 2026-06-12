using System;

namespace RCDragManagerProd.WPF.ViewModels
{
    public sealed class RecentEventRow
    {
        public int Id { get; set; }
        public bool IsMultiClass { get; set; }
        public string EventName { get; set; }
        public string SubText { get; set; }
        public DateTime EventDate { get; set; }
    }
}
