using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NumericCrossword.Models;

namespace NumericCrossword.Core
{
    public static class ScoreStorage
    {
        // Убираем прямое задание пути, используем метод для получения
        private static string FilePath => GetFilePath();

        public static List<LocalScoreRecord> Load()
        {
            if (!File.Exists(FilePath))
                return new List<LocalScoreRecord>();

            string json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<List<LocalScoreRecord>>(json) ?? new List<LocalScoreRecord>();
        }

        public static void Save(List<LocalScoreRecord> scores)
        {
            // Создаём директорию, если её нет
            string directory = Path.GetDirectoryName(FilePath);
            if (directory == null)
            {
                // Логика на случай, если директория не определена
                // Например, можно создать папку в корне или выбросить исключение
                directory = "."; // или другое значение по умолчанию
            }

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonConvert.SerializeObject(scores, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        public static void AddRecord(LocalScoreRecord record)
        {
            var list = Load();
            list.Add(record);

            // Сортируем по времени (от меньшего к большему)
            list.Sort((a, b) => a.Time.CompareTo(b.Time));

            // Оставляем только топ-5 рекордов
            if (list.Count > 5)
                list = list.GetRange(0, 5);

            Save(list);
        }

        public static void Clear()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        private static string GetFilePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NumericCrossword",
                "scores.json"
            );
        }
    }
}
