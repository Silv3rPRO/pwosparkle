using PWOBot;
using PWOProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace PWOSparkle
{
    public partial class ChatView : UserControl
    {
        private Dictionary<string, TabItem> _channelTabs;
        private Dictionary<string, TabItem> _pmTabs;
        private TabItem _localChatTab;
        private BotClient _bot;

        public ChatView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
            _localChatTab = new TabItem();
            _localChatTab.Header = "Local";
            _localChatTab.Content = new ChatPanel();
            TabControl.Items.Add(_localChatTab);
            _channelTabs = new Dictionary<string, TabItem>();
            AddChannelTab("All");
            AddChannelTab("Trade");
            AddChannelTab("Battle");
            AddChannelTab("Help");
            _pmTabs = new Dictionary<string, TabItem>();
        }

        public void Client_ChannelsUpdated(IList<ChatChannel> channelList)
        {
            Dispatcher.InvokeAsync(delegate
            {
                foreach (ChatChannel channel in channelList)
                {
                    if (!_channelTabs.ContainsKey(channel.Name))
                    {
                        AddChannelTab(channel.Name);
                    }
                }
                foreach (string key in _channelTabs.Keys.ToArray())
                {
                    if (!(channelList.Any(e => e.Name == key)))
                    {
                        RemoveChannelTab(key);
                    }
                }
            });
        }

        public void Client_LeavePrivateMessage(string conversation, string mode, string leaver)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddPrivateSystemMessage(conversation, mode, leaver, "has closed the PM window");
            });
        }

        public void Client_ChatMessage(string mode, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChatMessage(mode, author, message);
            });
        }

        public void Client_ChannelMessage(string channelName, string mod, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChannelMessage(channelName, mod, author, message);
            });
        }

        public void Client_ChannelSystemMessage(string channelName, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChannelSystemMessage(channelName, message);
            });
        }

        public void Client_PrivateMessage(string conversation, string mode, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddPrivateMessage(conversation, mode, author, message);
            });
        }

        private void AddChannelTab(string tabName)
        {
            TabItem tab = new TabItem();
            tab.Header = '#' + tabName;
            tab.Tag = tabName;
            tab.Content = new ChatPanel();
            _channelTabs[tabName] = tab;
            TabControl.Items.Add(tab);
        }

        private void RemoveChannelTab(string tabName)
        {
            TabControl.Items.Remove(_channelTabs[tabName]);
            _channelTabs.Remove(tabName);
        }

        private void AddPmTab(string tabName)
        {
            TabItem tab = new TabItem();
            tab.Header = tabName;
            tab.Tag = tabName;
            tab.Content = new ChatPanel();
            _pmTabs[tabName] = tab;
            TabControl.Items.Add(tab);
        }

        private void RemovePmTab(string tabName)
        {
            TabControl.Items.Remove(_pmTabs[tabName]);
            _pmTabs.Remove(tabName);
        }

        private void AddChannelMessage(string channelName, string mode, string author, string message)
        {
            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            if (!_channelTabs.ContainsKey(channelName))
            {
                AddChannelTab(channelName);
            }
            AppendMessageToPanel(_channelTabs[channelName].Content as ChatPanel, author + ": " + message);
        }

        private void AddChannelSystemMessage(string channelName, string message)
        {
            if (!_channelTabs.ContainsKey(channelName))
            {
                AddChannelTab(channelName);
            }
            AppendMessageToPanel(_channelTabs[channelName].Content as ChatPanel, "System: " + message);
        }

        private void AddChatMessage(string mode, string author, string message)
        {
            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            AppendMessageToPanel(_localChatTab.Content as ChatPanel, author + ": " + message);
        }

        private void AddPrivateMessage(string conversation, string mode, string author, string message)
        {
            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            if (!_pmTabs.ContainsKey(conversation))
            {
                AddPmTab(conversation);
            }
            AppendMessageToPanel(_pmTabs[conversation].Content as ChatPanel, author + ": " + message);
        }

        private void AddPrivateSystemMessage(string conversation, string mode, string author, string message)
        {
            if (mode != null)
            {
                author = "[" + mode + "]" + author;
            }
            if (!_pmTabs.ContainsKey(conversation))
            {
                AddPmTab(conversation);
            }
            AppendMessageToPanel(_pmTabs[conversation].Content as ChatPanel, author + " " + message);
        }

        private void AppendMessageToPanel(ChatPanel panel, string message)
        {
            MainWindow.AppendLineToTextBox(panel.ChatBox, "[" + DateTime.Now.ToLongTimeString() + "] " + message);
        }

        private void InputChatBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && _bot.Game.IsInitialized)
            {
                SendChatInput(InputChatBox.Text);
                InputChatBox.Clear();
            }
        }

        private void SendChatInput(string text)
        {
            if (text.Length == 0) return;

            TabItem tab = TabControl.SelectedItem as TabItem;
            if (_localChatTab == tab)
            {
                text = text.Replace('|', '#');
                _bot.Game.SendMessage(text);
            }
            else if (_channelTabs.ContainsValue(tab))
            {
                text = text.Replace('|', '#');
                string channelName = (string)tab.Tag;
                ChatChannel channel = _bot.Game.Channels.FirstOrDefault(e => e.Name == channelName);
                if (channel == null)
                {
                    return;
                }
                _bot.Game.SendMessage("/" + channel.Id + " " + text);
            }
            else if (_pmTabs.ContainsValue(tab))
            {
                text = text.Replace("|.|", "");
                _bot.Game.SendPrivateMessage((string)tab.Tag, text);
            }
        }
    }
}
