using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NumericCrossword.Models;

namespace NumericCrossword.Models
{
    public static class PlayerStorage
    {
        // Генерируем правильный путь: Документы -> NumericCrossword -> players.json
        private static string FilePath
        {
            get
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string gameFolder = Path.Combine(documentsPath, "NumericCrossword");

                // Гарантированно создаем папку, если её нет (ошибки не будет, если папка уже есть)
                if (!Directory.Exists(gameFolder))
                {
                    Directory.CreateDirectory(gameFolder);
                }

                return Path.Combine(gameFolder, "players.json");
            }
        }

        public static List<PlayerProfile> Load()
        {
            // Проверяем существование файла по НОВОМУ пути
            if (!File.Exists(FilePath))
                return new List<PlayerProfile>();

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<List<PlayerProfile>>(json) ?? new List<PlayerProfile>();
            }
            catch
            {
                // Если файл битый (невалидный JSON) — возвращаем пустой список, а не вылетаем
                return new List<PlayerProfile>();
            }
        }

        public static void Save(List<PlayerProfile> players)
        {
            try
            {
                // Пишем по НОВОМУ пути
                string json = JsonConvert.SerializeObject(players, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (UnauthorizedAccessException ex)
            {
                // На случай совсем странных блокировок антивируса или политик безопасности
                System.Windows.MessageBox.Show($"Не удалось сохранить профиль игрока. Ошибка: {ex.Message}", "Ошибка сохранения");
            }
        }
    }
}
