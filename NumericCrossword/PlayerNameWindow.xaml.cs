using System.Windows;

namespace NumericCrossword
{
    public partial class PlayerNameWindow : Window
    {
        public string PlayerName { get; private set; }

        public PlayerNameWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            PlayerName = NameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(PlayerName))
                PlayerName = "Игрок";

            DialogResult = true;
            Close();
        }
    }
}
