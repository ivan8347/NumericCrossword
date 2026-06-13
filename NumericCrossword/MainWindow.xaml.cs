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


        private string selectedTileValue = null;
        private Label selectedTileLabel = null;
        private string currentDifficulty = "Лёгкий";

        private string currentGameId;
       // private Random rnd;
       private Random rnd = new Random();



        private Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> cellUndoStack
            = new Stack<(int, int, string, string, bool)>();

        private Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> undoStack
            = new Stack<(int, int, string, string, bool)>();


        private List<CrosswordTemplate> templatesEasy = new List<CrosswordTemplate>();

        private List<Formula> formulas = new List<Formula>();

        public static PlayerProfile CurrentPlayer;

        public MainWindow()
        {
            InitializeComponent();
            CreateGrid();
            InitTimer();
            InitTemplates();
            DifficultyBox.SelectedIndex = 0;
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
        private void Cell_Click(object sender, MouseButtonEventArgs e)
        {
            Label cell = sender as Label;

            // работать только со скрытыми клетками
            if ((string)cell.Tag != "hidden")
                return;

            // если выбрана плитка — вставляем
            if (selectedTileValue != null)
            {
                InsertValueIntoCell(cell, selectedTileValue);
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
        private void InsertValueIntoCell(Label cell, string value)
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

            if (IsCrosswordSolved())
                ShowWinMessage();
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

        private void Cell_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat))
                return;

            string value = (string)e.Data.GetData(DataFormats.StringFormat);
            Label cell = sender as Label;

            // только скрытые клетки
            if ((string)cell.Tag != "hidden")
                return;

            InsertValueIntoCell(cell, value);
            RemoveTile(value);

            if (IsCrosswordSolved())
                ShowWinMessage();
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
            timerValue = timerValue.Add(TimeSpan.FromSeconds(1));
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
            undoStack.Clear();
            timer.Stop();
            timerValue = TimeSpan.Zero;   // сброс времени
            TimerText.Text = "00:00";
            timer.Start();
            score = 0;
            ScoreText.Text = "0";

            CreateGrid();
            formulas = GenerateCrossword();
            ApplyDifficulty(formulas);
            DrawFormulas(formulas);
            CreateTilesFromFormulas(formulas);
        }

        /*private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaused)
            {
                timer.Stop();
                BtnPause.Content = "Продолжить";
                isPaused = true;
            }
            else
            {
                timer.Start();
                BtnPause.Content = "Пауза";
                isPaused = false;
            }
        }*/

        // КОНЕЦ ИГРЫ

        /* private async void ShowWinMessage()
         {
             timer.Stop();

             // 1. Сохраняем локальный рекорд
             ScoreStorage.AddRecord(new ScoreRecord
             {
                 Name = MainWindow.CurrentPlayer.Name,
                 Difficulty = currentDifficulty,
                 Time = timerValue,
                 Score = score,
                 Date = DateTime.Now
             });

             // 2. Загружаем всех игроков
             var players = PlayerStorage.Load();

             // 3. Находим текущего игрока
             var p = players.FirstOrDefault(x => x.Name == MainWindow.CurrentPlayer.Name);

             if (p != null)
             {
                 // 4. Добавляем очки в профиль
                 p.TotalScore += score;
             }

             CurrentPlayer.TotalScore = p.TotalScore;
             BtnSelectPlayer.Content = $"{CurrentPlayer.Name} ({CurrentPlayer.TotalScore})";

             // 5. Сохраняем обновлённый список игроков
             PlayerStorage.Save(players);

             // 6. Отправляем результат на сервер
             if (!string.IsNullOrEmpty(currentGameId))
             {
                 await GameApi.SendResult(
                     currentGameId,
                     CurrentPlayer.Name,
                     score,
                     (int)timerValue.TotalSeconds
                 );
             }

             // 7. Показываем окно победы
             WinMessage msg = new WinMessage("УРА!\nКРОССВОРД РЕШЁН!");
             msg.Owner = this;
             msg.ShowDialog();
         }*/
        private async void ShowWinMessage()
        {
            timer.Stop();

            // 1. РАСЧЁТ ИТОГОВОГО СЧЁТА
            int totalScore = CalculateFinalScore();
            score = totalScore;
            ScoreText.Text = totalScore.ToString();

            // 2. Сохраняем локальный рекорд
            ScoreStorage.AddRecord(new ScoreRecord
            {
                Name = MainWindow.CurrentPlayer.Name,
                Difficulty = currentDifficulty,
                Time = timerValue,
                Score = totalScore,
                Date = DateTime.Now
            });

            // 3. Загружаем всех игроков
            var players = PlayerStorage.Load();

            // 4. Находим текущего игрока
            var p = players.FirstOrDefault(x => x.Name == MainWindow.CurrentPlayer.Name);

            if (p != null)
            {
                // 5. Добавляем очки в профиль
                p.TotalScore += totalScore;
            }

            CurrentPlayer.TotalScore = p.TotalScore;
            BtnSelectPlayer.Content = $"{CurrentPlayer.Name} ({CurrentPlayer.TotalScore})";

            // 6. Сохраняем обновлённый список игроков
            PlayerStorage.Save(players);

            // 7. Отправляем результат на сервер
            if (!string.IsNullOrEmpty(currentGameId))
            {
                await GameApi.SendResult(
                    currentGameId,
                    CurrentPlayer.Name,
                    totalScore,
                    (int)timerValue.TotalSeconds
                );
            }

            // 8. Показываем окно победы
            WinMessage msg = new WinMessage("УРА!\nКРОССВОРД РЕШЁН!\nОчки: " + totalScore);
            msg.Owner = this;
            msg.ShowDialog();
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
            PlayerSelectWindow ps = new PlayerSelectWindow();

            if (ps.ShowDialog() == true)
            {
                CurrentPlayer = ps.SelectedPlayer;
                BtnSelectPlayer.Content = $"{CurrentPlayer.Name} ({CurrentPlayer.TotalScore})";

                // BtnSelectPlayer.Content = CurrentPlayer.Name;
            }
        }
       

       
        private void StartNewGame()
        {
            undoStack.Clear();
            timer.Stop();
            timerValue = TimeSpan.Zero;
            TimerText.Text = "00:00";
            timer.Start();

            score = 0; // ОБНУЛЯЕМ СЧЁТ
            ScoreText.Text = "0";

            CreateGrid();
            formulas = GenerateCrossword();
            ApplyDifficulty(formulas);
            DrawFormulas(formulas);
            CreateTilesFromFormulas(formulas);
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
                score += 100; // большой бонус за быстрое решение
            else if (minutesElapsed < 10)
                score += 20; // средний бонус

            return score;
        }




        private void InitRandom(int seed)
        {
            rnd = new Random(seed);
        }

    }
}
