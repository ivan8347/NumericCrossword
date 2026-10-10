using NumericCrossword.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NumericCrossword
{
    public partial class ChatWindow : Window
    {
        private string playerName;
        private DispatcherTimer timer;

        public ChatWindow(string playerName)
        {
            InitializeComponent();
            this.playerName = playerName;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            var messages = await GameApi.GetChatMessages();
            if (messages == null) return;

            ChatList.Items.Clear();
            foreach (var m in messages)
            {
                ChatList.Items.Add($"{m.Player}: {m.Text}");
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInput.Text)) return;

            await GameApi.SendChatMessage(playerName, ChatInput.Text);
            ChatInput.Text = "";
        }

        public void AddMessage(string user, string text)
        {
            ChatList.Items.Add($"{user}: {text}");
            ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ChatInput.Text += btn.Content.ToString();
                ChatInput.CaretIndex = ChatInput.Text.Length;
                ChatInput.Focus();
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
