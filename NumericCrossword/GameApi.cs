using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace NumericCrossword.Core
{
    public class GameInfo
    {
        public string GameId { get; set; }
        public int Seed { get; set; }
        public string Difficulty { get; set; }
    }

    public class GameResult
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int TimeSeconds { get; set; }
    }

    public static class GameApi
    {
        private static readonly HttpClient http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7162/") // адрес твоего сервера
        };

        // Создать игру
        public static async Task<GameInfo> CreateGame(string difficulty)
        {
            var response = await http.PostAsJsonAsync("game/create", difficulty);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameInfo>();
        }

        // Получить игру по ID
        public static async Task<GameInfo> GetGame(string gameId)
        {
            var response = await http.GetAsync($"game/{gameId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameInfo>();
        }

        // Отправить результат
        public static async Task SendResult(string gameId, string playerName, int score, int timeSeconds)
        {
            var result = new GameResult
            {
                PlayerName = playerName,
                Score = score,
                TimeSeconds = timeSeconds
            };

            var response = await http.PostAsJsonAsync($"game/{gameId}/result", result);
            response.EnsureSuccessStatusCode();
        }

        // Получить результаты
        public static async Task<List<GameResult>> GetResults(string gameId)
        {
            var response = await http.GetAsync($"game/{gameId}/results");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<GameResult>>();
        }
    }
}
