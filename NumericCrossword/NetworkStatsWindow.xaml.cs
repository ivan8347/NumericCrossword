using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static NumericCrossword.Core.GameApi;

namespace NumericCrossword
{
    public partial class NetworkStatsWindow : Window
    {
        public NetworkStatsWindow(List<GameResultDto> results)
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
