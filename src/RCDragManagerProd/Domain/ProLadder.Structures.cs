// ProLadder.Structures.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        public class LadderMatch
        {
            public int MatchId { get; set; }
            public int? Seed1 { get; set; }
            public int? Seed2 { get; set; }
            public int? FromMatch1 { get; set; }
            public int? FromMatch2 { get; set; }
            public string RoundLabel { get; set; }
        }
    }
}
