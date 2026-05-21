using System.Collections.Generic;
using System.Windows;
using NumericCrossword.Core;
using NumericCrossword.Models;

namespace NumericCrossword
{
    public partial class PlayerSelectWindow : Window
    {
        public PlayerProfile SelectedPlayer { get; private set; }
        private List<PlayerProfile> players;

        public PlayerSelectWindow()
        {
            InitializeComponent();

            players = PlayerStorage.Load();
            RefreshList();
        }

        private void RefreshList()
        {
            ListPlayers.Items.Clear();
            foreach (var p in players)
                ListPlayers.Items.Add(p.Name);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            PlayerNameWindow win = new PlayerNameWindow();
            win.Owner = this;

            if (win.ShowDialog() == true)
            {
                players.Add(new PlayerProfile { Name = win.PlayerName, TotalScore = 0 });
                PlayerStorage.Save(players);
                RefreshList();
            }
        }


        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ListPlayers.SelectedIndex == -1) return;

            players.RemoveAt(ListPlayers.SelectedIndex);
            PlayerStorage.Save(players);
            RefreshList();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (ListPlayers.SelectedIndex == -1) return;

            SelectedPlayer = players[ListPlayers.SelectedIndex];
            DialogResult = true;
            Close();
        }
    }
}
