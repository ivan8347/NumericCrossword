
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumericCrossword.Core
{
    // DTOs (можно вынести в отдельный файл, но для удобства оставил здесь)
    public class GameInfo
    {
        public string GameId { get; set; }
        public int Seed { get; set; }
        public string Creator { get; set; }
        public string Difficulty { get; set; }
        public string Status { get; set; }
        public List<string> Players { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class GameItem
    {
        public string GameId { get; set; }
        public string Creator { get; set; }
        public List<string> Players { get; set; }
        public string Status { get; set; }
        public string Difficulty { get; set; }
    }

    public class GameResult
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int TimeSeconds { get; set; }
    }

    public class ScoreRecord
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int TimeSeconds { get; set; }
        public string Difficulty { get; set; }
        public DateTime Date { get; set; }
    }

    public class ResultResponse
    {
        public bool Deleted { get; set; }
    }

    public class GameStatusDto
    {
        public bool IsCompleted { get; set; }
    }

    public class ChatMessageDto
    {
        public string Player { get; set; }
        public string Text { get; set; }
        public DateTime Time { get; set; }
    }

    public static class GameApi
    {
        // Единый HttpClient для всех запросов
        private static readonly HttpClient http = new HttpClient
        {
            BaseAddress = new Uri("https://crosswordserver.onrender.com"),
            Timeout = TimeSpan.FromSeconds(10) // Увеличил таймаут до 10 сек
        };

        // Опционально: добавить заголовок User-Agent, чтобы сервер не блокировал запросы
        static GameApi()
        {
            http.DefaultRequestHeaders.Add("User-Agent", "NumericCrosswordClient/1.0");
        }

        public static async Task<List<GameItem>> GetGames()
        {
            try
            {
                var games = await http.GetFromJsonAsync<List<GameItem>>("games");
                return games ?? new List<GameItem>();
            }
            catch
            {
                return new List<GameItem>();
            }
        }

        public static async Task<GameInfo> CreateGame(string creatorName, string difficulty)
        {
            try
            {
                var body = new { creatorName, difficulty };
                var response = await http.PostAsJsonAsync("game/create", body);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GameInfo>();
            }
            catch
            {
                return null;
            }
        }

        public static async Task<GameInfo> JoinGame(string gameId, string playerName)
        {
            try
            {
                var body = new
                {
                    gameId,
                    playerName
                };

                var response = await http.PostAsJsonAsync("game/join", body);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<GameInfo>();
            }
            catch
            {
                return null;
            }
        }


        public static async Task<GameInfo> GetGameInfo(string gameId)
        {
            try
            {
                var response = await http.GetAsync($"game/info/{gameId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GameInfo>();
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> SendResult(string gameId, string playerName, int score, int timeSeconds)
        {
            try
            {
                var body = new { gameId, playerName, score, time = timeSeconds };
                var response = await http.PostAsJsonAsync("game/result", body);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ResultResponse>();
                return result?.Deleted ?? false;
            }
            catch
            {
                return false;
            }
        }

        private class GameStatusResponse
        {
            public bool isCompleted { get; set; }
        }

        public static async Task<GameStatusDto> GetGameStatus(string gameId)
        {
            try
            {
                var response = await http.GetAsync($"game/status/{gameId}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var statusResponse = JsonSerializer.Deserialize<GameStatusResponse>(json);

                if (statusResponse == null)
                    return new GameStatusDto { IsCompleted = false };

                return new GameStatusDto { IsCompleted = statusResponse.isCompleted };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetGameStatus error: " + ex.Message);
                return new GameStatusDto { IsCompleted = false };
            }
        }

        public static async Task<List<GameResult>> GetResults(string gameId)
        {
            try
            {
                var response = await http.GetAsync($"results/{gameId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<GameResult>>() ?? new List<GameResult>();
            }
            catch
            {
                return new List<GameResult>();
            }
        }

        public static async Task<string> RawGamesJson()
        {
            try
            {
                return await http.GetStringAsync("games");
            }
            catch
            {
                return "[]";
            }
        }


        public static async Task SendChatMessage(string player, string text)
        {
            var msg = new
            {
                Player = player,
                Text = text
            };

            try
            {
                await http.PostAsJsonAsync("/chat", msg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Chat send error: " + ex.Message);
            }
        }

        public static async Task<List<ChatMessageDto>> GetChatMessages()
        {
            try
            {
                var response = await http.GetAsync("/chat");
                if (!response.IsSuccessStatusCode)
                    return new List<ChatMessageDto>();

                return await response.Content.ReadFromJsonAsync<List<ChatMessageDto>>() ?? new List<ChatMessageDto>();
            }
            catch
            {
                return new List<ChatMessageDto>();
            }
        }

        /// <summary>
        /// Получает рейтинг с сервера. Если сервер недоступен — возвращает демо-данные (локальный рейтинг).
        /// Это позволяет окну рейтинга не быть пустым, даже если Render спит.
        /// </summary>
        public static async Task<List<ScoreRecord>> GetRating()
        {
            try
            {
                var ratings = await http.GetFromJsonAsync<List<ScoreRecord>>("rating");
                return ratings ?? new List<ScoreRecord>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Рейтинг не загружен (сеть/сервер): " + ex.Message);

                // Возвращаем локальный демо-рейтинг, если сеть недоступна
                return new List<ScoreRecord>
                {
                    new ScoreRecord { PlayerName = "Ты", Score = 100, TimeSeconds = 45, Difficulty = "Сложный", Date = DateTime.Now },
                    new ScoreRecord { PlayerName = "Бот Макс", Score = 95, TimeSeconds = 50, Difficulty = "Сложный", Date = DateTime.Now },
                    new ScoreRecord { PlayerName = "Новичок", Score = 50, TimeSeconds = 120, Difficulty = "Лёгкий", Date = DateTime.Now }
                };
            }
        }
    }
}
