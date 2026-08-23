using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NumericCrossword.Models;
using NumericCrossword.Core;
using System.Diagnostics;
using System.Net.Http;
using static NumericCrossword.Core.GameApi;
using System.Threading.Tasks;



namespace NumericCrossword
{

    class CrosswordTemplate
    {
        public List<FormulaSlot> Slots { get; set; } = new List<FormulaSlot>();
    }

    public partial class MainWindow : Window
    {
        private const int Rows = 15;
        private const int Cols = 20;
        private int score = 0;

        private Label[,] cells = new Label[Rows, Cols];

        private DispatcherTimer timer;
        private TimeSpan timerValue = TimeSpan.Zero;
        private DispatcherTimer networkCheckTimer = new DispatcherTimer();


        private string selectedTileValue = null;
        private Label selectedTileLabel = null;
        private string currentDifficulty = "Лёгкий";

        public bool IsOnlineGame { get; set; }
        private string currentGameId;
        private int totalScore;
        public static int CurrentTotalPlayers = 0;
        private bool isGameFinished = false;     
        private bool AllowMoves = true;


        // private Random rnd;
        private Random rnd = new Random();
        private DateTime serverStartTime;
        public static PlayerProfile CurrentPlayer;




        private Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> cellUndoStack
            = new Stack<(int, int, string, string, bool)>();

        private Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> undoStack
            = new Stack<(int, int, string, string, bool)>();


        private List<CrosswordTemplate> templatesEasy = new List<CrosswordTemplate>();

        private List<Formula> formulas = new List<Formula>();


        public MainWindow()
        {
            InitializeComponent();
            CreateGrid();
            InitTimer();
            InitTemplates();
            DifficultyBox.SelectedIndex = 0;
            networkCheckTimer.Interval = TimeSpan.FromSeconds(1);


        }


        // -----------------------------
        //  СОЗДАНИЕ СЕТКИ
        // -----------------------------
        private void CreateGrid()
        {
            CrosswordGrid.RowDefinitions.Clear();
            CrosswordGrid.ColumnDefinitions.Clear();
            CrosswordGrid.Children.Clear();


            for (int r = 0; r < Rows; r++)
                CrosswordGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            for (int c = 0; c < Cols; c++)
                CrosswordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            cells = new Label[Rows, Cols];

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    Label cell = new Label
                    {
                        Width = 50,
                        Height = 40,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontSize = 18,
                        Background = Brushes.White,
                        AllowDrop = true
                    };

                    cell.Drop += Cell_Drop;
                    cell.MouseLeftButtonUp += Cell_Click;

                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);

                    cells[r, c] = cell;
                    CrosswordGrid.Children.Add(cell);
                }
            }
        }
        private async void Cell_Click(object sender, MouseButtonEventArgs e)
        {
            if (!AllowMoves) return;
            Label cell = sender as Label;

            // работать только со скрытыми клетками
            if ((string)cell.Tag != "hidden")
                return;

            // если выбрана плитка — вставляем
            if (selectedTileValue != null)
            {
                await InsertValueIntoCell(cell, selectedTileValue);
                RemoveTile(selectedTileValue);
                selectedTileLabel.BorderBrush = Brushes.SteelBlue;
                selectedTileLabel = null;
                selectedTileValue = null;
                return;
            }

            // переключение выделения
            if (cell.BorderBrush == Brushes.Red)
            {
                cell.BorderBrush = Brushes.LightGray;
                cell.BorderThickness = new Thickness(1);
            }
            else
            {
                cell.BorderBrush = Brushes.Red;
                cell.BorderThickness = new Thickness(3);
            }
        }
        private async Task InsertValueIntoCell(Label cell, string value)
        {
            if ((string)cell.Tag != "hidden")
                return;

            int row = Grid.GetRow(cell);
            int col = Grid.GetColumn(cell);

            string oldValue = cell.Content?.ToString() ?? "";

            bool tileWasRemoved = string.IsNullOrEmpty(oldValue);

            if (!tileWasRemoved && int.TryParse(oldValue, out _))
                ReturnTile(oldValue);

            undoStack.Push((row, col, oldValue, value, tileWasRemoved));
            cellUndoStack.Push((row, col, oldValue, value, tileWasRemoved));

            cell.Content = value;

            // Подсветка правильной клетки
            cell.BorderBrush = Brushes.LightGreen;
            cell.BorderThickness = new Thickness(5);

            // 🎯 НАЧИСЛЕНИЕ ОЧКОВ
            /*  int add = 0;

              switch (currentDifficulty)
              {
                  case "Лёгкий": add = 1; break;
                  case "Средний": add = 5; break;
                  case "Сложный": add = 10; break;
              }

              score += add;
              ScoreText.Text = score.ToString();*/
            // -------------------------------

            // твоя логика вставки значения



            if (IsCrosswordSolved() && !IsOnlineGame)
            {
                ShowWinMessage();
                return;
            }

            if (IsOnlineGame)
            {
                await TryFinishOnlineGameIfSolved();
                return;
            }


        }


        // -----------------------------
        //  DRAG & DROP ПЛИТОК
        // -----------------------------
        private void Tile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Label tile = sender as Label;
                DragDrop.DoDragDrop(tile, tile.Content.ToString(), DragDropEffects.Copy);
            }

        }
        private void Tile_Click(object sender, MouseButtonEventArgs e)
        {
            Label tile = sender as Label;

            // если плитка уже выбрана — снять выбор
            if (selectedTileLabel == tile)
            {
                tile.BorderBrush = Brushes.SteelBlue;
                selectedTileLabel = null;
                selectedTileValue = null;
                return;
            }

            // снять выделение со старой плитки
            if (selectedTileLabel != null)
                selectedTileLabel.BorderBrush = Brushes.SteelBlue;

            // выделить новую плитку
            selectedTileLabel = tile;
            selectedTileValue = tile.Content.ToString();
            tile.BorderBrush = Brushes.Red;
        }

        private async void Cell_Drop(object sender, DragEventArgs e)
        {
            if (!AllowMoves) return;

            if (!e.Data.GetDataPresent(DataFormats.StringFormat))
                return;

            string value = (string)e.Data.GetData(DataFormats.StringFormat);
            Label cell = sender as Label;

            // только скрытые клетки
            if ((string)cell.Tag != "hidden")
                return;

            await InsertValueIntoCell(cell, value);
            RemoveTile(value);

            if (IsCrosswordSolved() && !IsOnlineGame)
            {
                ShowWinMessage();
                return;
            }

          if (IsOnlineGame)
    {
        await TryFinishOnlineGameIfSolved();  // <-- await
        return;
    }
        }

        private async Task TryFinishOnlineGameIfSolved()
        {
            if (!IsOnlineGame) return;
            if (!IsCrosswordSolved()) return;

            await FinishOnlineGame();
        }




        private void RemoveTile(string value)
        {
            Label toRemove = null;

            foreach (Label tile in TilesPanel.Children)
            {
                if (tile.Content.ToString() == value)
                {
                    toRemove = tile;
                    break;
                }
            }

            if (toRemove != null)
                TilesPanel.Children.Remove(toRemove);
        }

        // -----------------------------
        //  ОТМЕНА
        // -----------------------------
        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            // 1. Ищем выделенную ячейку по красной рамке
            int selRow = -1;
            int selCol = -1;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (cells[r, c].BorderBrush == Brushes.Red)
                    {
                        selRow = r;
                        selCol = c;
                        break;
                    }
                }
                if (selRow != -1) break;
            }

            // 2. Undo по выделенной ячейке
            if (selRow != -1)
            {
                Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> temp =
                    new Stack<(int, int, string, string, bool)>();

                (int row, int col, string oldValue, string newValue, bool tileWasRemoved) target =
                    (-1, -1, "", "", false);

                while (cellUndoStack.Count > 0)
                {
                    var move = cellUndoStack.Pop();

                    if (move.row == selRow && move.col == selCol)
                    {
                        target = move;
                        break;
                    }
                    else
                    {
                        temp.Push(move);
                    }
                }

                while (temp.Count > 0)
                    cellUndoStack.Push(temp.Pop());

                if (target.row != -1)
                {
                    // восстановить значение
                    cells[selRow, selCol].Content = target.oldValue;

                    // вернуть плитку, если она была удалена
                    if (target.tileWasRemoved)
                        ReturnTile(target.newValue);

                    // снять выделение
                    cells[selRow, selCol].BorderBrush = Brushes.LightGray;
                    cells[selRow, selCol].BorderThickness = new Thickness(1);
                    // cells[selRow, selCol].Background = Brushes.LightYellow;

                    // очистить ВСЕ записи об этой ячейке из cellUndoStack
                    Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> temp2 =
                        new Stack<(int, int, string, string, bool)>();

                    while (cellUndoStack.Count > 0)
                    {
                        var m = cellUndoStack.Pop();
                        if (!(m.row == selRow && m.col == selCol))
                            temp2.Push(m);
                    }

                    while (temp2.Count > 0)
                        cellUndoStack.Push(temp2.Pop());

                    // ❗ ОБЯЗАТЕЛЬНО: очистить ВСЕ записи об этой ячейке из undoStack
                    Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> temp3 =
                        new Stack<(int, int, string, string, bool)>();

                    while (undoStack.Count > 0)
                    {
                        var m = undoStack.Pop();
                        if (!(m.row == selRow && m.col == selCol))
                            temp3.Push(m);
                    }

                    while (temp3.Count > 0)
                        undoStack.Push(temp3.Pop());

                    return;
                }


            }

            // 3. Обычный Undo плиток
            if (undoStack.Count == 0)
                return;

            var tileMove = undoStack.Pop();

            Label cell = cells[tileMove.row, tileMove.col];

            cell.Content = tileMove.oldValue;

            if (tileMove.tileWasRemoved)
                ReturnTile(tileMove.newValue);

            cell.BorderBrush = Brushes.LightGray;
            cell.BorderThickness = new Thickness(1);

            /*cell.Background = string.IsNullOrEmpty(tileMove.oldValue)
                ? Brushes.LightYellow
                : Brushes.LightGreen;*/
        }

        private void ReturnTile(string value)
        {
            Label tile = new Label
            {
                Content = value,
                Width = 60,
                Height = 60,
                Background = Brushes.LightBlue,
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(2),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 28,
                Margin = new Thickness(5)
            };

            // ❗ ОБЯЗАТЕЛЬНО — подписываем на выбор плитки
            tile.MouseLeftButtonUp += Tile_Click;

            // ❗ ОБЯЗАТЕЛЬНО — подписываем на Drag&Drop
            tile.MouseMove += Tile_MouseMove;

            TilesPanel.Children.Add(tile);
        }

        // -----------------------------
        //  ТАЙМЕР
        // -----------------------------
        private void InitTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Просто увеличиваем накопленное время на 1 секунду
            timerValue = timerValue.Add(TimeSpan.FromSeconds(1));

            // Форматируем и выводим (mm:ss)
            if (TimerText != null)
                TimerText.Text = timerValue.ToString(@"mm\:ss");
        }


        //  ГЕНЕРАЦИЯ ОДНОЙ ФОРМУЛЫ
        private Formula GenerateRandomFormula()
        {
            char[] ops = { '+', '-', '*', '/' };
            char op = ops[rnd.Next(ops.Length)];

            int a, b, c;

            switch (op)
            {
                case '+':
                    a = rnd.Next(10, 50);
                    b = rnd.Next(10, 50);
                    c = a + b;
                    break;

                case '-':
                    a = rnd.Next(20, 90);
                    b = rnd.Next(10, a);
                    c = a - b;
                    break;

                case '*':
                    a = rnd.Next(2, 15);
                    b = rnd.Next(2, 15);
                    c = a * b;
                    break;

                case '/':
                    b = rnd.Next(2, 15);
                    c = rnd.Next(2, 15);
                    a = b * c;
                    break;

                default:
                    a = b = c = 0;
                    break;
            }

            return new Formula
            {
                A = a,
                B = b,
                C = c,
                Op = op
            };
        }

        // ===== ЧАСТЬ 2 =====
        //  ШАБЛОНЫ
        private void InitTemplates()
        {
            templatesEasy.Clear();

            string path = GetFilePath("templates_easy.json");

            if (!File.Exists(path))
            {
                MessageBox.Show("Файл шаблонов не найден: " + path);
                return;
            }

            string json = File.ReadAllText(path);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<TemplateFile>(json);

            foreach (var t in data.templates)
            {
                CrosswordTemplate ct = new CrosswordTemplate();
                foreach (var s in t.slots)
                {
                    ct.Slots.Add(new FormulaSlot
                    {
                        Row = s.Row,
                        Col = s.Col,
                        Horizontal = s.Horizontal
                    });
                }
                templatesEasy.Add(ct);
            }
        }

        private string GetFilePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", fileName);
        }


        //  ЗАПИСЬ ФОРМУЛЫ В СЕТКУ
        private void PlaceFormulaToGrid(Formula f, string[,] grid)
        {
            string[] symbols = {
                f.A.ToString(),
                f.Op.ToString(),
                f.B.ToString(),
                "=",
                f.C.ToString()
    };

            for (int i = 0; i < 5; i++)
            {
                int r = f.Row + (f.Horizontal ? 0 : i);
                int c = f.Col + (f.Horizontal ? i : 0);
                grid[r, c] = symbols[i];
            }
        }

        // -----------------------------
        //  ГЕНЕРАЦИЯ КРОССВОРДА ПО ШАБЛОНУ
        // -----------------------------
        private List<Formula> GenerateCrossword()
        {
            string[,] grid = new string[Rows, Cols];
            List<Formula> result = new List<Formula>();

            var template = templatesEasy[rnd.Next(templatesEasy.Count)];

            foreach (var slot in template.Slots)
            {
                Formula placed = null;

                for (int attempt = 0; attempt < 300; attempt++)
                {
                    Formula f = GenerateRandomFormula();
                    f.Row = slot.Row;
                    f.Col = slot.Col;
                    f.Horizontal = slot.Horizontal;

                    string[] symbols = {
                f.A.ToString(),
                f.Op.ToString(),
                f.B.ToString(),
                "=",
                f.C.ToString()
            };

                    bool ok = true;

                    for (int i = 0; i < 5; i++)
                    {
                        int r = f.Row + (f.Horizontal ? 0 : i);
                        int c = f.Col + (f.Horizontal ? i : 0);

                        if (r < 0 || r >= Rows || c < 0 || c >= Cols)
                        {
                            ok = false;
                            break;
                        }


                        string existing = grid[r, c];

                        if (existing != null && existing != symbols[i])
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (!ok)
                        continue;

                    // ВАЖНО: ТУТ МЫ ПИШЕМ В СЕТКУ
                    PlaceFormulaToGrid(f, grid);

                    placed = f;
                    break;
                }
                if (placed != null)
                {
                    result.Add(placed);
                }
                else
                {
                    // слот пропущен — НЕ добавляем формулу
                    // и НЕ добавляем её числа в плитки
                }

            }

            return result;
        }

        // -----------------------------
        //  СКРЫТИЕ ЧИСЕЛ
        // -----------------------------
        private void ApplyDifficulty(List<Formula> formulas)
        {
            int difficulty = DifficultyBox.SelectedIndex;

            double hideChance = difficulty == 0 ? 0.05 :
                                difficulty == 1 ? 0.05 : 0.80;

            foreach (var f in formulas)
            {
                // случайно скрываем
                f.HideA = rnd.NextDouble() < hideChance;
                f.HideB = rnd.NextDouble() < hideChance;
                f.HideC = rnd.NextDouble() < hideChance;

                // ❗ запрещаем скрывать ВСЕ три числа
                if (f.HideA && f.HideB && f.HideC)
                {
                    int leaveVisible = rnd.Next(3);
                    if (leaveVisible == 0) f.HideA = false;
                    else if (leaveVisible == 1) f.HideB = false;
                    else f.HideC = false;
                }
            }
        }

        // -----------------------------
        //  ОТРИСОВКА ФОРМУЛ
        // -----------------------------
        private void DrawFormulas(List<Formula> formulas)
        {
            // 1. Очищаем сетку
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    cells[r, c].Background = Brushes.White;
                    cells[r, c].Tag = null;
                    cells[r, c].Content = "";
                    cells[r, c].BorderBrush = Brushes.LightGray;
                }
            }

            // 2. Рисуем формулы
            foreach (var f in formulas)
            {
                string[] symbols = {
            f.A.ToString(),
            f.Op.ToString(),
            f.B.ToString(),
            "=",
            f.C.ToString()
        };

                for (int i = 0; i < 5; i++)
                {
                    int r = f.Row + (f.Horizontal ? 0 : i);
                    int c = f.Col + (f.Horizontal ? i : 0);

                    var cell = cells[r, c];

                    cell.Background = Brushes.LightYellow;

                    // --- ТИП КЛЕТКИ ---
                    if (i == 1 || i == 3)          // оператор или "="
                    {
                        cell.Tag = "op";
                        cell.Content = symbols[i];
                    }
                    else                            // число
                    {
                        bool hidden =
                            (i == 0 && f.HideA) ||
                            (i == 2 && f.HideB) ||
                            (i == 4 && f.HideC);

                        if (hidden)
                        {
                            cell.Tag = "hidden";    // сюда можно ставить плитки
                            cell.Content = "";
                        }
                        else
                        {
                            cell.Tag = "fixed";     // менять нельзя
                            cell.Content = symbols[i];
                        }
                    }
                }
            }
        }


        // -----------------------------
        //  ПЛИТКИ
        // -----------------------------
        private void CreateTilesFromFormulas(List<Formula> formulas)
        {
            TilesPanel.Children.Clear();

            List<int> tiles = new List<int>();

            // чтобы не добавлять плитку дважды для одной и той же клетки
            HashSet<(int r, int c)> usedCells = new HashSet<(int r, int c)>();

            foreach (var f in formulas)
            {
                // A
                if (f.HideA)
                {
                    int r = f.Row + (f.Horizontal ? 0 : 0);
                    int c = f.Col + (f.Horizontal ? 0 : 0);

                    if (IsHighlightedCell(r, c) &&
                        (cells[r, c].Content == null || cells[r, c].Content.ToString() == "") &&
                        !usedCells.Contains((r, c)))
                    {
                        tiles.Add(f.A);
                        usedCells.Add((r, c));
                    }
                }

                // B
                if (f.HideB)
                {
                    int r = f.Row + (f.Horizontal ? 0 : 2);
                    int c = f.Col + (f.Horizontal ? 2 : 0);

                    if (IsHighlightedCell(r, c) &&
                        (cells[r, c].Content == null || cells[r, c].Content.ToString() == "") &&
                        !usedCells.Contains((r, c)))
                    {
                        tiles.Add(f.B);
                        usedCells.Add((r, c));
                    }
                }

                // C
                if (f.HideC)
                {
                    int r = f.Row + (f.Horizontal ? 0 : 4);
                    int c = f.Col + (f.Horizontal ? 4 : 0);

                    if (IsHighlightedCell(r, c) &&
                        (cells[r, c].Content == null || cells[r, c].Content.ToString() == "") &&
                        !usedCells.Contains((r, c)))
                    {
                        tiles.Add(f.C);
                        usedCells.Add((r, c));
                    }
                }
            }

            tiles = tiles.OrderBy(x => rnd.Next()).ToList();

            foreach (int n in tiles)
            {
                Label tile = new Label
                {
                    Content = n.ToString(),
                    Width = 60,
                    Height = 60,
                    FontSize = 28,
                    Background = Brushes.LightBlue,
                    BorderBrush = Brushes.SteelBlue,
                    BorderThickness = new Thickness(2),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5)
                };

                tile.MouseMove += Tile_MouseMove;
                tile.MouseLeftButtonUp += Tile_Click;
                TilesPanel.Children.Add(tile);

            }
        }

        private bool IsHighlightedCell(int r, int c)
        {
            return cells[r, c].Background == Brushes.LightYellow;
        }

        // -----------------------------
        //  НОВАЯ ИГРА
        // -----------------------------
        private void BtnNewGame_Click(object sender, RoutedEventArgs e)
        {
            IsOnlineGame = false;
            currentGameId = null;

            undoStack.Clear();
            cellUndoStack.Clear();
            currentGameId = null;

            timer.Stop();

            // СБРОС ВРЕМЕНИ
            timerValue = TimeSpan.Zero;
            if (TimerText != null) TimerText.Text = "00:00";

            score = 0;
            ScoreText.Text = "0";

            serverStartTime = DateTime.UtcNow;

            // --- ДОБАВИТЬ: ПОЛНАЯ ОЧИСТКА СЕТКИ ---
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var cell = cells[r, c];
                    cell.Content = "";
                    cell.Tag = null;
                    cell.Background = Brushes.White;
                    cell.BorderBrush = Brushes.LightGray;
                    cell.BorderThickness = new Thickness(1);
                }
            }
            // ---------------------------------------

            CreateGrid(); // если нужно пересоздать разметку (обычно не требуется, если сетка уже создана)
            formulas = GenerateCrossword();
            ApplyDifficulty(formulas);
            DrawFormulas(formulas);
            CreateTilesFromFormulas(formulas);

            timer.Start();
        }

        // ⭐ Финальная версия ShowWinMessage
        private async void ShowWinMessage()
        {
            timer.Stop();

            totalScore = CalculateFinalScore();
            score = totalScore;
            ScoreText.Text = totalScore.ToString();

            ScoreStorage.AddRecord(new ScoreRecord
            {
                Name = CurrentPlayer.Name,
                Difficulty = currentDifficulty,
                Time = timerValue,
                Score = totalScore,
                Date = DateTime.Now
            });

            var players = PlayerStorage.Load();
            var p = players.FirstOrDefault(x => x.Name == CurrentPlayer.Name);
            if (p != null)
                p.TotalScore += totalScore;

            CurrentPlayer.TotalScore = p.TotalScore;
            BtnSelectPlayer.Content = $"{CurrentPlayer.Name} ({CurrentPlayer.TotalScore})";
            PlayerStorage.Save(players);

            WinMessage msg = new WinMessage("УРА!\nКРОССВОРД РЕШЁН!\nОчки: " + totalScore);
            msg.Owner = this;
            msg.ShowDialog();
        }

        private async Task FinishOnlineGame()
        {
            if (isGameFinished) return;

            isGameFinished = true;
            AllowMoves = false;
            timer.Stop();
            totalScore = CalculateFinalScore();

            await GameApi.SendResult(currentGameId, CurrentPlayer.Name, totalScore, (int)timerValue.TotalSeconds);

            MessageBox.Show("Ваш результат отправлен! Ожидайте завершения игры соперником...", "Ожидание");

            int maxAttempts = 150;      // до 5 минут ожидания (150 * 2 сек)
            int delayMs = 2000;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                await Task.Delay(delayMs);

                var status = await GameApi.GetGameStatus(currentGameId);

                if (status.IsCompleted)
                {
                    var results = await GameApi.GetResults(currentGameId);
                    if (results != null && results.Count > 0)
                    {
                        ShowWinMessage(results);
                        return;
                    }
                }
                attempts++;
            }

            // Таймаут: показываем то, что есть
            MessageBox.Show("Время ожидания истекло. Показываем текущие результаты.", "Таймаут");
            var finalResults = await GameApi.GetResults(currentGameId);
            if (finalResults != null && finalResults.Count > 0)
                ShowWinMessage(finalResults);
        }




        private void ShowWinMessage(List<GameResult> results)
        {
            if (results == null || results.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения результатов.", "Внимание");
                return;
            }

            // 1. Сначала показываем красивое окно со статистикой
            try
            {
                var statsWindow = new NetworkStatsWindow(results);
                statsWindow.Owner = this;
                statsWindow.ShowDialog(); // Код ждет здесь, пока пользователь не закроет окно
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии NetworkStatsWindow: {ex.Message}", "Ошибка");
            }

            // 2. Формируем текстовый отчет (теперь он не дублирует окно, а служит резервом или логом)
            string text = "Результаты сетевой игры:\n\n";
            foreach (var r in results)
            {
                text += $"{r.PlayerName}: {r.Score} очков, время {TimeSpan.FromSeconds(r.TimeSeconds):mm\\:ss}\n";
            }

            // 3. Показываем текстовое окно (если нужно)
            // Если WinMessage тоже требует List<GameResult> или строку, передай text
            try
            {
                // Предполагаем, что конструктор WinMessage принимает строку
                var msg = new WinMessage(text);
                msg.Owner = this;
                msg.ShowDialog();
            }
            catch (Exception ex)
            {
                // Если WinMessage сломается, основная статистика уже была показана
                System.Diagnostics.Debug.WriteLine("Не удалось открыть WinMessage: " + ex.Message);
            }
        }








        private bool IsCrosswordSolved()
        {
            foreach (var f in formulas)
            {
                int A = GetCellValue(f.Row, f.Col);
                int B = GetCellValue(f.Row + (f.Horizontal ? 0 : 2),
                                     f.Col + (f.Horizontal ? 2 : 0));
                int C = GetCellValue(f.Row + (f.Horizontal ? 0 : 4),
                                     f.Col + (f.Horizontal ? 4 : 0));

                if (A == -1 || B == -1 || C == -1)
                    return false;

                int result = 0;

                switch (f.Op)
                {
                    case '+':
                        result = A + B;
                        break;

                    case '-':
                        result = A - B;
                        break;

                    case '*':
                        result = A * B;
                        break;

                    case '/':
                        if (B != 0)
                            result = A / B;
                        else
                            result = -999999;
                        break;

                    default:
                        result = -999999;
                        break;
                }

                if (result != C)
                    return false;
            }

            return true;
        }
        private int GetCellValue(int r, int c)
        {
            string s = cells[r, c].Content?.ToString();
            if (int.TryParse(s, out int v))
                return v;
            return -1;
        }

        private void BtnRating_Click(object sender, RoutedEventArgs e)
        {
            RatingWindow win = new RatingWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void DifficultyBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DifficultyBox.SelectedItem is ComboBoxItem item)
            {
                currentDifficulty = item.Content.ToString();
            }
        }
        private void BtnSelectPlayer_Click(object sender, RoutedEventArgs e)
        {
            currentGameId = null;

            PlayerSelectWindow ps = new PlayerSelectWindow();

            if (ps.ShowDialog() == true)
            {
                CurrentPlayer = ps.SelectedPlayer;
                BtnSelectPlayer.Content = $"{CurrentPlayer.Name} ({CurrentPlayer.TotalScore})";

                // BtnSelectPlayer.Content = CurrentPlayer.Name;
            }
        }



        private int CalculateFinalScore()
        {
            int baseScorePerFormula = 0;

            switch (currentDifficulty)
            {
                case "Лёгкий":
                    baseScorePerFormula = 1;
                    break;
                case "Средний":
                    baseScorePerFormula = 5;
                    break;
                case "Сложный":
                    baseScorePerFormula = 10;
                    break;
                default:
                    baseScorePerFormula = 2;
                    break;
            }

            // Умножаем на количество формул
            int totalFormulas = formulas.Count;
            int score = baseScorePerFormula * totalFormulas;

            // Бонус за скорость: чем быстрее решили, тем больше бонус
            double minutesElapsed = timerValue.TotalMinutes;
            if (minutesElapsed < 5)
                score += 10; // большой бонус за быстрое решение
            else if (minutesElapsed < 5)
                score += 5; // средний бонус

            return score;
        }


        private void InitRandom(int seed)
        {
            rnd = new Random(seed);
        }
        private void BtnOnline_Click(object sender, RoutedEventArgs e)
        {
            isGameFinished = false;
            AllowMoves = true;
            timerValue = TimeSpan.Zero;
            try
            {
                // System.Diagnostics.Process.Start("U:\\Users\\kit\\source\\repos\\CrosswordServer\\CrosswordServer\\bin\\Debug\\net8.0\\CrosswordServer.exe");
            }
            catch { }
            // StartServerHidden(); 
            GameListWindow win = new GameListWindow();
            win.CurrentPlayer = CurrentPlayer;   // ← передаём игрока
            win.CurrentDifficulty = currentDifficulty;

            win.Owner = this;
            win.ShowDialog();

            if (win.SelectedGameId != null)
            {
                // После закрытия окна выбора игры мы получаем ID выбранной игры

                currentGameId = win.SelectedGameId;

                // Запускаем сетевую игру, передавая gameId в метод
                StartOnlineGame(currentGameId);

            }

        }

        private void BtnChat_Click(object sender, RoutedEventArgs e)
        {
            var chat = new ChatWindow(CurrentPlayer.Name);
            chat.Owner = this;
            chat.Show();
        }
        

        // Запуск сетевой игры после выбора или создания
        // Этот метод вызывается после того, как GameListWindow вернул GameId
        // Здесь мы получаем полную информацию об игре с сервера,
        // инициализируем генератор кроссворда и запускаем сетевую партию

        private async void StartOnlineGame(string gameId)
        {
            MessageBox.Show("StartOnlineGame вызван");

            currentGameId = gameId;
            IsOnlineGame = true;   // ← КРИТИЧЕСКИ ВАЖНО

            try
            {
               var info = await GameApi.JoinGame(gameId, CurrentPlayer.Name, currentDifficulty);
                //var info = await GameApi.JoinGame(gameId, CurrentPlayer.Name);

                if (info == null)
                {
                    MessageBox.Show("Ошибка: не удалось получить данные игры.");
                    return;
                }
                CurrentTotalPlayers = info.Players.Count;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] В игре {gameId} всего игроков: {info.Players.Count}");


                timer.Stop();
                timerValue = TimeSpan.Zero;

                if (TimerText != null)
                    TimerText.Text = "00:00";

                TilesPanel.Children.Clear();
                undoStack.Clear();
                cellUndoStack.Clear();

                CreateGrid();

                if (cells != null)
                {
                    for (int r = 0; r < Rows; r++)
                    {
                        for (int c = 0; c < Cols; c++)
                        {
                            var cell = cells[r, c];
                            if (cell != null)
                            {
                                cell.Content = "";
                                cell.Tag = null;
                                cell.Background = Brushes.White;
                                cell.BorderBrush = Brushes.LightGray;
                                cell.BorderThickness = new Thickness(1);
                            }
                        }
                    }
                }

                InitRandom(info.Seed);

                formulas = GenerateCrossword();
                ApplyDifficulty(formulas);
                DrawFormulas(formulas);
                CreateTilesFromFormulas(formulas);

                timer.Start();

                MessageBox.Show("Вы подключены к игре: " + gameId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сетевой игры: {ex.Message}");
            }
        }





        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://crosswordserver.onrender.com") // адрес твоего сервера
        };

    }
}
