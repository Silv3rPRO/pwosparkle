using Microsoft.Win32;
using PWOBot;
using PWOProtocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PWOSparkle
{
    public partial class MainWindow : Window
    {
        public GameClient Client { get; private set; }
        public BotClient Bot { get; private set; }

        public TeamView Team { get; private set; }
        public InventoryView Inventory { get; private set; }
        public ChatView Chat { get; private set; }

        public MainWindow()
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
#endif
            Thread.CurrentThread.Name = "UI Thread";

            InitializeComponent();

            App.InitializeVersion();

            Bot = new BotClient();
            Bot.StateChanged += Bot_StateChanged;
            Bot.LogMessage += Bot_LogMessage;

            Team = new TeamView(Bot);
            Inventory = new InventoryView();
            Chat = new ChatView(Bot);

            TeamContent.Content = Team;
            InventoryContent.Content = Inventory;
            ChatContent.Content = Chat;

            TeamContent.Visibility = Visibility.Visible;
            InventoryContent.Visibility = Visibility.Collapsed;
            ChatContent.Visibility = Visibility.Collapsed;
            TeamButton.IsChecked = true;

            SetTitle(null);
            
            LogMessage("Running " + App.Name + " by " + App.Author + ", version " + App.Version);

            Task.Run(() => UpdateClients());
        }

        private void SetTitle(string username)
        {
            Title = username == null ? "" : username + " - ";
            Title += App.Name + " " + App.Version;
#if DEBUG
            Title += " (debug)";
#endif
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Dispatcher.Invoke(() => HandleUnhandledException(e.Exception.InnerException));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        private void HandleUnhandledException(Exception ex)
        {
            try
            {
                if (ex != null)
                {
                    File.WriteAllText("crash_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt",
                        App.Name + " " + App.Version + " crash report: " + Environment.NewLine + ex);
                }
                MessageBox.Show(App.Name + " encountered a fatal error. The application will now terminate." + Environment.NewLine +
                    "An error file has been created next to the application.", App.Name + " - Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            catch
            {
            }
        }

        private void UpdateClients()
        {
            lock (Bot)
            {
                if (Client != null)
                {
                    Client.Update();
                }
                Bot.Update();
            }
            Task.Delay(1).ContinueWith((previous) => UpdateClients());
        }

        private async void LoginMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow { Owner = this };
            bool? result = login.ShowDialog();
            if (result != true)
            {
                return;
            }

            LogMessage("Connecting to the server...");
            LoginMenuItem.IsEnabled = false;
            try
            {
                lock (Bot)
                {
                    if (login.HasProxy)
                    {
                        Client = new GameClient(new SocksConnection(login.ProxyVersion, login.ProxyHost, login.ProxyPort, login.ProxyUsername, login.ProxyPassword));
                    }
                    else
                    {
                        Client = new GameClient(new GameConnection());
                    }
                    Client.ConnectionClosed += Client_ConnectionClosed;
                    Client.LoggedIn += Client_LoggedIn;
                    Client.AuthenticationFailed += Client_AuthenticationFailed;
                    Client.PositionUpdated += Client_PositionUpdated;
                    Client.TeamUpdated += Client_TeamUpdated;
                    Client.InventoryUpdated += Client_InventoryUpdated;
                    Client.BattleStarted += Client_BattleStarted;
                    Client.BattleMessage += Client_BattleMessage;
                    Client.BattleEnded += Client_BattleEnded;
                    Client.DialogMessage += Client_DialogMessage;
                    Client.ChatMessage += Chat.Client_ChatMessage;
                    Client.ChannelMessage += Chat.Client_ChannelMessage;
                    Client.ChannelSystemMessage += Chat.Client_ChannelSystemMessage;
                    Client.PrivateMessage += Chat.Client_PrivateMessage;
                    Client.LeavePrivateMessage += Chat.Client_LeavePrivateMessage;
                    Client.ChannelsUpdated += Chat.Client_ChannelsUpdated;
                    Client.SystemMessage += Client_SystemMessage;
                    Bot.SetClient(Client);
                }
                await Client.OpenAsync();
                Client.SendAuthentication(login.Username, login.Password);
                SetTitle(login.Username);
                UpdateBotMenu();
                LogMessage("Connected, authenticating...");
                LogoutMenuItem.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Bot.SetClient(null);
                LogMessage("Could not connect to the server: " + ex.Message);
                LoginMenuItem.IsEnabled = true;
            }
        }

        private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Logging out...");
            try
            {
                lock (Bot)
                {
                    Client.Close();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Could not log out from the server: " + ex.Message);
                LoginMenuItem.IsEnabled = true;
            }
            LogoutMenuItem.IsEnabled = false;
        }

        private void BotScriptMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = App.Name + " Scripts|*.lua;*.txt|All Files|*.*"
            };

            bool? result = openDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                try
                {
                    Bot.LoadScript(openDialog.FileName);
                    BotScriptMenuItem.Header = "Script: \"" + Bot.Script.Name + "\"";
                    LogMessage("Script \"{0}\" by \"{1}\" successfully loaded", Bot.Script.Name, Bot.Script.Author);
                    if (!string.IsNullOrEmpty(Bot.Script.Description))
                    {
                        LogMessage(Bot.Script.Description);
                    }
                    UpdateBotMenu();
                }
                catch (Exception ex)
                {
#if DEBUG
                    LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", Path.GetFileName(openDialog.FileName), ex.ToString());
#else
                    LogMessage("Could not load script {0}: " + Environment.NewLine + "{1}", Path.GetFileName(openDialog.FileName), ex.Message);
#endif
                }
            }
        }

        private void BotStartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Start();
            }
        }

        private void BotStopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Stop();
            }
        }

        private void Client_ConnectionClosed()
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("Disconnected from the server.");
                LoginMenuItem.IsEnabled = true;
                LogoutMenuItem.IsEnabled = false;
                Bot.SetClient(null);
                UpdateBotMenu();
                StatusText.Text = "Offline";
                StatusText.Foreground = Brushes.DarkRed;
            });
        }

        private void Client_LoggedIn()
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("Authenticated successfully!");
                UpdateBotMenu();
                StatusText.Text = "Online";
                StatusText.Foreground = Brushes.DarkGreen;
            });
        }

        private void Client_AuthenticationFailed(AuthenticationResult reason)
        {
            Dispatcher.InvokeAsync(delegate
            {
                string message = "";
                switch (reason)
                {
                    case AuthenticationResult.AlreadyLogged:
                        message = "Already logged in";
                        break;
                    case AuthenticationResult.Banned:
                        message = "You are banned from PWO";
                        break;
                    case AuthenticationResult.InvalidPassword:
                        message = "Invalid password";
                        break;
                    case AuthenticationResult.InvalidUser:
                        message = "Invalid username";
                        break;
                    case AuthenticationResult.InvalidVersion:
                        message = "Outdated client, please wait for an update";
                        break;
                    case AuthenticationResult.Locked:
                        message = "Server locked for maintenance";
                        break;
                }
                LogMessage("Authentication failed: " + message);
            });
        }
        
        private void Client_PositionUpdated(string map, int x, int y)
        {
            Dispatcher.InvokeAsync(delegate
            {
                MapNameText.Text = map;
                PlayerPositionText.Text = "(" + x + "," + y + ")";
            });
        }

        private void Client_TeamUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                IList<Pokemon> team;
                lock (Bot)
                {
                    team = Bot.Game.Team.ToArray();
                }
                Team.PokemonListView.ItemsSource = team;
                Team.PokemonListView.Items.Refresh();
            });
        }

        private void Client_InventoryUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                string money;
                IList<InventoryItem> items;
                lock (Bot)
                {
                    money = Bot.Game.Money.ToString("#,##0");
                    items = Bot.Game.Inventory.ToArray();
                }

                MoneyText.Text = "$" + money;
                Inventory.ItemsListView.ItemsSource = items;
                Inventory.ItemsListView.Items.Refresh();
            });
        }

        private void Client_BattleStarted()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "In battle";
                StatusText.Foreground = Brushes.Blue;
            });
        }

        private void Client_BattleMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message);
            });
        }

        private void Client_BattleEnded()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "Online";
                StatusText.Foreground = Brushes.DarkGreen;
            });
        }

        private void Client_DialogMessage(int dialogId, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage("({0}) {1}", dialogId, message);
            });
        }

        private void Client_SystemMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                message = Regex.Replace(message, @"\[.+?\]", "");
                LogMessage("System: " + message);
            });
        }

        private void Bot_StateChanged(bool state)
        {
            Dispatcher.InvokeAsync(delegate
            {
                UpdateBotMenu();
                LogMessage("Bot " + (state ? "started" : "stopped"));
            });
        }

        private void Bot_LogMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message);
            });
        }

        private void UpdateBotMenu()
        {
            BotStartMenuItem.IsEnabled = Client != null && Client.IsConnected && Bot.Script != null && !Bot.Running;
            BotStopMenuItem.IsEnabled = Client != null && Client.IsConnected && Bot.Running;
        }

        private void LogMessage(string message)
        {
            AppendLineToTextBox(MessageTextBox, "[" + DateTime.Now.ToLongTimeString() + "] " + message);
        }

        private void LogMessage(string format, params object[] args)
        {
            LogMessage(string.Format(format, args));
        }
        
        public static void AppendLineToTextBox(TextBox textBox, string message)
        {
            textBox.AppendText(message + Environment.NewLine);
            if (textBox.Text.Length > 12000)
            {
                string text = textBox.Text;
                text = text.Substring(text.Length - 10000, 10000);
                int index = text.IndexOf(Environment.NewLine);
                if (index != -1)
                {
                    text = text.Substring(index + Environment.NewLine.Length);
                }
                textBox.Text = text;
            }
            textBox.CaretIndex = textBox.Text.Length;
            textBox.ScrollToEnd();
        }

        private void TeamButton_Click(object sender, RoutedEventArgs e)
        {
            TeamContent.Visibility = Visibility.Visible;
            InventoryContent.Visibility = Visibility.Collapsed;
            ChatContent.Visibility = Visibility.Collapsed;
            TeamButton.IsChecked = true;
            InventoryButton.IsChecked = false;
            ChatButton.IsChecked = false;
        }

        private void InventoryButton_Click(object sender, RoutedEventArgs e)
        {
            TeamContent.Visibility = Visibility.Collapsed;
            InventoryContent.Visibility = Visibility.Visible;
            ChatContent.Visibility = Visibility.Collapsed;
            TeamButton.IsChecked = false;
            InventoryButton.IsChecked = true;
            ChatButton.IsChecked = false;
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            TeamContent.Visibility = Visibility.Collapsed;
            InventoryContent.Visibility = Visibility.Collapsed;
            ChatContent.Visibility = Visibility.Visible;
            TeamButton.IsChecked = false;
            InventoryButton.IsChecked = false;
            ChatButton.IsChecked = true;
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(App.Name + " version " + App.Version + ", by " + App.Author + "." + Environment.NewLine + App.Description, App.Name + " - About");
        }

        private void MenuWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://proshine-bot.ml/");
        }
    }
}
