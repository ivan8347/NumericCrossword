using System.Windows;

namespace NumericCrossword
{
    public partial class PlayerNameWindow : Window
    {
        // Свойство, куда мы запишем введённое имя
        public string PlayerName { get; private set; }

        public PlayerNameWindow()
        {
            InitializeComponent();
        }

        // Нажали OK
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            PlayerName = NameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(PlayerName))
                return;

            DialogResult = true;
            Close();
        }

        // Нажали Отмена
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
