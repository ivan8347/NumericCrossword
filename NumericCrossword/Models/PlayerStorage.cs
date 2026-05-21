using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NumericCrossword.Models;

namespace NumericCrossword.Models
{
    public static class PlayerStorage
    {
        private static string FilePath = "players.json";

        public static List<PlayerProfile> Load()
        {
            if (!File.Exists(FilePath))
                return new List<PlayerProfile>();

            return JsonConvert.DeserializeObject<List<PlayerProfile>>(
                File.ReadAllText(FilePath)
            );
        }

        public static void Save(List<PlayerProfile> players)
        {
            File.WriteAllText(FilePath,
                JsonConvert.SerializeObject(players, Formatting.Indented));
        }
    }
}
