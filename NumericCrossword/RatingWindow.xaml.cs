using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NumericCrossword.Core;      // GameApi, ScoreStorage
using NumericCrossword.Models;  // ScoreRecord, LocalScoreRecord

namespace NumericCrossword
{
    // DTO для отображения сетевого рейтинга
    public class NetworkScoreItem
    {
        public int Place { get; set; }
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public string TimeFormatted { get; set; }
        public string Difficulty { get; set; }
        public DateTime Date { get; set; }
    }

    // DTO для отображения локального топа
    public class LocalScoreItem
    {
        public int Place { get; set; }
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public string TimeFormatted { get; set; } // "01:23"
        public string Difficulty { get; set; }
        public DateTime Date { get; set; }
    }

    public partial class RatingWindow : Window
    {
        public RatingWindow()
        {
            InitializeComponent();
            LoadRatings();
        }

        private async void LoadRatings()
        {
            // 1. Сетевой рейтинг
            try
            {
                var scores = await GameApi.GetRating();
                var ordered = scores
                    .OrderByDescending(s => s.Score)
                    .ThenBy(s => s.TimeSeconds)
                    .ToList();

                var networkItems = ordered
                    .Select((s, index) => new NetworkScoreItem
                    {
                        Place = index + 1,
                        PlayerName = s.PlayerName,
                        Score = s.Score,
                        TimeFormatted = $"{s.TimeSeconds / 60:D2}:{s.TimeSeconds % 60:D2}",
                       // TimeFormatted = s.TimeSeconds,
                        Difficulty = s.Difficulty,
                        Date = s.Date
                    })
                    .ToList();

                ListScoresNetwork.ItemsSource = networkItems;
            }
            catch (Exception ex)
            {
                ListScoresNetwork.Items.Clear();
                ListScoresNetwork.Items.Add($"Ошибка загрузки сетевого рейтинга: {ex.Message}");
            }

            // 2. Локальный топ‑5
            try
            {
                var localScores = ScoreStorage.Load();
                // Локальные уже отсортированы по времени и обрезаны до топ‑5 в ScoreStorage,
                // но для отображения хочется по очкам или по времени — выбирай.
                // Здесь оставим как есть (по времени) и добавим место
                var localItems = localScores
                    .Select((s, index) => new LocalScoreItem
                    {
                        Place = index + 1,
                        PlayerName = s.PlayerName,
                        Score = s.Score,
                        TimeFormatted = s.Time.ToString(@"mm\:ss"), // "01:23"
                        Difficulty = s.Difficulty,
                        Date = s.Date
                    })
                    .ToList();

                ListScoresLocal.ItemsSource = localItems;
            }
            catch (Exception ex)
            {
                ListScoresLocal.Items.Clear();
                ListScoresLocal.Items.Add($"Ошибка чтения локального кэша: {ex.Message}");
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnClearLocal_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите очистить только локальный кэш (топ‑5 рекордов на диске)?\n\n" +
                "Сетевой рейтинг (с сервера) останется нетронутым.",
                "Подтверждение очистки",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ScoreStorage.Clear();
                    MessageBox.Show("Локальный кэш очищен.");
                    LoadRatings(); // обновить UI
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка очистки: " + ex.Message);
                }
            }
        }
    }
}
