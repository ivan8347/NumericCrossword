using System.Windows;
using System.IO;
using NumericCrossword.Core;      // где лежит ScoreStorage
using NumericCrossword.Models;   // где лежит ScoreRecord
using System.Linq;
using System;
//using CrosswordServer.Models;

namespace NumericCrossword
{
    public partial class RatingWindow : Window
    {
        public RatingWindow()
{
    InitializeComponent();
    LoadRating();
}

        private async void LoadRating()
        {
            var scores = await GameApi.GetRating();

            var ordered = scores
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.TimeSeconds)
                .ToList();

            ListScores.Items.Clear();

            int place = 1;
            foreach (var s in ordered)
            {
                ListScores.Items.Add(
                    $"{place}. {s.PlayerName} — {s.Score} очков — {s.TimeSeconds} сек — {s.Difficulty} — {s.Date:dd.MM.yyyy}"
                );
                place++;
            }
        }



        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем сохранённые данные (вызываем метод из ScoreStorage)
            ScoreStorage.Clear();

            // Обновляем список в окне (перезагружаем данные)
            var scores = ScoreStorage.Load();
            ListScores.Items.Clear();

        }
       


    }
}
