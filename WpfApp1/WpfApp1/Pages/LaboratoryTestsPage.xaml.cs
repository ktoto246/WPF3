using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public partial class LaboratoryTestsPage : Page
    {
        public LaboratoryTestsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DpTestDate.SelectedDate = DateTime.Today;
            DpTestDate.DisplayDateEnd = DateTime.Today;
            LoadData();
            LoadPendingDonations();
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                TestsGrid.ItemsSource = db.LaboratoryTests
                    .Include(t => t.Donation)
                    .Include(t => t.Employee)
                    .OrderByDescending(t => t.TestDate)
                    .ThenByDescending(t => t.TestId)
                    .ToList();
            }
        }

        private void LoadPendingDonations()
        {
            using (var db = new BloodBankContext())
            {
                var testedDonationIds = db.LaboratoryTests.Select(t => t.DonationId).ToList();
                CmbDonation.ItemsSource = db.Donations
                    .Where(d => !testedDonationIds.Contains(d.DonationId) && d.MedicalStatus != "Брак")
                    .OrderBy(d => d.DonationNumber)
                    .ToList();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            CmbDonation.SelectedIndex = -1;
            DpTestDate.SelectedDate = DateTime.Today;
            CmbHIV.SelectedIndex = -1;
            CmbHBsAg.SelectedIndex = -1;
            CmbHCV.SelectedIndex = -1;
            CmbSyphilis.SelectedIndex = -1;
            CmbNAT_HIV.SelectedIndex = -1;
            CmbNAT_HBV.SelectedIndex = -1;
            CmbNAT_HCV.SelectedIndex = -1;
            TxtAlt.Clear();
            CmbOverall.SelectedIndex = -1;
            TxtNotes.Clear();
        }

        private decimal? ParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string formattedInput = input.Replace(',', '.');
            if (decimal.TryParse(formattedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return null;
        }

        private bool ValidateForm()
        {
            if (AppSession.CurrentEmployee.Role != "Лаборант" && AppSession.CurrentEmployee.Role != "Заведующий")
            {
                MessageBox.Show("Внесение результатов доступно только лаборанту или заведующему.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (CmbDonation.SelectedItem == null)
            {
                MessageBox.Show("Выберите донацию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!DpTestDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату тестирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbHIV.SelectedItem == null || CmbHBsAg.SelectedItem == null || CmbHCV.SelectedItem == null || CmbSyphilis.SelectedItem == null)
            {
                MessageBox.Show("Все основные инфекции (ВИЧ, HBsAg, HCV, Сифилис) обязательны для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbOverall.SelectedItem == null)
            {
                MessageBox.Show("Укажите итоговый результат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            decimal? alt = ParseDecimal(TxtAlt.Text);
            if (!string.IsNullOrWhiteSpace(TxtAlt.Text) && !alt.HasValue)
            {
                MessageBox.Show("Некорректное значение АЛТ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (alt.HasValue && alt > 44)
            {
                string overallResult = CmbOverall.SelectedItem != null ? (CmbOverall.SelectedItem as ComboBoxItem).Content.ToString() : "";
                if (overallResult == "Годен")
                {
                    MessageBox.Show($"АЛТ = {alt} Ед/л превышает норму (≤44 Ед/л). Итоговый результат не может быть 'Годен'. Установите статус 'Брак'.", "Нарушение протокола", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            string overall = (CmbOverall.SelectedItem as ComboBoxItem).Content.ToString();
            string hiv = (CmbHIV.SelectedItem as ComboBoxItem).Content.ToString();
            string hbsag = (CmbHBsAg.SelectedItem as ComboBoxItem).Content.ToString();
            string hcv = (CmbHCV.SelectedItem as ComboBoxItem).Content.ToString();
            string syphilis = (CmbSyphilis.SelectedItem as ComboBoxItem).Content.ToString();

            if (overall == "Годен" && (hiv != "Отрицательный" || hbsag != "Отрицательный" || hcv != "Отрицательный" || syphilis != "Отрицательный"))
            {
                MessageBox.Show("Нельзя поставить статус 'Годен' при наличии положительных или сомнительных результатов ИФА.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            using (var db = new BloodBankContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        string overall = (CmbOverall.SelectedItem as ComboBoxItem).Content.ToString();
                        int donationId = (CmbDonation.SelectedItem as Donation).DonationId;

                        var test = new LaboratoryTest
                        {
                            DonationId = donationId,
                            EmployeeId = AppSession.CurrentEmployee.EmployeeId,
                            TestDate = DpTestDate.SelectedDate.Value.Date,
                            HIV_Result = (CmbHIV.SelectedItem as ComboBoxItem).Content.ToString(),
                            HBsAg_Result = (CmbHBsAg.SelectedItem as ComboBoxItem).Content.ToString(),
                            HCV_Result = (CmbHCV.SelectedItem as ComboBoxItem).Content.ToString(),
                            Syphilis_Result = (CmbSyphilis.SelectedItem as ComboBoxItem).Content.ToString(),
                            NAT_HIV = CmbNAT_HIV.SelectedItem != null ? (CmbNAT_HIV.SelectedItem as ComboBoxItem).Content.ToString() : null,
                            NAT_HBV = CmbNAT_HBV.SelectedItem != null ? (CmbNAT_HBV.SelectedItem as ComboBoxItem).Content.ToString() : null,
                            NAT_HCV = CmbNAT_HCV.SelectedItem != null ? (CmbNAT_HCV.SelectedItem as ComboBoxItem).Content.ToString() : null,
                            AltUL = ParseDecimal(TxtAlt.Text),
                            OverallResult = overall,
                            Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim()
                        };

                        db.LaboratoryTests.Add(test);

                        if (overall == "Брак")
                        {
                            var donation = db.Donations.Find(donationId);
                            if (donation != null)
                            {
                                donation.MedicalStatus = "Брак";
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Ошибка при сохранении результатов.", "Сбой", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            LoadData();
            LoadPendingDonations();
            BtnClear_Click(null, null);
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Журнал_лаборатории.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.LaboratoryTests.Include(t => t.Donation).Include(t => t.Employee).OrderByDescending(t => t.TestDate).ToList();
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Лаборатория");

                        worksheet.Cell(1, 1).Value = "ID Теста";
                        worksheet.Cell(1, 2).Value = "Дата";
                        worksheet.Cell(1, 3).Value = "№ Донации";
                        worksheet.Cell(1, 4).Value = "Лаборант";
                        worksheet.Cell(1, 5).Value = "ВИЧ";
                        worksheet.Cell(1, 6).Value = "HBsAg";
                        worksheet.Cell(1, 7).Value = "HCV";
                        worksheet.Cell(1, 8).Value = "Сифилис";
                        worksheet.Cell(1, 9).Value = "НАТ ВИЧ";
                        worksheet.Cell(1, 10).Value = "НАТ HBV";
                        worksheet.Cell(1, 11).Value = "НАТ HCV";
                        worksheet.Cell(1, 12).Value = "АЛТ";
                        worksheet.Cell(1, 13).Value = "Итог";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].TestId;
                            worksheet.Cell(i + 2, 2).Value = data[i].TestDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 3).Value = data[i].Donation.DonationNumber;
                            worksheet.Cell(i + 2, 4).Value = data[i].Employee.FullName;
                            worksheet.Cell(i + 2, 5).Value = data[i].HIV_Result;
                            worksheet.Cell(i + 2, 6).Value = data[i].HBsAg_Result;
                            worksheet.Cell(i + 2, 7).Value = data[i].HCV_Result;
                            worksheet.Cell(i + 2, 8).Value = data[i].Syphilis_Result;
                            worksheet.Cell(i + 2, 9).Value = data[i].NAT_HIV;
                            worksheet.Cell(i + 2, 10).Value = data[i].NAT_HBV;
                            worksheet.Cell(i + 2, 11).Value = data[i].NAT_HCV;
                            worksheet.Cell(i + 2, 12).Value = data[i].AltUL;
                            worksheet.Cell(i + 2, 13).Value = data[i].OverallResult;
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