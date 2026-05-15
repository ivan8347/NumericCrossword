using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NumericCrossword
{
    public partial class WinMessage : Window
    {
        Random rnd = new Random();
        DispatcherTimer timer = new DispatcherTimer();

        public WinMessage(string text)
        {
            InitializeComponent();
            MessageText.Text = text;

            timer.Interval = TimeSpan.FromMilliseconds(120);
            timer.Tick += FireworkTick;
            timer.Start();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            this.Close();
        }

        private void FireworkTick(object sender, EventArgs e)
        {
            for (int i = 0; i < 6; i++)
            {
                Ellipse dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(
                        (byte)rnd.Next(100, 255),
                        (byte)rnd.Next(100, 255),
                        (byte)rnd.Next(100, 255)))
                };

                Canvas.SetLeft(dot, rnd.Next(20, 380));
                Canvas.SetTop(dot, rnd.Next(20, 200));

                FireworksCanvas.Children.Add(dot);

                var anim = new System.Windows.Media.Animation.DoubleAnimation(1, 0,
                    TimeSpan.FromMilliseconds(600));
                anim.Completed += (s, a) => FireworksCanvas.Children.Remove(dot);
                dot.BeginAnimation(OpacityProperty, anim);
            }
        }
    }
}
