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
    /// <summary>
    /// Логика взаимодействия для MedicalExamsPage.xaml
    /// </summary>
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
            TxtBloodPressure.Clear();
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
                if (string.IsNullOrWhiteSpace(TxtRejectionReason.Text) || TxtRejectionReason.Text.Trim().Length < 10)
                {
                    MessageBox.Show("Причина отвода должна содержать не менее 10 символов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            else if (result == "Допущен")
            {
                decimal? hemoglobin = ParseDecimal(TxtHemoglobin.Text);
                if (!hemoglobin.HasValue || hemoglobin < 50 || hemoglobin > 250)
                {
                    MessageBox.Show("Укажите корректный гемоглобин в диапазоне от 50 до 250.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                decimal? weight = ParseDecimal(TxtWeight.Text);
                if (!weight.HasValue || weight < 45 || weight > 200)
                {
                    MessageBox.Show("Укажите корректный вес в диапазоне от 45 до 200.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                decimal? temp = ParseDecimal(TxtTemperature.Text);
                if (!temp.HasValue || temp < 35.0m || temp > 37.2m)
                {
                    if (temp > 37.2m)
                    {
                        MessageBox.Show("Температура выше нормы. Донор не может быть допущен.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    MessageBox.Show("Укажите корректную температуру в диапазоне от 35.0 до 37.2.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(TxtPulse.Text))
                {
                    if (!int.TryParse(TxtPulse.Text.Trim(), out int pulse) || pulse < 40 || pulse > 150)
                    {
                        MessageBox.Show("Укажите корректный пульс в диапазоне от 40 до 150.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }

                if (weight < 50)
                {
                    MessageBox.Show("Донор с весом менее 50 кг не допускается к донации.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (hemoglobin < 120)
                {
                    var msgResult = MessageBox.Show("Гемоглобин ниже нормы. Убедитесь в правильности данных. Продолжить сохранение?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (msgResult == MessageBoxResult.No)
                    {
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
                    PulseBpm = result == "Допущен" && !string.IsNullOrWhiteSpace(TxtPulse.Text) ? short.Parse(TxtPulse.Text.Trim()) : (short?)null,
                    BloodPressure = result == "Допущен" && !string.IsNullOrWhiteSpace(TxtBloodPressure.Text) ? TxtBloodPressure.Text.Trim() : null,
                    Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim()
                };

                db.MedicalExams.Add(exam);

                if (result == "Отведён")
                {
                    var donorToUpdate = db.Donors.Find(exam.DonorId);
                    if (donorToUpdate != null)
                    {
                        donorToUpdate.Status = "Временное отстранение";
                        donorToUpdate.DisqualifiedUntil = exam.ExamDate.AddDays(60);
                        MessageBox.Show($"Донор автоматически отстранён до {donorToUpdate.DisqualifiedUntil.Value:dd.MM.yyyy}.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        worksheet.Cell(1, 6).Value = "Причина отвода";
                        worksheet.Cell(1, 7).Value = "Гемоглобин";
                        worksheet.Cell(1, 8).Value = "Вес";
                        worksheet.Cell(1, 9).Value = "Температура";
                        worksheet.Cell(1, 10).Value = "Давление";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].ExamId;
                            worksheet.Cell(i + 2, 2).Value = data[i].ExamDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 3).Value = data[i].Donor.FullName;
                            worksheet.Cell(i + 2, 4).Value = data[i].Employee.FullName;
                            worksheet.Cell(i + 2, 5).Value = data[i].Result;
                            worksheet.Cell(i + 2, 6).Value = data[i].RejectionReason;
                            worksheet.Cell(i + 2, 7).Value = data[i].HemoglobinGdl;
                            worksheet.Cell(i + 2, 8).Value = data[i].WeightKg;
                            worksheet.Cell(i + 2, 9).Value = data[i].TemperatureC;
                            worksheet.Cell(i + 2, 10).Value = data[i].BloodPressure;
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