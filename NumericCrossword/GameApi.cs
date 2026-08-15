//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Net.Http.Json;
//using System.Threading.Tasks;

//namespace NumericCrossword.Core
//{
//    // Модель полной информации об игре, которую возвращает сервер
//    public class GameInfo
//    {
//        // Уникальный ID игры, создаётся сервером
//        public string GameId { get; set; }

//        // Seed — число, которое гарантирует одинаковую генерацию кроссворда
//        public int Seed { get; set; }

//        // Имя создателя игры
//        public string Creator { get; set; }

//        // Выбранная сложность ("Лёгкий", "Средний", "Сложный")
//        public string Difficulty { get; set; }

//        // Статус игры: "waiting", "started", "finished"
//        public string Status { get; set; }

//        // Список игроков, подключённых к игре
//        public List<string> Players { get; set; }
//        public DateTime StartTime { get; set; }

//    }
//    // Модель игры в списке игр (короткая версия)
//    public class GameItem
//    {
//        public string GameId { get; set; }
//        public string Creator { get; set; }
//        public List<string> Players { get; set; }
//        public string Status { get; set; }
//        public string Difficulty { get; set; }
//    }

//    // Модель результата игрока
//    public class GameResult
//    {
//        public string PlayerName { get; set; }
//        public int Score { get; set; }
//        public int TimeSeconds { get; set; }
//    }

//    // Ответ сервера после отправки результата
//    public class ResultResponse
//    {
//        // Если true — сервер удалил игру (все игроки закончили)
//        public bool Deleted { get; set; }
//    }

//    // Основной класс API для общения с сервером
//    public static class GameApi
//    {
//        // HttpClient создаётся один раз на всё приложение
//        // Это важно: иначе будут утечки сокетов
//        private static readonly HttpClient http = new HttpClient
//        {
//            // Адрес твоего сервера
//            BaseAddress = new Uri("http://192.168.0.18:5270/"),


//            // Ограничиваем время ожидания ответа
//            Timeout = TimeSpan.FromSeconds(5)
//        };

//        // 1) Получить список всех игр
//        public static async Task<List<GameItem>> GetGames()
//        {
//            try
//            {
//                // GET /games — сервер возвращает список игр
//                var games = await http.GetFromJsonAsync<List<GameItem>>("games");

//                // Если сервер вернул null — возвращаем пустой список
//                return games ?? new List<GameItem>();
//            }
//            catch (Exception)
//            {
//                // В случае ошибки — возвращаем пустой список
//                // (можно показать MessageBox, если хочешь)
//                return new List<GameItem>();
//            }
//        }

//        // 2) Создать новую игру
//        public static async Task<GameInfo> CreateGame(string creatorName, string difficulty)
//        {
//            try
//            {
//                // Тело POST-запроса
//                var body = new
//                {
//                    creatorName,
//                    difficulty
//                };

//                // POST /game/create
//                var response = await http.PostAsJsonAsync("game/create", body);

//                // Если сервер вернул ошибку — бросит исключение
//                response.EnsureSuccessStatusCode();

//                // Читаем GameInfo из ответа
//                return await response.Content.ReadFromJsonAsync<GameInfo>();
//            }
//            catch (Exception)
//            {
//                return null; // ошибка — вернём null
//            }
//        }

//        // 3) Подключиться к существующей игре
//        public static async Task<GameInfo> JoinGame(string gameId, string playerName, string difficulty)
//        {
//            try
//            {
//                var body = new
//                {
//                    gameId,
//                    playerName,
//                    difficulty
//                };

//                var response = await http.PostAsJsonAsync("game/join", body);
//                response.EnsureSuccessStatusCode();

//                return await response.Content.ReadFromJsonAsync<GameInfo>();
//            }
//            catch (Exception)
//            {
//                return null;
//            }
//        }
//        public static async Task<GameInfo> GetGameInfo(string gameId)
//        {
//            try
//            {
//                // GET /game/info/{gameId}
//                var response = await http.GetAsync($"game/info/{gameId}");
//                response.EnsureSuccessStatusCode();

//                return await response.Content.ReadFromJsonAsync<GameInfo>();
//            }
//            catch
//            {
//                return null;
//            }
//        }


//        // 4) Отправить результат игрока
//        public static async Task<bool> SendResult(string gameId, string playerName, int score, int timeSeconds)
//        {
//            try
//            {
//                var body = new
//                {
//                    gameId,
//                    playerName,
//                    score,
//                    time = timeSeconds
//                };

//                // POST /game/result
//                var response = await http.PostAsJsonAsync("game/result", body);
//                response.EnsureSuccessStatusCode();

//                // Читаем ответ сервера
//                var result = await response.Content.ReadFromJsonAsync<ResultResponse>();

//                // Если сервер вернул null — считаем, что игра не удалена
//                return result?.Deleted ?? false;
//            }
//            catch (Exception)
//            {
//                return false;
//            }

//        }
//        /* public static async Task<List<GameResultDto>> GetResults(string gameId)
//         {
//             try
//             {
//                 var resp = await http.GetAsync($"results/{gameId}");
//                 resp.EnsureSuccessStatusCode();

//                 var json = await resp.Content.ReadAsStringAsync();
//                 return Newtonsoft.Json.JsonConvert
//                     .DeserializeObject<List<GameResultDto>>(json);
//             }
//             catch
//             {
//                 return new List<GameResultDto>();
//             }
//         }*/

       

//        public async Task<GameStatusDto> GetGameStatus(string gameId)
//        {
//            var response = await _httpClient.GetAsync($"game/status/{gameId}");
//            response.EnsureSuccessStatusCode();
//            var json = await response.Content.ReadAsStringAsync();

//            // Так как сервер возвращает { "isCompleted": true }, создадим простой DTO или используем dynamic
//            var data = JsonSerializer.Deserialize<dynamic>(json);
//            return new GameStatusDto { IsCompleted = data.isCompleted };
//        }

//        public class GameResultDto
//        {
//            public string PlayerName { get; set; }
//            public int Score { get; set; }
//            public int TimeSeconds { get; set; }
//        }

//        public static async Task<string> RawGamesJson()
//        {
//            return await http.GetStringAsync("games");
//        }

//    }
//}
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

    public class ResultResponse
    {
        public bool Deleted { get; set; }
    }

    // ⭐ Новый DTO для статуса игры
    public class GameStatusDto
    {
        public bool IsCompleted { get; set; }
    }

    public static class GameApi
    {
        private static readonly HttpClient http = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.0.18:5270/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        private static readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.0.18:5270")
        };

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

        public static async Task<GameInfo> JoinGame(string gameId, string playerName, string difficulty)
        {
            try
            {
                var body = new { gameId, playerName, difficulty };
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
        // Вспомогательный класс только для ответа эндпоинта /game/status
        private class GameStatusResponse
        {
            public bool isCompleted { get; set; }
        }

        // ⭐ ИСПРАВЛЕННЫЙ GetGameStatus (статический, использует http)
        public static async Task<GameStatusDto> GetGameStatus(string gameId)
        {
            try
            {
                // URL должен совпадать с тем, что в Program.cs: /game/status/{id}
                var response = await http.GetAsync($"game/status/{gameId}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var statusResponse = System.Text.Json.JsonSerializer.Deserialize<GameStatusResponse>(json);

                if (statusResponse == null)
                    return new GameStatusDto { IsCompleted = false };

                return new GameStatusDto { IsCompleted = statusResponse.isCompleted };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetGameStatus error: " + ex.Message);
                // Возвращаем false, чтобы цикл опроса не ломался
                return new GameStatusDto { IsCompleted = false };
            }
        }

        // ⭐ Метод для получения результатов (если ещё не добавлен)
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

            await client.PostAsJsonAsync("/chat", msg);
        }

        public static async Task<List<ChatMessageDto>> GetChatMessages()
        {
            var response = await client.GetAsync("/chat");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<List<ChatMessageDto>>();
        }

        public class ChatMessageDto
        {
            public string Player { get; set; }
            public string Text { get; set; }
            public DateTime Time { get; set; }
        }

    }
}
