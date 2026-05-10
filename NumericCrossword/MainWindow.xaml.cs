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

        private string selectedTileValue = null;
        private Label selectedTileLabel = null;

        private DispatcherTimer timer;
        private int secondsPassed = 0;

        private Stack<(int row, int col, string oldValue, string newValue, bool tileWasRemoved)> undoStack
        = new Stack<(int, int, string, string, bool)>();

        private Stack<(int row, int col, string oldState)> selectionUndoStack = new Stack<(int, int, string)>();

        private Stack<(int row, int col, string oldValue, string newValue)> cellUndoStack
        = new Stack<(int, int, string, string)>();



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


            // работаем только с клетками формул
            if (cell.Background != Brushes.LightYellow && cell.Background != Brushes.LightBlue)
                return;

            // если выбрана плитка — вставляем число (вариант 2)
            if (selectedTileValue != null)
            {
                InsertValueIntoCell(cell, selectedTileValue);

                RemoveTile(selectedTileValue);

                selectedTileLabel.BorderBrush = Brushes.Black;
                selectedTileLabel = null;
                selectedTileValue = null;

                return;
            }

            // иначе — выделение ячейки
            if (cell.Tag as string == "selected")
            {
                cell.Background = Brushes.LightYellow;
                cell.Tag = "normal";
            }
            else
            {
                cell.Background = Brushes.LightBlue;
                cell.Tag = "selected";
            }
        }


        private void InsertValueIntoCell(Label cell, string value)
        {

            // только жёлтые/выделенные клетки
            if (cell.Background != Brushes.LightYellow && cell.Background != Brushes.LightBlue)
                return;

            int row = Grid.GetRow(cell);
            int col = Grid.GetColumn(cell);

            string oldValue = cell.Content?.ToString() ?? "";

            // запрещаем вставку в операторы
            if (oldValue == "+" || oldValue == "-" || oldValue == "*" || oldValue == "/" || oldValue == "=")
                return;

            undoStack.Push((row, col, oldValue, value, oldValue == ""));
            cellUndoStack.Push((row, col, oldValue, value));

            cell.Content = value;
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
                //tile.MouseLeftButtonUp += Tile_Click;
            }

        }
        private void Tile_Click(object sender, MouseButtonEventArgs e)
        {
            Label tile = sender as Label;

            // если плитка уже выбрана — снять выбор
            if (selectedTileLabel == tile)
            {
                tile.BorderBrush = Brushes.Black;
                selectedTileLabel = null;
                selectedTileValue = null;
                return;
            }

            // снять выделение со старой плитки
            if (selectedTileLabel != null)
                selectedTileLabel.BorderBrush = Brushes.Black;

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

            int row = Grid.GetRow(cell);
            int col = Grid.GetColumn(cell);

            string oldValue = cell.Content?.ToString() ?? "";

            // 1. Если ячейка — оператор, запрещаем вставку
            if (oldValue == "+" || oldValue == "-" || oldValue == "*" || oldValue == "/" || oldValue == "=")
            {
                return; // плитку НЕ удаляем
            }

            // 2. Записываем действие в стек плиток
            bool tileRemoved = oldValue == "";
            undoStack.Push((row, col, oldValue, value, tileRemoved));

            // 3. Записываем действие в стек ячеек (для Undo по выделенной ячейке)
            cellUndoStack.Push((row, col, oldValue, value));

            // 4. Меняем содержимое клетки
            cell.Content = value;

            // 5. Удаляем плитку справа
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
            // 1. Ищем выделенную ячейку
            int selRow = -1;
            int selCol = -1;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (cells[r, c].Tag as string == "selected")
                    {
                        selRow = r;
                        selCol = c;
                        break;
                    }
                }
                if (selRow != -1) break;
            }

            // 2. Если выделенная ячейка есть — откатываем её последнее изменение
            if (selRow != -1)
            {
                // ищем последнее действие именно с этой ячейкой
                Stack<(int row, int col, string oldValue, string newValue)> temp = new Stack<(int, int, string, string)>();

                (int row, int col, string oldValue, string newValue) target = (-1, -1, "", "");

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

                // возвращаем остальные записи обратно
                while (temp.Count > 0)
                    cellUndoStack.Push(temp.Pop());

                // если нашли действие — откатываем
                if (target.row != -1)
                {
                    cells[selRow, selCol].Content = target.oldValue;
                    return;
                }
            }

            // 3. Если выделенной нет — обычный Undo плиток
            if (undoStack.Count == 0)
                return;

            var tileMove = undoStack.Pop();

            Label cell = cells[tileMove.row, tileMove.col];

            cell.Content = tileMove.oldValue;

            if (tileMove.tileWasRemoved)
            {
                ReturnTile(tileMove.newValue);
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

            string path = "Data/templates_easy.json";

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
            // 1. Сначала очищаем ВСЕ клетки
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    cells[r, c].Background = Brushes.White;
                    cells[r, c].Tag = null;
                    cells[r, c].Content = "";
                }
            }

            // 2. Теперь рисуем формулы
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

                    // клетки формулы — жёлтые
                    cell.Background = Brushes.LightYellow;
                    cell.Tag = "normal";

                    // скрытые значения
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
