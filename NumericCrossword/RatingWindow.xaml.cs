using System.Windows;
using System.IO;
using NumericCrossword.Core;      // где лежит ScoreStorage
using NumericCrossword.Models;   // где лежит ScoreRecord
using System.Linq;
using System;

namespace NumericCrossword
{
    public partial class RatingWindow : Window
    {
        public RatingWindow()
        {
            InitializeComponent();

            // Загружаем список рекордов
            var scores = ScoreStorage.Load();

            // На всякий случай отсортируем по времени (наименьшее — лучше)
            var ordered = scores.OrderBy(s => s.Time).ToList();

            // Заполняем ListBox
            ListScores.Items.Clear();

            int place = 1;
            foreach (var s in ordered)
            {
               /* string medal =  place == 1 ? "🥇" :
                                place == 2 ? "🥈" :
                                place == 3 ? "🥉" : $"{place}.";*/

                ListScores.Items.Add(
                    $"{place}  - {s.Name} — {s.Score} очков - {s.Time:mm\\:ss} — {s.Difficulty} — {s.Date:dd.MM.yyyy}"
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
