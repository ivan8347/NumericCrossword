using NumericCrossword.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NumericCrossword
{
    public class ChatMessageItem
    {
        public string Sender { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }
        public string RecipientDisplay { get; set; }

        public bool IsMine { get; set; }
        public bool IsPersonalForMe { get; set; }
        public bool IsPersonalGeneral { get; set; }
    }

    public partial class ChatWindow : Window
    {
        private string playerName;
        private DispatcherTimer timer;

        // Список сообщений для ListBox
        public ObservableCollection<ChatMessageItem> Messages { get; set; } = new ObservableCollection<ChatMessageItem>();

        // Событие для главного окна: "пришло новое сообщение"
        public event Action OnNewMessageReceived;

        public ChatWindow(string playerName)
        {
            InitializeComponent();

            this.playerName = playerName;

            ChatList.ItemsSource = Messages;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                var rawMessages = await GameApi.GetChatMessages(playerName);
                if (rawMessages == null) return;

                bool hasNew = false;

                foreach (var m in rawMessages)
                {
                    // Пропускаем дубликаты
                    if (Messages.Any(x => x.Text == m.Text && x.Sender == m.Player))
                        continue;

                    DateTime timeToShow = (m.Time != default(DateTime))
                        ? m.Time.AddHours(3)
                        : DateTime.UtcNow.AddHours(3);

                    string timeString = timeToShow.ToString("HH:mm:ss");

                    bool isMine = m.Player == playerName;
                    bool isPersonalForMe = !string.IsNullOrEmpty(m.Recipient); // сервер отдал лично мне

                    string recipientDisplay = isPersonalForMe ? $"→ {m.Recipient}" : "";

                    var newItem = new ChatMessageItem
                    {
                        Sender = m.Player,
                        Text = m.Text,
                        Time = timeString,
                        RecipientDisplay = recipientDisplay,

                        IsMine = isMine,
                        IsPersonalForMe = isPersonalForMe,
                        IsPersonalGeneral = false
                    };

                    Messages.Add(newItem);
                    hasNew = true;
                }

                if (hasNew)
                {
                    // Пищим только если окно чата не активно (не в фокусе)
                    if (!IsFocused && !IsActive)
                    {
                        SystemSounds.Beep.Play();
                        OnNewMessageReceived?.Invoke();
                    }

                    if (Messages.Count > 0)
                        ChatList.ScrollIntoView(Messages[Messages.Count - 1]);
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chat Error: {ex.Message}");
            }
        }

        // Выбранный кликом адресат (null = пишем всем)
        private string selectedRecipient = null;

        // Клик по имени игрока в чате — выбираем его как адресата
        private void Sender_Click(object sender, MouseButtonEventArgs e)
        {
            var tb = (TextBlock)sender;
            var msg = tb.DataContext as ChatMessageItem;

            // Своё имя кликать бессмысленно
            if (msg == null || msg.IsMine) return;

            selectedRecipient = msg.Sender;
            UpdateRecipientIndicator();
            ChatInput.Focus();
        }

        // Кнопка "×" — снять выбор адресата
        private void ClearRecipient_Click(object sender, RoutedEventArgs e)
        {
            selectedRecipient = null;
            UpdateRecipientIndicator();
        }

        private void UpdateRecipientIndicator()
        {
            if (!string.IsNullOrEmpty(selectedRecipient))
            {
                RecipientIndicatorText.Text = $"Кому: {selectedRecipient}";
                RecipientIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                RecipientIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInput.Text)) return;

            string text = ChatInput.Text.Trim();
            string recipient = selectedRecipient;

            // Оставляем поддержку "@Ник" как запасной вариант (пока приоритет у клика)
            if (recipient == null && text.StartsWith("@"))
            {
                int spaceIndex = text.IndexOf(' ');
                if (spaceIndex > 1)
                {
                    recipient = text.Substring(1, spaceIndex - 1);
                    text = text.Substring(spaceIndex).TrimStart();
                }
            }

            await GameApi.SendChatMessage(playerName, text, recipient);
            ChatInput.Text = "";

            // После отправки снимаем выбор — следующий вопрос может быть уже другому
            if (recipient != null)
            {
                selectedRecipient = null;
                UpdateRecipientIndicator();
            }
        }

        public void AddMessage(string user, string text)
        {
            // Добавляем через коллекцию, а не через ChatList.Items,
            // иначе при установленном ItemsSource будет исключение
            Messages.Add(new ChatMessageItem
            {
                Sender = user,
                Text = text,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                IsMine = user == playerName,
                IsPersonalForMe = false,
                IsPersonalGeneral = false
            });

            if (Messages.Count > 0)
                ChatList.ScrollIntoView(Messages[Messages.Count - 1]);
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ChatInput.Text += btn.Content.ToString();
                ChatInput.Focus();
                ChatInput.CaretIndex = ChatInput.Text.Length;
            }
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            EmojiPanel.Visibility =
                EmojiPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
