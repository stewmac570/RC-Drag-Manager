using System.Collections.Generic;
using System.Threading;
using System;

namespace RCDragManagerProd.Domain
{
    public class Driver
    {

        private static int _nextRuntimeId = 0;
        private int _id;   // backing field for the custom property

        public Driver()
        {
            // Assign a unique Id immediately if none supplied.
            _id = Interlocked.Increment(ref _nextRuntimeId);
        }

        public int Id
        {
            get => _id;
            set
            {
                if (value == 0)
                {
                    // Ignore attempts to reset to 0 once a real Id exists.
                    if (_id == 0)
                    {
                        _id = Interlocked.Increment(ref _nextRuntimeId);
                    }
                }
                else
                {
                    _id = value;   // honour explicit non-zero assignments
                }
            }
        }

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
