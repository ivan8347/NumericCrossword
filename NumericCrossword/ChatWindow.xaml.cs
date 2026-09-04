using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NumericCrossword.Core;
using System.Windows.Threading;

namespace NumericCrossword
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
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
        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            EmojiPanel.Visibility =
                EmojiPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
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
        public void AddMessage(string user, string text)
        {
            ChatList.Items.Add($"{user}: {text}");
            ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
        }

    }

}
