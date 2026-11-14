using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumericCrossword.Models
{
    public class LocalScoreRecord
    {
        public LocalScoreRecord(string playerName, string difficulty, TimeSpan time, int score, DateTime date)
        {
            PlayerName = playerName;
            Difficulty = difficulty;
            Time = time;
            Score = score;
            Date = date;
        }

        public string PlayerName { get; set; }      // <-- добавили ?
        public int Score { get; set; }
        public TimeSpan Time { get; set; }
        public string Difficulty { get; set; }      // <-- добавили ?
        public DateTime Date { get; set; }
    }
}



