using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NumericCrossword
{
    class Formula
    {
        public int Row;
        public int Col;
        public bool Horizontal;

        public int A;
        public int B;
        public int C;
        public char Op;

        public bool HideA;
        public bool HideB;
        public bool HideC;
    }

    class FormulaSlot
    {
        public int Row;
        public int Col;
        public bool Horizontal;
    }

    class CrosswordTemplate
    {
        public List<FormulaSlot> Slots { get; set; } = new List<FormulaSlot>();
    }

    public partial class MainWindow : Window
    {
        private const int Rows = 15;
        private const int Cols = 20;

        private Label[,] cells = new Label[Rows, Cols];

        private DispatcherTimer timer;
        private int secondsPassed = 0;

        private Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> undoStack
     = new Stack<(int, int, string, string, bool)>();


        private Random rnd = new Random();

        private List<CrosswordTemplate> templatesEasy = new List<CrosswordTemplate>();

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
                CrosswordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            cells = new Label[Rows, Cols];

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    Label cell = new Label
                    {
                        Width = 40,
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

                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);

                    cells[r, c] = cell;
                    CrosswordGrid.Children.Add(cell);
                }
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

        private void Cell_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat))
                return;

            string value = (string)e.Data.GetData(DataFormats.StringFormat);
            Label cell = sender as Label;

            int row = Grid.GetRow(cell);
            int col = Grid.GetColumn(cell);

            string oldValue = cell.Content?.ToString() ?? "";

            // ❗ 1. Если ячейка — оператор, запрещаем вставку
            if (oldValue == "+" || oldValue == "-" || oldValue == "*" || oldValue == "/" || oldValue == "=")
            {
                // плитку НЕ удаляем, просто выходим
                return;
            }

            // ❗ 2. Если ячейка — число или пустая — разрешаем вставку
            bool tileRemoved = oldValue == ""; // если клетка была пустой — плитка удалена
            undoStack.Push((row, col, oldValue, value, tileRemoved));

            cell.Content = value;

            RemoveTile(value);
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
            if (undoStack.Count == 0)
                return;

            var move = undoStack.Pop();

            Label cell = cells[move.row, move.col];

            // 1. Восстанавливаем старое значение в клетке
            cell.Content = move.oldValue;

            // 2. Если плитка была удалена — вернуть её
            if (move.tileWasRemoved)
            {
                ReturnTile(move.newValue);
            }
        }
        private void ReturnTile(string value)
        {
            Label tile = new Label
            {
                Content = value,
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
            secondsPassed++;
            TimerText.Text = TimeSpan.FromSeconds(secondsPassed).ToString(@"mm\:ss");
        }

        // -----------------------------
        //  ГЕНЕРАЦИЯ ОДНОЙ ФОРМУЛЫ
        // -----------------------------
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
        // -----------------------------
        //  ШАБЛОНЫ
        // -----------------------------
        private void InitTemplates()
        {
            templatesEasy.Clear();

            var t = new CrosswordTemplate();

            // Центральная горизонтальная формула
            t.Slots.Add(new FormulaSlot { Row = 5, Col = 4, Horizontal = true });

            // Вертикальная формула пересекает по B (индекс 2)
            t.Slots.Add(new FormulaSlot { Row = 3, Col = 6, Horizontal = false });

            // Левая горизонтальная формула пересекает по A (индекс 0)
            t.Slots.Add(new FormulaSlot { Row = 5, Col = 2, Horizontal = true });

            // Правая горизонтальная формула пересекает по C (индекс 4)
            t.Slots.Add(new FormulaSlot { Row = 5, Col = 8, Horizontal = true });

            // Нижняя вертикальная формула пересекает по C (индекс 4)
            // ❗ СДВИГАЕМ НА 1 ВНИЗ, ЧТОБЫ НЕ ПЕРЕКРЫВАТЬСЯ С ВЕРХНЕЙ
            t.Slots.Add(new FormulaSlot { Row = 6, Col = 6, Horizontal = false });

            templatesEasy.Add(t);
        }





        // -----------------------------
        //  ЗАПИСЬ ФОРМУЛЫ В СЕТКУ
        // -----------------------------
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

            double hideChance = difficulty == 0 ? 0.45 :
                                difficulty == 1 ? 0.65 : 0.80;

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

                    if (i == 1 || i == 3)
                        cell.Background = Brushes.LightYellow;
                    else
                        cell.Background = Brushes.LightYellow;

                    if (i == 0 && f.HideA) cell.Content = "";
                    else if (i == 2 && f.HideB) cell.Content = "";
                    else if (i == 4 && f.HideC) cell.Content = "";
                    else cell.Content = symbols[i];
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
            secondsPassed = 0;
            TimerText.Text = "00:00";
            timer.Stop();
            timer.Start();

            CreateGrid();

            var formulas = GenerateCrossword();
            ApplyDifficulty(formulas);
            DrawFormulas(formulas);
            CreateTilesFromFormulas(formulas);
        }
    }
}
