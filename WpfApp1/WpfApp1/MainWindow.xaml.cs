using System.Windows;
using WpfApp1.Models;
using WpfApp1.Pages;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentEmployee == null)
            {
                MessageBox.Show("Критическая ошибка сессии. Авторизуйтесь заново.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Logout();
                return;
            }

            UserInfoText.Text = AppSession.CurrentEmployee.FullName;
            UserRoleText.Text = $"Роль: {AppSession.CurrentEmployee.Role}";

            SetupRoleAccess(AppSession.CurrentEmployee.Role);
        }

        private void SetupRoleAccess(string role)
        {
            ResetMenuVisibility();

            switch (role)
            {
                case "Регистратор":
                    BtnDonors.Visibility = Visibility.Visible;
                    BtnExams.Visibility = Visibility.Visible;
                    BtnRecipients.Visibility = Visibility.Visible;
                    break;

                case "Медсестра":
                case "Врач":
                    BtnDonors.Visibility = Visibility.Visible;
                    BtnExams.Visibility = Visibility.Visible;
                    BtnDonations.Visibility = Visibility.Visible;
                    BtnRecipients.Visibility = Visibility.Visible;
                    break;

                case "Лаборант":
                    BtnLabTests.Visibility = Visibility.Visible;
                    BtnComponents.Visibility = Visibility.Visible;
                    BtnQuarantine.Visibility = Visibility.Visible;
                    BtnIssues.Visibility = Visibility.Visible;
                    break;

                case "Заведующий":
                    BtnDonors.Visibility = Visibility.Visible;
                    BtnExams.Visibility = Visibility.Visible;
                    BtnDonations.Visibility = Visibility.Visible;
                    BtnLabTests.Visibility = Visibility.Visible;
                    BtnComponents.Visibility = Visibility.Visible;
                    BtnQuarantine.Visibility = Visibility.Visible;
                    BtnIssues.Visibility = Visibility.Visible;
                    BtnRecipients.Visibility = Visibility.Visible;
                    BtnEmployees.Visibility = Visibility.Visible;
                    BtnReports.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ResetMenuVisibility()
        {
            BtnDonors.Visibility = Visibility.Collapsed;
            BtnExams.Visibility = Visibility.Collapsed;
            BtnDonations.Visibility = Visibility.Collapsed;
            BtnLabTests.Visibility = Visibility.Collapsed;
            BtnComponents.Visibility = Visibility.Collapsed;
            BtnQuarantine.Visibility = Visibility.Collapsed;
            BtnIssues.Visibility = Visibility.Collapsed;
            BtnRecipients.Visibility = Visibility.Collapsed;
            BtnEmployees.Visibility = Visibility.Collapsed;
            BtnReports.Visibility = Visibility.Collapsed;
        }

        private void BtnDonors_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new DonorsPage());
        private void BtnExams_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new MedicalExamsPage());
        private void BtnDonations_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new DonationsPage());
        private void BtnLabTests_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new LaboratoryTestsPage());
        private void BtnComponents_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new BloodComponentsPage());
        private void BtnQuarantine_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new PlasmaQuarantinePage());
        private void BtnIssues_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ComponentIssuesPage());
        private void BtnRecipients_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new RecipientsPage());
        private void BtnEmployees_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new EmployeesPage());
        private void BtnReports_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ReportsPage());

        private void BtnLogout_Click(object sender, RoutedEventArgs e) => Logout();

        private void Logout()
        {
            AppSession.CurrentEmployee = null;
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}