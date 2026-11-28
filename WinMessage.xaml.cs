using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using NumericCrossword.Core;

namespace NumericCrossword
{
    public partial class WinMessage : Window
    {
        Random rnd = new Random();
        DispatcherTimer timer = new DispatcherTimer();

        // локальная победа: просто текст + салют
        public WinMessage(string text)
        {
            InitializeComponent();
            MessageText.Text = text;
            ResultsPanel.Visibility = Visibility.Collapsed;

            StartFireworks();
            AdjustWindowSize();
        }

        // сетевая игра: таблица результатов, высота под число игроков
        public WinMessage(List<GameResult> results)
        {
            InitializeComponent();
            MessageText.Text = "УРА! КРОССВОРД РЕШЁН!";

            FillResults(results);

            StartFireworks();
            AdjustWindowSize();
        }

        private void StartFireworks()
        {
            timer.Interval = TimeSpan.FromMilliseconds(200);   // залпы реже, но мощнее
            timer.Tick += FireworkTick;
            timer.Start();
            FireworkTick(null, null);   // первый залп сразу, без ожидания
        }

        private void FireworkTick(object sender, EventArgs e)
        {
            int bursts = rnd.Next(2,4);
            for (int b = 0; b < bursts; b++)
                CreateBurst();
        }

        private void CreateBurst()
        {
            double w = FireworksCanvas.ActualWidth > 0 ? FireworksCanvas.ActualWidth : 600;
            double h = FireworksCanvas.ActualHeight > 0 ? FireworksCanvas.ActualHeight : 400;

            double ox = w / 2 + (rnd.NextDouble() - 0.5) * w * 0.6;
            double oy = h / 2 + (rnd.NextDouble() - 0.5) * h * 0.6;

            CreateFlash(ox, oy);   // яркая вспышка в центре

            Color[] palette =
            {
        Color.FromRgb(0xFF, 0xD7, 0x00),   // золото
        Color.FromRgb(0xFF, 0xC0, 0x40),
        Color.FromRgb(0xFF, 0xFF, 0xFF),   // белые искры
        Color.FromRgb(0x7F, 0xFF, 0xD4),   // редкая бирюза для контраста
    };

            int particles = 45;
            for (int i = 0; i < particles; i++)
            {
                var color = palette[rnd.Next(palette.Length)];
                double angle = rnd.NextDouble() * Math.PI * 2;
                double distance = 80 + rnd.NextDouble() * 220;
                double tx = ox + Math.Cos(angle) * distance;
                double ty = oy + Math.Sin(angle) * distance;

                var duration = TimeSpan.FromMilliseconds(rnd.Next(400, 700));
                var ease = new System.Windows.Media.Animation.CubicEase
                { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

                if (rnd.Next(3) == 0)   // каждая третья — длинный луч-хвост
                {
                    CreateSparkLine(ox, oy, tx, ty, color, duration, ease);
                }
                else                    // светящаяся крупная искра
                {
                    var dot = new Ellipse
                    {
                        Width = rnd.Next(5, 11),
                        Height = rnd.Next(5, 11),
                        Fill = new SolidColorBrush(color),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = color,
                            BlurRadius = 12,
                            ShadowDepth = 0,
                            Opacity = 1
                        }
                    };
                    Canvas.SetLeft(dot, ox);
                    Canvas.SetTop(dot, oy);
                    FireworksCanvas.Children.Add(dot);

                    var moveX = new System.Windows.Media.Animation.DoubleAnimation(ox, tx, duration) { EasingFunction = ease };
                    var moveY = new System.Windows.Media.Animation.DoubleAnimation(oy, ty, duration) { EasingFunction = ease };
                    var fade = TwinkleFade(duration);   // мерцание перед угасанием
                    moveX.Completed += (s, a) => { if (FireworksCanvas.Children.Contains(dot)) FireworksCanvas.Children.Remove(dot); };

                    dot.BeginAnimation(Canvas.LeftProperty, moveX);
                    dot.BeginAnimation(Canvas.TopProperty, moveY);
                    dot.BeginAnimation(OpacityProperty, fade);
                }
            }
        }

        // вспышка: белое светящееся пятно, быстро разгорается и гаснет
        private void CreateFlash(double x, double y)
        {
            var flash = new Ellipse
            {
                Width = 60,
                Height = 60,
                Fill = new System.Windows.Media.RadialGradientBrush(
                    System.Windows.Media.Color.FromArgb(230, 255, 255, 210),
                    System.Windows.Media.Color.FromArgb(0, 255, 255, 210))
            };
            Canvas.SetLeft(flash, x - 30);
            Canvas.SetTop(flash, y - 30);
            FireworksCanvas.Children.Add(flash);

            
            var fade = new System.Windows.Media.Animation.DoubleAnimation(1, 0,
                TimeSpan.FromMilliseconds(450));
            fade.Completed += (s, a) => { if (FireworksCanvas.Children.Contains(flash)) FireworksCanvas.Children.Remove(flash); };

            flash.BeginAnimation(OpacityProperty, fade);

            var scaleX = new System.Windows.Media.Animation.DoubleAnimation(0.4, 1.6, TimeSpan.FromMilliseconds(300));
            var scaleY = new System.Windows.Media.Animation.DoubleAnimation(0.4, 1.6, TimeSpan.FromMilliseconds(300));
            var transform = new System.Windows.Media.ScaleTransform();
            flash.RenderTransform = transform;
            flash.RenderTransformOrigin = new Point(0.5, 0.5);
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleX);
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleY);
        }

        // луч-хвост: вытянутая линия от центра к краю, летит и тает
        private void CreateSparkLine(double x0, double y0, double x1, double y1,
            Color color, TimeSpan duration, System.Windows.Media.Animation.IEasingFunction ease)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = x0,
                Y1 = y0,
                X2 = x0,
                Y2 = y0,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = rnd.Next(2, 4),
                StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,   // было StrokeStartPointCap
                StrokeEndLineCap = System.Windows.Media.PenLineCap.Round,     // было StrokeEndPointCap
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = color,
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 1
                }
            };

            FireworksCanvas.Children.Add(line);

            var moveX = new System.Windows.Media.Animation.DoubleAnimation(x0, x1, duration) { EasingFunction = ease };
            var moveY = new System.Windows.Media.Animation.DoubleAnimation(y0, y1, duration) { EasingFunction = ease };
            var fade = TwinkleFade(duration);
            moveX.Completed += (s, a) => { if (FireworksCanvas.Children.Contains(line)) FireworksCanvas.Children.Remove(line); };

            line.BeginAnimation(System.Windows.Shapes.Line.X2Property, moveX);
            line.BeginAnimation(System.Windows.Shapes.Line.Y2Property, moveY);
            line.BeginAnimation(OpacityProperty, fade);
        }

        // мерцание: пара вспышек яркости, потом плавное угасание
        private System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames TwinkleFade(TimeSpan duration)
        {
            var fade = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames
            {
                Duration = duration
            };
            fade.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.Zero));
            fade.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0.3, TimeSpan.FromMilliseconds(150)));
            fade.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(300)));
            fade.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0.5, TimeSpan.FromMilliseconds(450)));
            fade.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0,
                TimeSpan.FromMilliseconds(duration.TotalMilliseconds)));

            return fade;
        }



        private void FillResults(List<GameResult> results)
        {
            if (results == null || results.Count == 0)
            {
                ResultsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            ResultsList.Children.Clear();

            foreach (var r in results)
            {
                var row = new Grid();
                row.Margin = new Thickness(0, 8, 0, 8);
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var name = new TextBlock
                {
                    Text = r.PlayerName,
                    FontSize = 24,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                };

                var score = new TextBlock
                {
                    Text = $"{r.Score} очк.",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0x99)),
                    Margin = new Thickness(20, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var time = new TextBlock
                {
                    Text = TimeSpan.FromSeconds(r.TimeSeconds).ToString(@"mm\:ss"),
                    FontSize = 24,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xB0)),
                    Margin = new Thickness(20, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(score, 1);
                Grid.SetColumn(time, 2);
                row.Children.Add(name);
                row.Children.Add(score);
                row.Children.Add(time);

                ResultsList.Children.Add(row);
            }

            ResultsPanel.Visibility = Visibility.Visible;
        }

        // размер окна зависит от числа игроков
        private void AdjustWindowSize()
        {
            int rows = ResultsPanel.Visibility == Visibility.Visible
                ? ResultsList.Children.Count
                : 0;

            double baseHeight = 420;
            double rowHeight = 50;

            Height = rows > 0 ? baseHeight + rows * rowHeight : 450;
            Width = Math.Max(600, 500 + rows * 15);

            Left = SystemParameters.WorkArea.Left +
                   (SystemParameters.WorkArea.Width - Width) / 2;
            Top = SystemParameters.WorkArea.Top +
                  (SystemParameters.WorkArea.Height - Height) / 2;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            this.Close();
        }

      
    }
}
