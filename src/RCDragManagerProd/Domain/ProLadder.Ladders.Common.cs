// ProLadder.Ladders.Common.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        public static List<LadderMatch> GetLadder(int fieldSize)
        {
            switch (fieldSize)
            {
                case 3: return GetLadder3();
                case 4: return GetLadder4();
                case 5: return GetLadder5();
                case 6: return GetLadder6();
                case 7: return GetLadder7();
                case 8: return GetLadder8();
                case 9: return GetLadder9();
                case 10: return GetLadder10();
                case 11: return GetLadder11();
                case 12: return GetLadder12();
                case 13: return GetLadder13();
                case 14: return GetLadder14();
                case 15: return GetLadder15();
                case 16: return GetLadder16();
                case 17: return GetLadder17();
                case 18: return GetLadder18();
                case 19: return GetLadder19();
                case 20: return GetLadder20();
                case 21: return GetLadder21();
                case 22: return GetLadder22();
                case 23: return GetLadder23();
                case 24: return GetLadder24();
                default: return new List<LadderMatch>();
            }
        }
    }
}
