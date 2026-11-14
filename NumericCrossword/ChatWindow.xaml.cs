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
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Text;

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

        public ObservableCollection<ChatMessageItem> Messages { get; set; } = new ObservableCollection<ChatMessageItem>();

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
                    string recipientDisplay = string.IsNullOrEmpty(m.Recipient) ? "" : $"[{m.Recipient}]";

                    // Пропускаем дубликаты — теперь и по адресату
                    if (Messages.Any(x => x.Text == m.Text && x.Sender == m.Player && x.RecipientDisplay == recipientDisplay))
                        continue;

                    DateTime timeToShow = (m.Time != default(DateTime))
                        ? m.Time.AddHours(3)
                        : DateTime.UtcNow.AddHours(3);

                    bool isMine = m.Player == playerName;
                    bool isPersonalForMe = !string.IsNullOrEmpty(m.Recipient);

                    Messages.Add(new ChatMessageItem
                    {
                        Sender = m.Player,
                        Text = m.Text,
                        Time = timeToShow.ToString("HH:mm:ss"),
                        RecipientDisplay = recipientDisplay,

                        IsMine = isMine,
                        IsPersonalForMe = isPersonalForMe,
                        IsPersonalGeneral = false
                    });

                    hasNew = true;
                }

                if (hasNew)
                {
                    // Пищим только если окно чата не активно
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

        private string selectedRecipient = null;

        private void Sender_Click(object sender, MouseButtonEventArgs e)
        {
            var tb = (TextBlock)sender;
            var msg = tb.DataContext as ChatMessageItem;

            if (msg == null || msg.IsMine) return;

            selectedRecipient = msg.Sender;
            UpdateRecipientIndicator();
            ChatInput.Focus();
        }

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

            // Запасной вариант адресата "@Ник"
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

            if (recipient != null)
            {
                selectedRecipient = null;
                UpdateRecipientIndicator();
            }
        }

        public void AddMessage(string user, string text)
        {
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

        // Универсально: работает и с Tag (кнопки-картинки), и с Content (кнопки с символом)
        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string emoji = btn.Tag as string ?? btn.Content as string;
                if (emoji == null) return;

                ChatInput.Text += emoji;
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

        private static readonly Dictionary<string, string> EmojiImages = new Dictionary<string, string>
        {
            ["😂"] = "/Images/Emoji/air_kiss.gif",   
            ["😊"] = "/Images/Emoji/1f60a.png",   
            ["😍"] = "/Images/Emoji/1f60d.png",
            ["😎"] = "/Images/Emoji/1f60e.png",
            ["🤔"] = "/Images/Emoji/1f914.png",
            ["😴"] = "/Images/Emoji/1f634.png",
            ["😢"] = "/Images/Emoji/1f622.png",
            ["😡"] = "/Images/Emoji/1f621.png",
            ["🤯"] = "/Images/Emoji/1f92f.png",
            ["🥳"] = "/Images/Emoji/1f973.png",
            ["😌"] = "/Images/Emoji/1f60c.png",
            ["👍"] = "/Images/Emoji/1f44d.png",
            ["👎"] = "/Images/Emoji/1f44e.png",
            ["👏"] = "/Images/Emoji/1f44f.png",
            ["🙌"] = "/Images/Emoji/1f64c.png",
            ["💪"] = "/Images/Emoji/1f4aa.png",
            ["🤝"] = "/Images/Emoji/1f91d.png",
            ["✌️"] = "/Images/Emoji/270c.png",
            ["🎉"] = "/Images/Emoji/1f389.png",
            ["🔥"] = "/Images/Emoji/1f525.png",
            ["💯"] = "/Images/Emoji/1f4af.png",
            ["❤️"] = "/Images/Emoji/2764.png",
            ["✨"] = "/Images/Emoji/2728.png",
            ["🏆"] = "/Images/Emoji/1f3c6.png",
            ["⭐"] = "/Images/Emoji/2b50.png",
            ["🥇"] = "/Images/Emoji/1f947.png",
            ["🧩"] = "/Images/Emoji/1f9e9.png",
            ["⏰"] = "/Images/Emoji/23f0.png",
            ["🎯"] = "/Images/Emoji/1f3af.png",
            ["💡"] = "/Images/Emoji/1f4a1.png",
            ["🚀"] = "/Images/Emoji/1f680.png",
        };

        private void MessageText_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var tb = (TextBlock)sender;
            if (tb.DataContext is ChatMessageItem item)
                SetMessageInlines(tb, item.Text);
        }

        private void MessageText_Loaded(object sender, RoutedEventArgs e)
        {
            var tb = (TextBlock)sender;
            if (tb.DataContext is ChatMessageItem item)
                SetMessageInlines(tb, item.Text);
        }

        private void SetMessageInlines(TextBlock tb, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            tb.Inlines.Clear();

            var sb = new StringBuilder();

            void FlushText()
            {
                if (sb.Length == 0) return;
                tb.Inlines.Add(new Run(sb.ToString()) { FontSize = 20 });
                sb.Clear();
            }

            for (int i = 0; i < text.Length; i++)
            {
                // Ищем самый длинный совпадающий эмодзи-ключ с текущей позиции
                string match = null;
                int matchLen = 0;

                foreach (var key in EmojiImages.Keys)
                {
                    if (key.Length > matchLen &&
                        i + key.Length <= text.Length &&
                        string.CompareOrdinal(text, i, key, 0, key.Length) == 0)
                    {
                        match = key;
                        matchLen = key.Length;
                    }
                }

                if (match != null)
                {
                    FlushText();
                    tb.Inlines.Add(CreateEmojiInline(EmojiImages[match], match));
                    i += matchLen - 1;
                }
                else
                {
                    sb.Append(text[i]);
                }
            }
            FlushText();
        }

        private Inline CreateEmojiInline(string resourcePath, string fallbackGlyph)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri("pack://application:,,," + resourcePath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                var img = new Image { Source = bmp, Width = 28, Height = 28 };
                img.ImageFailed += (s, e) =>
                    System.Diagnostics.Debug.WriteLine($"Emoji FAILED: {resourcePath} — {e.ErrorException?.Message}");

                return new InlineUIContainer { Child = img, BaselineAlignment = BaselineAlignment.Center };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Emoji FAILED (init): {resourcePath} — {ex.Message}");
                return new Run(fallbackGlyph) { FontSize = 20 }; // не пустота, а обычный символ
            }
        }
    }
}
