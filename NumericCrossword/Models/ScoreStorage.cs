using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NumericCrossword.Models;

namespace NumericCrossword.Core
{
    public static class ScoreStorage
    {
        private static string FilePath = "scores.json";

        public static List<ScoreRecord> Load()
        {
            if (!File.Exists(FilePath))
                return new List<ScoreRecord>();

            string json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<List<ScoreRecord>>(json);
        }

        public static void Save(List<ScoreRecord> scores)
        {
            string json = JsonConvert.SerializeObject(scores, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        public static void AddRecord(ScoreRecord record)
        {
            var list = Load();
            list.Add(record);

            list.Sort((a, b) => a.Time.CompareTo(b.Time));

            if (list.Count > 5)
                list = list.GetRange(0, 5);

            Save(list);
        }
    }
}
