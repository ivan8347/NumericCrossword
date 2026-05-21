using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumericCrossword.Models
{
    public class PlayerProfile
    {
        public string Name { get; set; }
        public string Avatar { get; set; }
        public int TotalScore { get; set; } = 0;
    }
}
