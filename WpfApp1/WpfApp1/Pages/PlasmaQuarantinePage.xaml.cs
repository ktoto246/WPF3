using ClosedXML.Excel;
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
using Microsoft.EntityFrameworkCore;

namespace WpfApp1.Pages
{
    /// <summary>
    /// Логика взаимодействия для PlasmaQuarantinePage.xaml
    /// </summary>
    public partial class PlasmaQuarantinePage : Page
    {
        private PlasmaQuarantine _selectedRecord;

        public PlasmaQuarantinePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            ApplyRoleRestrictions();
        }

        private void ApplyRoleRestrictions()
        {
            string role = AppSession.CurrentEmployee?.Role;
            if (role != "Лаборант" && role != "Заведующий")
            {
                BtnRelease.IsEnabled = false;
                BtnDispose.IsEnabled = false;
                CmbConfirmDonation.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                QuarantineGrid.ItemsSource = db.PlasmaQuarantines
                    .Include(q => q.BloodComponent)
                        .ThenInclude(bc => bc.Donation)
                        .ThenInclude(d => d.Donor)
                    .OrderByDescending(q => q.StartDate)
                    .ToList();
            }
        }

        private void QuarantineGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuarantineGrid.SelectedItem is PlasmaQuarantine record)
            {
                _selectedRecord = record;
                TxtSelectedLot.Text = record.BloodComponent.LotNumber;
                TxtPlannedDate.Text = record.PlannedReleaseDate.ToString("dd.MM.yyyy");

                if (record.Status == "На карантине")
                {
                    BtnRelease.IsEnabled = true;
                    BtnDispose.IsEnabled = true;
                    CmbConfirmDonation.IsEnabled = true;
                    LoadConfirmDonations(record.BloodComponent.Donation.DonorId);
                }
                else
                {
                    BtnRelease.IsEnabled = false;
                    BtnDispose.IsEnabled = false;
                    CmbConfirmDonation.IsEnabled = false;
                    CmbConfirmDonation.ItemsSource = null;
                }
                ApplyRoleRestrictions();
            }
            else
            {
                _selectedRecord = null;
                TxtSelectedLot.Clear();
                TxtPlannedDate.Clear();
                CmbConfirmDonation.ItemsSource = null;
            }
        }

        private void LoadConfirmDonations(int donorId)
        {
            using (var db = new BloodBankContext())
            {
                var originalDonationId = _selectedRecord.BloodComponent.DonationId;
                DateTime minConfirmDate = _selectedRecord.StartDate.AddDays(180);

                var passedDonationIds = db.LaboratoryTests
                    .Where(t => t.OverallResult == "Годен")
                    .Select(t => t.DonationId)
                    .ToList();

                CmbConfirmDonation.ItemsSource = db.Donations
                    .Where(d => d.DonorId == donorId
                             && d.DonationId != originalDonationId
                             && passedDonationIds.Contains(d.DonationId)
                             && d.DonationDate >= minConfirmDate)
                    .OrderByDescending(d => d.DonationDate)
                    .ToList();
            }
        }

        private void BtnRelease_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecord == null) return;

            if (_selectedRecord.PlannedReleaseDate > DateTime.Today)
            {
                MessageBox.Show("Срок 180 дней еще не истек. Снятие с карантина невозможно.", "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbConfirmDonation.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать повторную донацию донора для подтверждения безопасности.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int confirmDonationId = (int)CmbConfirmDonation.SelectedValue;

            using (var db = new BloodBankContext())
            {
                var labTest = db.LaboratoryTests.FirstOrDefault(t => t.DonationId == confirmDonationId);
                if (labTest == null || labTest.OverallResult != "Годен")
                {
                    MessageBox.Show("Повторная донация не имеет подтвержденных годных лабораторных тестов.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var confirmDonation = db.Donations.Find(confirmDonationId);
                if (confirmDonation != null && confirmDonation.DonationDate < _selectedRecord.StartDate.AddDays(180))
                {
                    MessageBox.Show("Подтверждающая донация должна быть получена не ранее чем через 180 дней после исходной донации. Это нарушает требования Приказа №1166н.", "Нарушение протокола", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var qRecord = db.PlasmaQuarantines.Find(_selectedRecord.QuarantineId);
                if (qRecord != null)
                {
                    // Обновляем статус карантина
                    qRecord.Status = "Снят с карантина";
                    qRecord.ReleasedDate = DateTime.Today;
                    qRecord.ReleasedByEmployeeId = AppSession.CurrentEmployee.EmployeeId;
                    qRecord.ConfirmationDonationId = confirmDonationId;
                    qRecord.ConfirmationTestId = labTest.TestId;

                    // Обновляем статус самого пакета крови
                    var component = db.BloodComponents.Find(qRecord.ComponentId);
                    if (component != null)
                    {
                        component.Status = "В наличии";
                    }

                    db.SaveChanges();
                    MessageBox.Show("Компонент успешно снят с карантина и доступен для выдачи.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            LoadData();
            _selectedRecord = null;
            TxtSelectedLot.Clear();
            TxtPlannedDate.Clear();
            CmbConfirmDonation.ItemsSource = null;
        }

        private void BtnDispose_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecord == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите досрочно утилизировать этот компонент?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                using (var db = new BloodBankContext())
                {
                    var qRecord = db.PlasmaQuarantines.Find(_selectedRecord.QuarantineId);
                    if (qRecord != null)
                    {
                        qRecord.Status = "Утилизирован";

                        var component = db.BloodComponents.Find(qRecord.ComponentId);
                        if (component != null)
                        {
                            component.Status = "Утилизировано";
                        }

                        db.SaveChanges();
                    }
                }
                LoadData();
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Журнал_Карантина.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.PlasmaQuarantines.Include(q => q.BloodComponent).ToList();
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Карантин");

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "LOT Партии";
                        worksheet.Cell(1, 3).Value = "Дата начала";
                        worksheet.Cell(1, 4).Value = "Плановый выход";
                        worksheet.Cell(1, 5).Value = "Статус";
                        worksheet.Cell(1, 6).Value = "Дата снятия";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].QuarantineId;
                            worksheet.Cell(i + 2, 2).Value = data[i].BloodComponent.LotNumber;
                            worksheet.Cell(i + 2, 3).Value = data[i].StartDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 4).Value = data[i].PlannedReleaseDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 5).Value = data[i].Status;
                            worksheet.Cell(i + 2, 6).Value = data[i].ReleasedDate?.ToString("yyyy-MM-dd");
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }
                }
                MessageBox.Show("Данные успешно экспортированы.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}