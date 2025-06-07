using System.Collections.Generic;

namespace RCDragManagerProd
{
    public class Driver
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double? QualTime { get; set; }
        public string Notes { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int EventsEntered { get; set; }
        public int EventsWon { get; set; }
        public int? Seed { get; set; }
        public string State { get; set; }  // ✅ NEW FIELD
        public List<Car> Cars { get; set; } = new List<Car>();
    }
}
