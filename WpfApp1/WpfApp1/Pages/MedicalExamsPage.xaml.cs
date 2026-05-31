using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
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

namespace WpfApp1.Pages
{
    public partial class MedicalExamsPage : Page
    {
        public MedicalExamsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DpExamDate.DisplayDateEnd = DateTime.Today;
            DpExamDate.SelectedDate = DateTime.Today;
            LoadData();
            LoadDonors();
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                ExamsGrid.ItemsSource = db.MedicalExams
                    .Include(m => m.Donor)
                    .Include(m => m.Employee)
                    .OrderByDescending(m => m.ExamDate)
                    .ThenByDescending(m => m.ExamId)
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
                        if (donor.Status == "Постоянное отстранение")
                        {
                            MessageBox.Show("Донор постоянно отстранён. Осмотр запрещён.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                            DisableForm();
                            return;
                        }

                        if (donor.Status == "Временное отстранение" && donor.DisqualifiedUntil.HasValue && donor.DisqualifiedUntil.Value.Date >= DateTime.Today)
                        {
                            MessageBox.Show($"Донор отстранён до {donor.DisqualifiedUntil.Value:dd.MM.yyyy}. Осмотр невозможен.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                            DisableForm();
                            return;
                        }
                        EnableForm();
                    }
                }
            }
        }

        private void DisableForm()
        {
            BtnAdd.IsEnabled = false;
            DpExamDate.IsEnabled = false;
            CmbResult.IsEnabled = false;
            PnlRejection.IsEnabled = false;
            PnlVitals.IsEnabled = false;
            TxtNotes.IsEnabled = false;
        }

        private void EnableForm()
        {
            BtnAdd.IsEnabled = true;
            DpExamDate.IsEnabled = true;
            CmbResult.IsEnabled = true;
            PnlRejection.IsEnabled = true;
            PnlVitals.IsEnabled = true;
            TxtNotes.IsEnabled = true;
        }

        private void CmbResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbResult.SelectedItem is ComboBoxItem item)
            {
                if (item.Content.ToString() == "Отведён")
                {
                    PnlRejection.Visibility = Visibility.Visible;
                    PnlVitals.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PnlRejection.Visibility = Visibility.Collapsed;
                    PnlVitals.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            CmbDonor.SelectedIndex = -1;
            DpExamDate.SelectedDate = DateTime.Today;
            CmbResult.SelectedIndex = -1;
            TxtRejectionReason.Clear();
            TxtHemoglobin.Clear();
            TxtWeight.Clear();
            TxtTemperature.Clear();
            TxtPulse.Clear();
            TxtSystolic.Clear();
            TxtDiastolic.Clear();
            TxtProtein.Clear();
            TxtAlt.Clear();
            TxtNotes.Clear();
            EnableForm();
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
            if (CmbDonor.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать донора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!DpExamDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Необходимо указать дату осмотра.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbResult.SelectedItem == null)
            {
                MessageBox.Show("Необходимо указать результат осмотра.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int donorId = (CmbDonor.SelectedItem as Donor).DonorId;
            DateTime examDate = DpExamDate.SelectedDate.Value.Date;

            using (var db = new BloodBankContext())
            {
                bool exists = db.MedicalExams.Any(m => m.DonorId == donorId && m.ExamDate == examDate);
                if (exists)
                {
                    MessageBox.Show("У донора уже есть осмотр на эту дату.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            string result = (CmbResult.SelectedItem as ComboBoxItem).Content.ToString();

            if (result == "Отведён")
            {
                if (string.IsNullOrWhiteSpace(TxtRejectionReason.Text))
                {
                    MessageBox.Show("Укажите причину отвода.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            else if (result == "Допущен")
            {
                // Убрали жесткие медицинские рамки, оставили только защиту от опечаток (базовые лимиты)
                decimal? weight = ParseDecimal(TxtWeight.Text);
                if (!weight.HasValue || weight <= 0 || weight > 500)
                {
                    MessageBox.Show("Укажите корректный вес.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                decimal? hemoglobin = ParseDecimal(TxtHemoglobin.Text);
                if (!hemoglobin.HasValue || hemoglobin <= 0 || hemoglobin > 500)
                {
                    MessageBox.Show("Укажите корректный гемоглобин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                decimal? temp = ParseDecimal(TxtTemperature.Text);
                if (!temp.HasValue || temp <= 0 || temp > 100)
                {
                    MessageBox.Show("Укажите корректную температуру.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!short.TryParse(TxtSystolic.Text, out short sys) || sys <= 0 || sys > 500)
                {
                    MessageBox.Show("Укажите корректное систолическое давление.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!short.TryParse(TxtDiastolic.Text, out short dia) || dia <= 0 || dia > 500)
                {
                    MessageBox.Show("Укажите корректное диастолическое давление.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!short.TryParse(TxtPulse.Text, out short pulse) || pulse <= 0 || pulse > 500)
                {
                    MessageBox.Show("Укажите корректный пульс.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                decimal? protein = ParseDecimal(TxtProtein.Text);
                if (!protein.HasValue || protein < 0 || protein > 1000)
                {
                    MessageBox.Show("Укажите корректный общий белок.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                decimal? alt = ParseDecimal(TxtAlt.Text);
                if (!alt.HasValue || alt < 0 || alt > 5000)
                {
                    MessageBox.Show("Укажите корректное значение АЛТ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            using (var db = new BloodBankContext())
            {
                string result = (CmbResult.SelectedItem as ComboBoxItem).Content.ToString();

                var exam = new MedicalExam
                {
                    DonorId = (CmbDonor.SelectedItem as Donor).DonorId,
                    EmployeeId = AppSession.CurrentEmployee.EmployeeId,
                    ExamDate = DpExamDate.SelectedDate.Value.Date,
                    Result = result,
                    RejectionReason = result == "Отведён" ? TxtRejectionReason.Text.Trim() : null,
                    HemoglobinGdl = result == "Допущен" ? ParseDecimal(TxtHemoglobin.Text) : null,
                    WeightKg = result == "Допущен" ? ParseDecimal(TxtWeight.Text) : null,
                    TemperatureC = result == "Допущен" ? ParseDecimal(TxtTemperature.Text) : null,
                    PulseBpm = result == "Допущен" ? short.Parse(TxtPulse.Text.Trim()) : (short?)null,
                    SystolicBP = result == "Допущен" ? short.Parse(TxtSystolic.Text.Trim()) : (short?)null,
                    DiastolicBP = result == "Допущен" ? short.Parse(TxtDiastolic.Text.Trim()) : (short?)null,
                    TotalProteinGdl = result == "Допущен" ? ParseDecimal(TxtProtein.Text) : null,
                    AltUL = result == "Допущен" ? ParseDecimal(TxtAlt.Text) : null,
                    Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim()
                };

                db.MedicalExams.Add(exam);

                if (result == "Отведён")
                {
                    var donorToUpdate = db.Donors.Find(exam.DonorId);
                    if (donorToUpdate != null)
                    {
                        var proposedUntil = exam.ExamDate.AddDays(60);
                        if (donorToUpdate.DisqualifiedUntil == null || proposedUntil > donorToUpdate.DisqualifiedUntil)
                        {
                            donorToUpdate.Status = "Временное отстранение";
                            donorToUpdate.DisqualifiedUntil = proposedUntil;
                        }
                        MessageBox.Show($"Донор отстранен до {donorToUpdate.DisqualifiedUntil.Value:dd.MM.yyyy}.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                db.SaveChanges();
            }

            LoadData();
            BtnClear_Click(null, null);
            LoadDonors();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Журнал_Осмотров.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.MedicalExams
                        .Include(m => m.Donor)
                        .Include(m => m.Employee)
                        .OrderByDescending(m => m.ExamDate)
                        .ToList();

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Осмотры");

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Дата";
                        worksheet.Cell(1, 3).Value = "Донор";
                        worksheet.Cell(1, 4).Value = "Врач";
                        worksheet.Cell(1, 5).Value = "Результат";
                        worksheet.Cell(1, 6).Value = "Гемоглобин";
                        worksheet.Cell(1, 7).Value = "АД (Сист/Диаст)";
                        worksheet.Cell(1, 8).Value = "Белок";
                        worksheet.Cell(1, 9).Value = "АЛТ";
                        worksheet.Cell(1, 10).Value = "Причина отвода";
                        worksheet.Cell(1, 11).Value = "Примечание";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].ExamId;
                            worksheet.Cell(i + 2, 2).Value = data[i].ExamDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 3).Value = data[i].Donor.FullName;
                            worksheet.Cell(i + 2, 4).Value = data[i].Employee.FullName;
                            worksheet.Cell(i + 2, 5).Value = data[i].Result;
                            worksheet.Cell(i + 2, 6).Value = data[i].HemoglobinGdl;
                            worksheet.Cell(i + 2, 7).Value = $"{data[i].SystolicBP}/{data[i].DiastolicBP}";
                            worksheet.Cell(i + 2, 8).Value = data[i].TotalProteinGdl;
                            worksheet.Cell(i + 2, 9).Value = data[i].AltUL;
                            worksheet.Cell(i + 2, 10).Value = data[i].RejectionReason;
                            worksheet.Cell(i + 2, 11).Value = data[i].Notes;
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