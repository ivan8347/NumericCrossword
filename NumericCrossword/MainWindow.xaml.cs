using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NumericCrossword
{
    // -----------------------------
    //  КЛАСС ФОРМУЛЫ (A | Op | B)
    // -----------------------------
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

    public partial class MainWindow : Window
    {
        private const int GridSize = 15;
        private Label[,] cells = new Label[GridSize, GridSize];

        private DispatcherTimer timer;
        private int secondsPassed = 0;

        private Stack<(int row, int col, string oldValue, string newValue)> undoStack
            = new Stack<(int, int, string, string)>();

        private Random rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            CreateGrid();
            InitTimer();
        }

        // -----------------------------
        //  СОЗДАНИЕ СЕТКИ 15×15
        // -----------------------------
        private void CreateGrid()
        {
            CrosswordGrid.RowDefinitions.Clear();
            CrosswordGrid.ColumnDefinitions.Clear();
            CrosswordGrid.Children.Clear();

            for (int i = 0; i < GridSize; i++)
            {
                CrosswordGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
                CrosswordGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            }


            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
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
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string value = (string)e.Data.GetData(DataFormats.StringFormat);
                Label cell = sender as Label;

                int row = Grid.GetRow(cell);
                int col = Grid.GetColumn(cell);

                string oldValue = cell.Content?.ToString() ?? "";

                undoStack.Push((row, col, oldValue, value));

                cell.Content = value;
            }
        }

        // -----------------------------
        //  КНОПКА ОТМЕНА
        // -----------------------------
        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count == 0)
                return;

            var move = undoStack.Pop();
            cells[move.row, move.col].Content = move.oldValue;
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
                    a = b * c; // чтобы деление было целым
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



        // -----------------------------
        //  ПОПЫТКА РАЗМЕСТИТЬ ФОРМУЛУ
        // -----------------------------
        private bool TryPlaceFormula(Formula f, string[,] grid, bool requireIntersection)
        {
            f.Horizontal = rnd.Next(2) == 0;

            int len = 5;

            int maxRow = f.Horizontal ? 15 : 15 - len;
            int maxCol = f.Horizontal ? 15 - len : 15;

            f.Row = rnd.Next(0, maxRow);
            f.Col = rnd.Next(0, maxCol);

            string[] symbols = {
        f.A.ToString(),
        f.Op.ToString(),
        f.B.ToString(),
        "=",
        f.C.ToString()
    };

            bool hasIntersection = false;

            for (int i = 0; i < len; i++)
            {
                int r = f.Row + (f.Horizontal ? 0 : i);
                int c = f.Col + (f.Horizontal ? i : 0);

                string existing = grid[r, c];

                // Пересечение
                if (existing != null)
                {
                    if (existing != symbols[i])
                        return false;

                    hasIntersection = true;
                }

                // Проверка соседей (запрет касаний)
                int[,] around = {
            { r-1, c }, { r+1, c }, { r, c-1 }, { r, c+1 }
        };

                for (int k = 0; k < 4; k++)
                {
                    int rr = around[k, 0];
                    int cc = around[k, 1];

                    if (rr < 0 || rr >= 15 || cc < 0 || cc >= 15)
                        continue;

                    if (grid[rr, cc] != null)
                    {
                        // если это не наша же формула — запрещаем
                        if (!(f.Horizontal && rr == f.Row && Math.Abs(cc - f.Col) < len) &&
                            !(!f.Horizontal && cc == f.Col && Math.Abs(rr - f.Row) < len))
                        {
                            return false;
                        }
                    }
                }
            }

            // Требуем пересечение, кроме первой формулы
            if (requireIntersection && !hasIntersection)
                return false;

            // Записываем формулу
            for (int i = 0; i < len; i++)
            {
                int r = f.Row + (f.Horizontal ? 0 : i);
                int c = f.Col + (f.Horizontal ? i : 0);
                grid[r, c] = symbols[i];
            }

            return true;
        }




        // -----------------------------
        //  ГЕНЕРАЦИЯ ВСЕХ ФОРМУЛ
        // -----------------------------
        private List<Formula> GenerateAllFormulas()
        {
            string[,] grid = new string[15, 15];
            List<Formula> list = new List<Formula>();

            int difficulty = DifficultyBox.SelectedIndex;

            int targetCount = difficulty == 0 ? 12 :
                              difficulty == 1 ? 18 : 25;

            int attempts = 0;

            while (list.Count < targetCount && attempts < 8000)
            {
                attempts++;

                Formula f = GenerateRandomFormula();

                bool requireIntersection = list.Count > 0;

                if (TryPlaceFormula(f, grid, requireIntersection))
                    list.Add(f);
            }

            return list;
        }




        // -----------------------------
        //  СЛОЖНОСТЬ (СКРЫТИЕ ЧИСЕЛ)
        // -----------------------------
        private void ApplyDifficulty(List<Formula> formulas)
        {
            int difficulty = DifficultyBox.SelectedIndex;

            double hideChance;

            switch (difficulty)
            {
                case 0:
                    hideChance = 0.25; // лёгкий
                    break;

                case 1:
                    hideChance = 0.45; // средний
                    break;

                case 2:
                    hideChance = 0.65; // сложный
                    break;

                default:
                    hideChance = 0.25;
                    break;
            }

            foreach (var f in formulas)
            {
                f.HideA = rnd.NextDouble() < hideChance;
                f.HideB = rnd.NextDouble() < hideChance;
                f.HideC = rnd.NextDouble() < hideChance;

            }
        }


        // -----------------------------
        //  ОТОБРАЖЕНИЕ ФОРМУЛ В СЕТКЕ
        // -----------------------------
        private void DrawFormulas(List<Formula> formulas)
        {
            foreach (var f in formulas)
            {
                // A
                var cellA = cells[f.Row, f.Col];
                cellA.Content = f.HideA ? "" : f.A.ToString();
                cellA.Background = Brushes.LightYellow;

                // Op
                var cellOp = cells[f.Row + (f.Horizontal ? 0 : 1),
                                   f.Col + (f.Horizontal ? 1 : 0)];
                cellOp.Content = f.Op;
                cellOp.Background = Brushes.LightYellow;

                // B
                var cellB = cells[f.Row + (f.Horizontal ? 0 : 2),
                                   f.Col + (f.Horizontal ? 2 : 0)];
                cellB.Content = f.HideB ? "" : f.B.ToString();
                cellB.Background = Brushes.LightYellow;

                // "="
                var cellEq = cells[f.Row + (f.Horizontal ? 0 : 3),
                                   f.Col + (f.Horizontal ? 3 : 0)];
                cellEq.Content = "=";
                cellEq.Background = Brushes.LightYellow;

                // C
                var cellC = cells[f.Row + (f.Horizontal ? 0 : 4),
                                   f.Col + (f.Horizontal ? 4 : 0)];
                cellC.Content = f.HideC ? "" : f.C.ToString();
                cellC.Background = Brushes.LightYellow;
            }
        }


        // -----------------------------
        //  СОЗДАНИЕ ПЛИТОК ИЗ СКРЫТЫХ ЧИСЕЛ
        // -----------------------------
        private void CreateTilesFromFormulas(List<Formula> formulas)
        {
            TilesPanel.Children.Clear();

            List<int> numbers = new List<int>();

            foreach (var f in formulas)
            {
                if (f.HideA) numbers.Add(f.A);
                if (f.HideB) numbers.Add(f.B);
                if (f.HideC) numbers.Add(f.C);

            }

            numbers = numbers.OrderBy(x => rnd.Next()).ToList();

            foreach (int n in numbers)
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

            var formulas = GenerateAllFormulas();
            ApplyDifficulty(formulas);
            DrawFormulas(formulas);
            CreateTilesFromFormulas(formulas);
        }
    }
}
