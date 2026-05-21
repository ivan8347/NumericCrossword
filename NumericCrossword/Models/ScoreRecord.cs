using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumericCrossword.Models
{
    public class ScoreRecord
    {
        public string Name { get; set; }
        public string Difficulty { get; set; }
        public TimeSpan Time {  get; set; }
        public DateTime Date { get; set; }
        public int Score { get; set; }


    }
}
