using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp1.Models;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private int _failedAttempts = 0;
        private DispatcherTimer _lockoutTimer;
        private int _secondsRemaining = 30;

        public LoginWindow()
        {
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _lockoutTimer = new DispatcherTimer();
            _lockoutTimer.Interval = TimeSpan.FromSeconds(1);
            _lockoutTimer.Tick += LockoutTimer_Tick;
        }

        private void LockoutTimer_Tick(object sender, EventArgs e)
        {
            _secondsRemaining--;
            if (_secondsRemaining <= 0)
            {
                _lockoutTimer.Stop();
                _failedAttempts = 0;
                _secondsRemaining = 30;

                LoginButton.IsEnabled = true;
                UsernameBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
                ErrorText.Text = string.Empty;
            }
            else
            {
                ErrorText.Text = $"Система заблокирована. Повторите через {_secondsRemaining} сек.";
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Заполните все поля ввода.";
                return;
            }

            ErrorText.Text = "Проверка данных...";

            try
            {
                using (var db = new BloodBankContext())
                {
                    var employee = await db.Employees
                        .FirstOrDefaultAsync(emp => emp.Login == username && emp.IsActive == true);

                    if (employee != null && employee.Password == password)
                    {
                        AppSession.CurrentEmployee = employee;

                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        _failedAttempts++;
                        if (_failedAttempts >= 3)
                        {
                            LoginButton.IsEnabled = false;
                            UsernameBox.IsEnabled = false;
                            PasswordBox.IsEnabled = false;
                            _lockoutTimer.Start();
                        }
                        else
                        {
                            ErrorText.Text = $"Неверный логин или пароль. Осталось попыток: {3 - _failedAttempts}";
                        }
                    }
                }
            }
           catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}