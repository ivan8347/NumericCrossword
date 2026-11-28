using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static NumericCrossword.Core.GameApi;
using NumericCrossword.Core;
namespace NumericCrossword
{
    public partial class NetworkStatsWindow : Window
    {
        public NetworkStatsWindow(List<GameResult> results)
        {
            InitializeComponent();

           

            var winner = results
      .OrderByDescending(r => r.Score)
      .ThenBy(r => r.TimeSeconds)
      .First();

            var formatted = results
     .Select(r => new
     {
         r.PlayerName,
         r.Score,
         Time = TimeSpan.FromSeconds(
                    int.TryParse(r.TimeSeconds.ToString(), out int sec) ? sec : 0
                ).ToString(@"mm\:ss"),
         IsWinner = (r.PlayerName == winner.PlayerName)
     })
     .ToList();


            StatsGrid.ItemsSource = formatted;

            Title = $"Победитель: {winner.PlayerName}";

        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows;


//namespace NumericCrossword
//{
//    public partial class NetworkStatsWindow : Window
//    {
//        public NetworkStatsWindow(List<GamePlayer> results)
//        {
//            InitializeComponent();

//            // ЗАЩИТА: Если список пустой (null или Count == 0)
//            // Это случится, если клиент запросил статистику ПОСЛЕ того, как сервер удалил игру.
//            if (results == null || !results.Any())
//            {
//                // Показываем одну строку-заглушку, чтобы интерфейс не ломался
//                StatsGrid.ItemsSource = new List<GamePlayer>
//                {
//                    new GamePlayer
//                    {
//                        PlayerName = "Нет данных (игра завершена и удалена)",
//                        Score = 0,
//                        TimeSeconds = 0
//                    }
//                };
//                this.Title = "Статистика: данных нет";
//                return;
//            }

//            // Сортировка: Сначала очки (убывание), потом время (возрастание)
//            var sortedResults = results
//                .OrderByDescending(p => p.Score)
//                .ThenBy(p => p.TimeSeconds)
//                .ToList();

//            // Заполняем таблицу
//            StatsGrid.ItemsSource = sortedResults;

//            // Обновляем заголовок окна
//            var winner = sortedResults.First();
//            this.Title = $"Статистика: Победитель - {winner.PlayerName} ({winner.Score} очков)";
//        }

//        private void BtnOk_Click(object sender, RoutedEventArgs e)
//        {
//            this.Close();
//        }
//    }
//}
