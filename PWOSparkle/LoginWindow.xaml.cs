using System.Collections.Generic;
using System.Windows;

namespace PWOSparkle
{
    public partial class LoginWindow : Window
    {
        public string Username
        {
            get { return UsernameTextBox.Text.Trim(); }
        }

        public string Password
        {
            get { return PasswordTextBox.Password; }
        }

        public bool HasProxy
        {
            get { return ProxyCheckBox.IsChecked.Value; }
        }

        public int ProxyVersion
        {
            get { return Socks4RadioButton.IsChecked.Value ? 4 : 5; }
        }

        public string ProxyHost
        {
            get { return ProxyHostTextBox.Text.Trim(); }
        }

        public int ProxyPort { get; private set; }

        public string ProxyUsername
        {
            get { return ProxyUsernameTextBox.Text.Trim(); }
        }

        public string ProxyPassword
        {
            get { return ProxyPasswordTextBox.Password; }
        }

        public LoginWindow()
        {
            InitializeComponent();
            ProxyCheckBox_Checked(null, null);

            Title = App.Name + " - " + Title;
            UsernameTextBox.Focus();

            ServerComboBox.ItemsSource = new List<string>() { "Sapphire" };
            ServerComboBox.SelectedIndex = 0;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (Username.Length == 0)
            {
                UsernameTextBox.Focus();
                return;
            }
            if (Password.Length == 0)
            {
                PasswordTextBox.Focus();
                return;
            }
            if (HasProxy)
            {
                int port;
                if (int.TryParse(ProxyPortTextBox.Text.Trim(), out port) && port >= 0 && port <= 65535)
                {
                    ProxyPort = port;
                    DialogResult = true;
                }
            }
            else
            {
                DialogResult = true;
            }
        }

        private void ProxyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Visibility hasProxy = ProxyCheckBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            Visibility isSocks5 = ProxyCheckBox.IsChecked.Value && Socks5RadioButton.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            Visibility hasAuth = ProxyCheckBox.IsChecked.Value && Socks5RadioButton.IsChecked.Value && !AnonymousCheckBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            if (ProxyTypePanel != null)
            {
                ProxyTypePanel.Visibility = hasProxy;
            }
            if (ProxyHostLabel != null)
            {
                ProxyHostLabel.Visibility = hasProxy;
            }
            if (ProxyHostTextBox != null)
            {
                ProxyHostTextBox.Visibility = hasProxy;
            }
            if (ProxyPortLabel != null)
            {
                ProxyPortLabel.Visibility = hasProxy;
            }
            if (ProxyPortTextBox != null)
            {
                ProxyPortTextBox.Visibility = hasProxy;
            }
            if (AnonymousCheckBox != null)
            {
                AnonymousCheckBox.Visibility = isSocks5;
            }
            if (ProxyUsernameLabel != null)
            {
                ProxyUsernameLabel.Visibility = hasAuth;
            }
            if (ProxyPasswordLabel != null)
            {
                ProxyPasswordLabel.Visibility = hasAuth;
            }
            if (ProxyUsernameTextBox != null)
            {
                ProxyUsernameTextBox.Visibility = hasAuth;
            }
            if (ProxyPasswordTextBox != null)
            {
                ProxyPasswordTextBox.Visibility = hasAuth;
            }
        }
    }
}
