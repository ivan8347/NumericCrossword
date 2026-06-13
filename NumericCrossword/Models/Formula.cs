using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumericCrossword.Models
{
    public class Formula
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public char Op { get; set; }

        public int Row { get; set; }
        public int Col { get; set; }
        public bool Horizontal { get; set; }
        public bool HideA { get; set;}
        public bool HideB { get; set; }
        public bool HideC { get; set; }
    }

}
