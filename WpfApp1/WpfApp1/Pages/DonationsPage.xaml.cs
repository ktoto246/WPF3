using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Models;

namespace WpfApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для DonationsPage.xaml
    /// </summary>
    public partial class DonationsPage : Page
    {
        private int _rejectDonationId = 0;

        public DonationsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DpDonationDate.SelectedDate = DateTime.Today;
            DpDonationDate.DisplayDateEnd = DateTime.Today;
            LoadData();
            LoadDonors();
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                DonationsGrid.ItemsSource = db.Donations
                    .Include(d => d.Donor)
                    .OrderByDescending(d => d.DonationDate)
                    .ThenByDescending(d => d.DonationId)
                    .ToList();
            }
        }

        private void LoadDonors()
        {
            using (var db = new BloodBankContext())
            {
                CmbDonor.ItemsSource = db.Donors.OrderBy(d => d.FullName).ToList();
            }
        }

        private void CmbDonor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDonor.SelectedItem is Donor selectedDonor)
            {
                using (var db = new BloodBankContext())
                {
                    var donor = db.Donors.Find(selectedDonor.DonorId);
                    if (donor != null)
                    {
                        if (donor.Status == "Постоянное отстранение" || (donor.Status == "Временное отстранение" && donor.DisqualifiedUntil.HasValue && donor.DisqualifiedUntil.Value.Date >= DateTime.Today))
                        {
                            MessageBox.Show("Донор отстранён от донаций.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                            DisableForm();
                            return;
                        }

                        var todaysExam = db.MedicalExams
                            .Where(m => m.DonorId == donor.DonorId && m.ExamDate == DateTime.Today && m.Result == "Допущен")
                            .FirstOrDefault();

                        if (todaysExam == null)
                        {
                            MessageBox.Show("У донора нет допуска врача на сегодня. Сначала проведите осмотр.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            DisableForm();
                            return;
                        }

                        CmbExam.ItemsSource = new[] { todaysExam };
                        CmbExam.SelectedIndex = 0;
                        EnableForm();
                    }
                }
            }
        }

        private void DisableForm()
        {
            BtnAdd.IsEnabled = false;
            CmbDonationType.IsEnabled = false;
            TxtVolume.IsEnabled = false;
            DpDonationDate.IsEnabled = false;
            CmbExam.ItemsSource = null;
        }

        private void EnableForm()
        {
            BtnAdd.IsEnabled = true;
            CmbDonationType.IsEnabled = true;
            TxtVolume.IsEnabled = true;
            DpDonationDate.IsEnabled = true;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            CmbDonor.SelectedIndex = -1;
            CmbExam.ItemsSource = null;
            CmbDonationType.SelectedIndex = -1;
            TxtVolume.Clear();
            DpDonationDate.SelectedDate = DateTime.Today;
            EnableForm();
        }

        private bool ValidateForm()
        {
            if (CmbDonor.SelectedItem == null || CmbExam.SelectedItem == null)
            {
                MessageBox.Show("Не выбран донор или отсутствует допуск.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbDonationType.SelectedItem == null)
            {
                MessageBox.Show("Укажите тип донации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(TxtVolume.Text.Trim(), out int volume))
            {
                MessageBox.Show("Объем должен быть целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string type = (CmbDonationType.SelectedItem as ComboBoxItem).Content.ToString();

            if (type == "Цельная кровь" && (volume < 300 || volume > 450)) { MessageBox.Show("Объем цельной крови: 300-450 мл.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (type == "Плазма" && (volume < 400 || volume > 600)) { MessageBox.Show("Объем плазмы: 400-600 мл.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if ((type == "Тромбоциты" || type == "Эритроциты (аферез)" || type == "Гранулоциты") && (volume < 200 || volume > 400)) { MessageBox.Show($"Объем для {type}: 200-400 мл.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }

            int donorId = (CmbDonor.SelectedItem as Donor).DonorId;
            DateTime donationDate = DpDonationDate.SelectedDate.Value.Date;

            using (var db = new BloodBankContext())
            {
                var lastDonation = db.Donations
                    .Where(d => d.DonorId == donorId && d.DonationType == type)
                    .OrderByDescending(d => d.DonationDate)
                    .FirstOrDefault();

                if (lastDonation != null)
                {
                    int daysPassed = (donationDate - lastDonation.DonationDate).Days;
                    int requiredInterval = type == "Цельная кровь" ? 60 : 14;

                    if (daysPassed < requiredInterval)
                    {
                        DateTime nextAllowed = lastDonation.DonationDate.AddDays(requiredInterval);
                        MessageBox.Show($"Минимальный интервал не соблюдён. Следующая возможная дата: {nextAllowed:dd.MM.yyyy}.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                if (type == "Цельная кровь")
                {
                    int currentYear = donationDate.Year;
                    int donationsThisYear = db.Donations.Count(d => d.DonorId == donorId && d.DonationType == "Цельная кровь" && d.DonationDate.Year == currentYear && d.MedicalStatus != "Брак");
                    if (donationsThisYear >= 5)
                    {
                        MessageBox.Show("Донор исчерпал годовой лимит сдачи цельной крови (5 раз в год).", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            return true;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            using (var db = new BloodBankContext())
            {
                var donation = new Donation
                {
                    DonorId = (CmbDonor.SelectedItem as Donor).DonorId,
                    EmployeeId = AppSession.CurrentEmployee.EmployeeId,
                    ExamId = (CmbExam.SelectedItem as MedicalExam).ExamId,
                    DonationDate = DpDonationDate.SelectedDate.Value.Date,
                    DonationType = (CmbDonationType.SelectedItem as ComboBoxItem).Content.ToString(),
                    VolumeMl = int.Parse(TxtVolume.Text.Trim()),
                    MedicalStatus = "На проверке"
                };

                db.Donations.Add(donation);
                db.SaveChanges();
            }

            LoadData();
            BtnClear_Click(null, null);
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentEmployee.Role != "Врач" && AppSession.CurrentEmployee.Role != "Заведующий")
            {
                MessageBox.Show("Нет прав для допуска компонентов.", "Отказ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var donation = (sender as Button).DataContext as Donation;
            if (donation == null || donation.MedicalStatus != "На проверке") return;

            var result = MessageBox.Show("Допустить донацию и разделить на компоненты?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                using (var db = new BloodBankContext())
                {
                    var dbDonation = db.Donations.Find(donation.DonationId);
                    if (dbDonation != null)
                    {
                        dbDonation.MedicalStatus = "Допущено";

                        string baseLot = $"LOT-{DateTime.Now:yyyyMMdd}-{dbDonation.DonationId}";

                        if (dbDonation.DonationType == "Цельная кровь")
                        {
                            db.BloodComponents.Add(new BloodComponent { DonationId = dbDonation.DonationId, LotNumber = baseLot + "-A", ComponentType = "Эритроцитарная масса", VolumeMl = 280, CollectionDate = dbDonation.DonationDate, ExpirationDate = dbDonation.DonationDate.AddDays(42), Status = "В наличии" });
                            db.BloodComponents.Add(new BloodComponent { DonationId = dbDonation.DonationId, LotNumber = baseLot + "-B", ComponentType = "Свежезамороженная плазма", VolumeMl = 170, CollectionDate = dbDonation.DonationDate, ExpirationDate = dbDonation.DonationDate.AddDays(365), Status = "В наличии" });
                        }
                        else if (dbDonation.DonationType == "Плазма")
                        {
                            db.BloodComponents.Add(new BloodComponent { DonationId = dbDonation.DonationId, LotNumber = baseLot + "-A", ComponentType = "Свежезамороженная плазма", VolumeMl = dbDonation.VolumeMl, CollectionDate = dbDonation.DonationDate, ExpirationDate = dbDonation.DonationDate.AddDays(365), Status = "В наличии" });
                        }
                        else if (dbDonation.DonationType == "Тромбоциты")
                        {
                            db.BloodComponents.Add(new BloodComponent { DonationId = dbDonation.DonationId, LotNumber = baseLot + "-A", ComponentType = "Тромбоцитарный концентрат", VolumeMl = dbDonation.VolumeMl, CollectionDate = dbDonation.DonationDate, ExpirationDate = dbDonation.DonationDate.AddDays(5), Status = "В наличии" });
                        }
                        else if (dbDonation.DonationType == "Эритроциты (аферез)")
                        {
                            db.BloodComponents.Add(new BloodComponent { DonationId = dbDonation.DonationId, LotNumber = baseLot + "-A", ComponentType = "Эритроцитарная масса", VolumeMl = dbDonation.VolumeMl, CollectionDate = dbDonation.DonationDate, ExpirationDate = dbDonation.DonationDate.AddDays(42), Status = "В наличии" });
                        }
                        else if (dbDonation.DonationType == "Гранулоциты")
                        {
                            db.BloodComponents.Add(new BloodComponent { DonationId = dbDonation.DonationId, LotNumber = baseLot + "-A", ComponentType = "Гранулоцитарная масса", VolumeMl = dbDonation.VolumeMl, CollectionDate = dbDonation.DonationDate, ExpirationDate = dbDonation.DonationDate.AddDays(1), Status = "В наличии" });
                        }

                        db.SaveChanges();
                    }
                }
                LoadData();
            }
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentEmployee.Role != "Врач" && AppSession.CurrentEmployee.Role != "Заведующий")
            {
                MessageBox.Show("Нет прав для отбраковки компонентов.", "Отказ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var donation = (sender as Button).DataContext as Donation;
            if (donation == null || donation.MedicalStatus != "На проверке") return;

            _rejectDonationId = donation.DonationId;
            TxtRejectReason.Clear();
            RejectOverlay.Visibility = Visibility.Visible;
        }

        private void ConfirmReject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRejectReason.Text))
            {
                MessageBox.Show("Укажите причину брака.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new BloodBankContext())
            {
                var donation = db.Donations.Find(_rejectDonationId);
                if (donation != null)
                {
                    donation.MedicalStatus = "Брак";

                    var newIssue = new ComponentIssue
                    {
                        ComponentId = 0, // Условно, компоненты не созданы, логируем в примечания
                        EmployeeId = AppSession.CurrentEmployee.EmployeeId,
                        IssueDate = DateTime.Today,
                        IssueType = "Списание",
                        WriteOffReason = "Брак на этапе проверки",
                        Comments = $"Причина брака донации: {TxtRejectReason.Text.Trim()}"
                    };

                    db.SaveChanges();
                }
            }

            RejectOverlay.Visibility = Visibility.Collapsed;
            LoadData();
        }

        private void CancelReject_Click(object sender, RoutedEventArgs e)
        {
            RejectOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Донации.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.Donations.Include(d => d.Donor).Include(d => d.Employee).OrderByDescending(d => d.DonationDate).ToList();
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Донации");

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Дата";
                        worksheet.Cell(1, 3).Value = "Донор";
                        worksheet.Cell(1, 4).Value = "Сотрудник";
                        worksheet.Cell(1, 5).Value = "Тип донации";
                        worksheet.Cell(1, 6).Value = "Объем (мл)";
                        worksheet.Cell(1, 7).Value = "Статус";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].DonationId;
                            worksheet.Cell(i + 2, 2).Value = data[i].DonationDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 3).Value = data[i].Donor.FullName;
                            worksheet.Cell(i + 2, 4).Value = data[i].Employee.FullName;
                            worksheet.Cell(i + 2, 5).Value = data[i].DonationType;
                            worksheet.Cell(i + 2, 6).Value = data[i].VolumeMl;
                            worksheet.Cell(i + 2, 7).Value = data[i].MedicalStatus;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }
                }
                MessageBox.Show("Данные успешно экспортированы.", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}