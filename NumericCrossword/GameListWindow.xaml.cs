using System;
using System.Numerics;
using System.Windows;
using NumericCrossword.Core;
using NumericCrossword.Models;

namespace NumericCrossword
{
    public partial class GameListWindow : Window
    {
        public string SelectedGameId { get; private set; }
        public  PlayerProfile CurrentPlayer;
        public string CurrentDifficulty { get; set; }




        public GameListWindow()
        {
            InitializeComponent();
            LoadGames();
            GamesList.SelectionChanged += GamesList_SelectionChanged;

        }

        // Загрузка списка игр
        private async void LoadGames()
        {
            try
            {
                var games = await GameApi.GetGames(); 
                GamesList.ItemsSource = games;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки игр: " + ex.Message);
            }

        }

        // Кнопка "Обновить"
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadGames();
        }

        // Кнопка "Создать игру"
        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаём игру
               // var difficulty = CurrentDifficulty; // Лёгкий / Средний / Сложный
                var info = await GameApi.CreateGame(CurrentPlayer.Name, CurrentDifficulty);

                //var info = await GameApi.CreateGame(CurrentPlayer.Name, "Лёгкий");


                if (info != null)
                {
                    SelectedGameId = info.GameId;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Не удалось создать игру");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка создания игры: " + ex.Message);
            }
            MessageBox.Show(await GameApi.RawGamesJson());

        }

        // Кнопка "Присоединиться"

        // Вызывается, когда игрок выбирает игру из списка и нажимает кнопку
        private async void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            // 1) Проверяем, выбрал ли пользователь игру в списке

            GameItem item = GamesList.SelectedItem as GameItem;

            if (item == null)
            {
                // Если игра не выбрана — показываем сообщение и выходим
                MessageBox.Show("Выберите игру");
                return;
            }

            // 2) Пытаемся подключиться к выбранной игре через сервер
            // JoinGame возвращает GameInfo или null, если ошибка
            //GameInfo info = await GameApi.JoinGame(item.GameId, CurrentPlayer.Name, CurrentDifficul);
            GameInfo info = await GameApi.JoinGame(item.GameId, CurrentPlayer.Name);


            if (info == null)
            {
                // Сервер вернул ошибку — подключиться не удалось
                MessageBox.Show("Не удалось подключиться к игре");
                return;
            }

            // 3) Если подключение успешно — сохраняем ID игры
            // и закрываем окно, чтобы MainWindow получил результат

            SelectedGameId = info.GameId;

            // Закрываем окно и возвращаем управление в MainWindow
            DialogResult = true;
            Close();
        }
        private void GamesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var item = GamesList.SelectedItem as GameItem;
            if (item == null) return;

            // ⭐ Автоматически подставляем сложность выбранной игры
            CurrentDifficulty = item.Difficulty;

            // ⭐ Если у тебя есть ComboBox сложности — обнови его
           // DifficultyComboBox.SelectedItem = item.Difficulty;
        }

    }
}
