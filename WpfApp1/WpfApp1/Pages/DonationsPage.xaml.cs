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
                    .Include(d => d.Employee)
                    .OrderByDescending(d => d.DonationDate)
                    .ThenByDescending(d => d.DonationId)
                    .ToList();
            }
        }

        private void LoadDonors()
        {
            using (var db = new BloodBankContext())
            {
                DateTime today = DateTime.Today;

                var eligibleDonorIds = db.MedicalExams
                    .Where(m => m.ExamDate == today && m.Result == "Допущен")
                    .Select(m => m.DonorId)
                    .ToList();

                var alreadyDonatedIds = db.Donations
                    .Where(d => d.DonationDate == today)
                    .Select(d => d.DonorId)
                    .ToList();

                var availableDonorIds = eligibleDonorIds.Except(alreadyDonatedIds).ToList();

                CmbDonor.ItemsSource = db.Donors
                    .Where(d => availableDonorIds.Contains(d.DonorId))
                    .OrderBy(d => d.FullName)
                    .ToList();
            }
        }

        private void CmbDonor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDonor.SelectedItem is Donor selectedDonor)
            {
                using (var db = new BloodBankContext())
                {
                    var todaysExam = db.MedicalExams
                        .FirstOrDefault(m => m.DonorId == selectedDonor.DonorId && m.ExamDate == DateTime.Today && m.Result == "Допущен");

                    if (todaysExam != null && CmbExam != null)
                    {
                        CmbExam.ItemsSource = new[] { todaysExam };
                        CmbExam.SelectedIndex = 0;
                    }
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            CmbDonor.SelectedIndex = -1;
            if (CmbExam != null) CmbExam.ItemsSource = null;
            CmbDonationType.SelectedIndex = -1;
            TxtVolume.Clear();
            DpDonationDate.SelectedDate = DateTime.Today;
        }

        private bool ValidateForm()
        {
            if (CmbDonor.SelectedItem == null)
            {
                MessageBox.Show("Не выбран донор. В списке доступны только доноры, успешно прошедшие медосмотр СЕГОДНЯ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            DateTime donationDate = DpDonationDate.SelectedDate ?? DateTime.Today;
            string donorGender = (CmbDonor.SelectedItem as Donor).Gender;

            using (var db = new BloodBankContext())
            {
                var lastDonation = db.Donations
                    .Where(d => d.DonorId == donorId && d.DonationType == type)
                    .OrderByDescending(d => d.DonationDate)
                    .FirstOrDefault();

                if (lastDonation != null)
                {
                    int daysPassed = (donationDate - lastDonation.DonationDate).Days;

                    int requiredInterval = type switch
                    {
                        "Цельная кровь" => 60,
                        "Эритроциты (аферез)" => 30,
                        "Гранулоциты" => 30,
                        _ => 14
                    };

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

                    int annualLimit = donorGender == "Ж" ? 4 : 5;

                    if (donationsThisYear >= annualLimit)
                    {
                        MessageBox.Show($"Донор исчерпал годовой лимит сдачи цельной крови ({annualLimit} раз(а) в год для данного пола).", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                int donorId = (CmbDonor.SelectedItem as Donor).DonorId;
                DateTime today = DpDonationDate.SelectedDate ?? DateTime.Today;

                var todayExam = db.MedicalExams
                    .FirstOrDefault(m => m.DonorId == donorId && m.ExamDate == today && m.Result == "Допущен");

                string newDonationNumber = $"DON-{today:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                var newDonation = new Donation
                {
                    DonationNumber = newDonationNumber,
                    DonorId = donorId,
                    EmployeeId = AppSession.CurrentEmployee.EmployeeId,
                    ExamId = todayExam?.ExamId,
                    DonationDate = today,
                    DonationType = (CmbDonationType.SelectedItem as ComboBoxItem).Content.ToString(),
                    VolumeMl = int.Parse(TxtVolume.Text.Trim()),
                    MedicalStatus = "На проверке"
                };

                db.Donations.Add(newDonation);
                db.SaveChanges();
                MessageBox.Show("Донация успешно зарегистрирована.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadData();
            LoadDonors();
            BtnClear_Click(null, null);
        }

        private void CreatePlasmaWithQuarantine(BloodBankContext db, int donationId, string lotNumber, int volumeMl, DateTime collectionDate)
        {
            var plasmaComponent = new BloodComponent
            {
                DonationId = donationId,
                LotNumber = lotNumber,
                ComponentType = "Свежезамороженная плазма",
                VolumeMl = volumeMl,
                CollectionDate = collectionDate,
                ExpirationDate = collectionDate.AddDays(365),
                Status = "На карантине"
            };

            db.BloodComponents.Add(plasmaComponent);
            db.SaveChanges();

            var quarantine = new PlasmaQuarantine
            {
                ComponentId = plasmaComponent.ComponentId,
                StartDate = collectionDate,
                Status = "На карантине"
            };

            db.PlasmaQuarantines.Add(quarantine);
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

            // ЖЕСТКАЯ ПРОВЕРКА ЛАБОРАТОРИИ
            using (var checkDb = new BloodBankContext())
            {
                var labTest = checkDb.LaboratoryTests.FirstOrDefault(t => t.DonationId == donation.DonationId);

                if (labTest == null)
                {
                    MessageBox.Show("Донация еще не прошла лабораторное исследование! Допуск невозможен.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (labTest.OverallResult == "Брак")
                {
                    MessageBox.Show("Лаборатория выявила инфекции (Брак). Эту донацию нужно отбраковать, а не допускать!", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var result = MessageBox.Show("Лаборатория пройдена успешно. Допустить донацию и разделить на компоненты?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                            CreatePlasmaWithQuarantine(db, dbDonation.DonationId, baseLot + "-B", 170, dbDonation.DonationDate);
                        }
                        else if (dbDonation.DonationType == "Плазма")
                        {
                            CreatePlasmaWithQuarantine(db, dbDonation.DonationId, baseLot + "-A", dbDonation.VolumeMl, dbDonation.DonationDate);
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
            if (TxtRejectReason != null) TxtRejectReason.Clear();
            if (RejectOverlay != null) RejectOverlay.Visibility = Visibility.Visible;
        }

        private void ConfirmReject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRejectReason?.Text))
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
                        ComponentId = 0,
                        EmployeeId = AppSession.CurrentEmployee.EmployeeId,
                        IssueDate = DateTime.Today,
                        IssueType = "Списание",
                        WriteOffReason = "Брак на этапе проверки/лаборатории",
                        Comments = $"Причина брака донации: {TxtRejectReason.Text.Trim()}"
                    };

                    db.ComponentIssues.Add(newIssue);
                    db.SaveChanges();
                }
            }

            if (RejectOverlay != null) RejectOverlay.Visibility = Visibility.Collapsed;
            LoadData();
        }

        private void CancelReject_Click(object sender, RoutedEventArgs e)
        {
            if (RejectOverlay != null) RejectOverlay.Visibility = Visibility.Collapsed;
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
                        worksheet.Cell(1, 3).Value = "Номер донации";
                        worksheet.Cell(1, 4).Value = "Донор";
                        worksheet.Cell(1, 5).Value = "Сотрудник";
                        worksheet.Cell(1, 6).Value = "Тип донации";
                        worksheet.Cell(1, 7).Value = "Объем (мл)";
                        worksheet.Cell(1, 8).Value = "Статус";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].DonationId;
                            worksheet.Cell(i + 2, 2).Value = data[i].DonationDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 3).Value = data[i].DonationNumber;
                            worksheet.Cell(i + 2, 4).Value = data[i].Donor.FullName;
                            worksheet.Cell(i + 2, 5).Value = data[i].Employee.FullName;
                            worksheet.Cell(i + 2, 6).Value = data[i].DonationType;
                            worksheet.Cell(i + 2, 7).Value = data[i].VolumeMl;
                            worksheet.Cell(i + 2, 8).Value = data[i].MedicalStatus;
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