using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public partial class DonorsPage : Page
    {
        private Donor _selectedDonor;

        public DonorsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                DonorsGrid.ItemsSource = db.Donors.ToList();
            }
        }

        private void DonorsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DonorsGrid.SelectedItem is Donor donor)
            {
                _selectedDonor = donor;
                TxtFullName.Text = donor.FullName;
                DpBirthDate.SelectedDate = donor.BirthDate;
                TxtPassport.Text = donor.PassportData;
                TxtPhone.Text = donor.ContactPhone;
                TxtEmail.Text = donor.Email;
                TxtAddress.Text = donor.Address;
                TxtNotes.Text = donor.Notes;
                DpDisqualifiedUntil.SelectedDate = donor.DisqualifiedUntil;
                ChkHonorary.IsChecked = donor.IsHonoraryDonor;
                TxtHonoraryNumber.Text = donor.HonoraryDonorNumber;

                SetComboBoxValue(CmbGender, donor.Gender);
                SetComboBoxValue(CmbBloodGroup, donor.BloodGroup);
                SetComboBoxValue(CmbRhFactor, donor.RhFactor);
                SetComboBoxValue(CmbKell, donor.KellAntigen);
                SetComboBoxValue(CmbStatus, donor.Status);
            }
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedIndex = -1;
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbStatus.SelectedItem is ComboBoxItem item)
            {
                string status = item.Content.ToString();
                if (status == "Временное отстранение")
                {
                    PnlDisqualified.Visibility = Visibility.Visible;
                }
                else
                {
                    PnlDisqualified.Visibility = Visibility.Collapsed;
                    DpDisqualifiedUntil.SelectedDate = null;
                }
            }
        }

        private void ChkHonorary_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkHonorary.IsChecked == true)
                TxtHonoraryNumber.Visibility = Visibility.Visible;
            else
            {
                TxtHonoraryNumber.Visibility = Visibility.Collapsed;
                TxtHonoraryNumber.Clear();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _selectedDonor = null;
            TxtFullName.Clear();
            DpBirthDate.SelectedDate = null;
            TxtPassport.Clear();
            TxtPhone.Clear();
            TxtEmail.Clear();
            TxtAddress.Clear();
            TxtNotes.Clear();
            ChkHonorary.IsChecked = false;
            TxtHonoraryNumber.Clear();
            DpDisqualifiedUntil.SelectedDate = null;
            CmbGender.SelectedIndex = -1;
            CmbBloodGroup.SelectedIndex = -1;
            CmbRhFactor.SelectedIndex = -1;
            CmbKell.SelectedIndex = -1;
            CmbStatus.SelectedIndex = -1;
            DonorsGrid.SelectedItem = null;
        }

        private void FormatPhone()
        {
            string phone = TxtPhone.Text;
            if (string.IsNullOrWhiteSpace(phone)) return;

            string digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && (digits.StartsWith("7") || digits.StartsWith("8")))
            {
                TxtPhone.Text = $"+7-{digits.Substring(1, 3)}-{digits.Substring(4, 3)}-{digits.Substring(7, 4)}";
            }
            else if (digits.Length == 10)
            {
                TxtPhone.Text = $"+7-{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
            }
        }

        private void TxtPhone_LostFocus(object sender, RoutedEventArgs e)
        {
            FormatPhone();
        }

        private bool ValidateForm(bool isUpdating)
        {
            FormatPhone();

            if (string.IsNullOrWhiteSpace(TxtFullName.Text) || TxtFullName.Text.Length < 5 || !Regex.IsMatch(TxtFullName.Text, @"^[А-ЯЁа-яё\s\-]+$"))
            {
                MessageBox.Show("ФИО должно содержать минимум 5 символов и состоять только из кириллицы, пробелов и дефисов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!DpBirthDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату рождения.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            DateTime birthDate = DpBirthDate.SelectedDate.Value;
            DateTime today = DateTime.Today;
            int age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;

            if (age < 18)
            {
                MessageBox.Show("Донор должен быть старше 18 лет.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (age > 60)
            {
                MessageBox.Show("Донор старше 60 лет не может быть зарегистрирован.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtPassport.Text) || !Regex.IsMatch(TxtPassport.Text.Trim(), @"^\d{4}\s\d{6}$"))
            {
                MessageBox.Show("Паспортные данные должны быть в формате: 4 цифры серии, пробел, 6 цифр номера.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbGender.SelectedItem == null || CmbBloodGroup.SelectedItem == null || CmbRhFactor.SelectedItem == null || CmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Поля Пол, Группа крови, Резус-фактор и Статус обязательны для выбора.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TxtPhone.Text) && !Regex.IsMatch(TxtPhone.Text.Trim(), @"^\+7-\d{3}-\d{3}-\d{4}$"))
            {
                MessageBox.Show("Номер телефона должен соответствовать формату +7-XXX-XXX-XXXX.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string status = (CmbStatus.SelectedItem as ComboBoxItem).Content.ToString();
            if (status == "Временное отстранение")
            {
                if (!DpDisqualifiedUntil.SelectedDate.HasValue)
                {
                    MessageBox.Show("Для статуса 'Временное отстранение' обязательно указание даты окончания отстранения.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (DpDisqualifiedUntil.SelectedDate.Value <= DateTime.Today)
                {
                    MessageBox.Show("Дата окончания отстранения должна быть исключительно в будущем времени.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            using (var db = new BloodBankContext())
            {
                string passport = TxtPassport.Text.Trim();
                bool passportExists = db.Donors.Any(d => d.PassportData == passport && (!isUpdating || d.DonorId != _selectedDonor.DonorId));
                if (passportExists)
                {
                    MessageBox.Show("Донор с такими паспортными данными уже зарегистрирован.", "Ошибка уникальности данных", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(false)) return;

            using (var db = new BloodBankContext())
            {
                var donor = new Donor
                {
                    FullName = TxtFullName.Text.Trim(),
                    BirthDate = DpBirthDate.SelectedDate.Value,
                    Gender = (CmbGender.SelectedItem as ComboBoxItem).Content.ToString(),
                    PassportData = TxtPassport.Text.Trim(),
                    BloodGroup = (CmbBloodGroup.SelectedItem as ComboBoxItem).Content.ToString(),
                    RhFactor = (CmbRhFactor.SelectedItem as ComboBoxItem).Content.ToString(),
                    KellAntigen = CmbKell.SelectedItem != null ? (CmbKell.SelectedItem as ComboBoxItem).Content.ToString() : null,
                    ContactPhone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim(),
                    Address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim(),
                    IsHonoraryDonor = ChkHonorary.IsChecked == true,
                    HonoraryDonorNumber = ChkHonorary.IsChecked == true ? TxtHonoraryNumber.Text.Trim() : null,
                    Status = (CmbStatus.SelectedItem as ComboBoxItem).Content.ToString(),
                    DisqualifiedUntil = DpDisqualifiedUntil.SelectedDate,
                    Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim(),
                    RegistrationDate = DateTime.Today
                };

                db.Donors.Add(donor);
                db.SaveChanges();
            }
            LoadData();
            BtnClear_Click(null, null);
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDonor == null)
            {
                MessageBox.Show("Выберите донора из списка для сохранения изменений.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!ValidateForm(true)) return;

            using (var db = new BloodBankContext())
            {
                var donor = db.Donors.Find(_selectedDonor.DonorId);
                if (donor != null)
                {
                    donor.FullName = TxtFullName.Text.Trim();
                    donor.BirthDate = DpBirthDate.SelectedDate.Value;
                    donor.Gender = (CmbGender.SelectedItem as ComboBoxItem).Content.ToString();
                    donor.PassportData = TxtPassport.Text.Trim();
                    donor.BloodGroup = (CmbBloodGroup.SelectedItem as ComboBoxItem).Content.ToString();
                    donor.RhFactor = (CmbRhFactor.SelectedItem as ComboBoxItem).Content.ToString();
                    donor.KellAntigen = CmbKell.SelectedItem != null ? (CmbKell.SelectedItem as ComboBoxItem).Content.ToString() : null;
                    donor.ContactPhone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim();
                    donor.Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim();
                    donor.Address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim();
                    donor.IsHonoraryDonor = ChkHonorary.IsChecked == true;
                    donor.HonoraryDonorNumber = ChkHonorary.IsChecked == true ? TxtHonoraryNumber.Text.Trim() : null;
                    donor.Status = (CmbStatus.SelectedItem as ComboBoxItem).Content.ToString();
                    donor.DisqualifiedUntil = DpDisqualifiedUntil.SelectedDate;
                    donor.Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim();

                    db.SaveChanges();
                }
            }
            LoadData();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Реестр_Доноров.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.Donors.ToList();
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Доноры");

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "ФИО";
                        worksheet.Cell(1, 3).Value = "Пол";
                        worksheet.Cell(1, 4).Value = "Дата рождения";
                        worksheet.Cell(1, 5).Value = "Паспорт";
                        worksheet.Cell(1, 6).Value = "Группа крови";
                        worksheet.Cell(1, 7).Value = "Резус-фактор";
                        worksheet.Cell(1, 8).Value = "Телефон";
                        worksheet.Cell(1, 9).Value = "Email";
                        worksheet.Cell(1, 10).Value = "Адрес";
                        worksheet.Cell(1, 11).Value = "Статус";
                        worksheet.Cell(1, 12).Value = "Примечание";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].DonorId;
                            worksheet.Cell(i + 2, 2).Value = data[i].FullName;
                            worksheet.Cell(i + 2, 3).Value = data[i].Gender;
                            worksheet.Cell(i + 2, 4).Value = data[i].BirthDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 5).Value = data[i].PassportData;
                            worksheet.Cell(i + 2, 6).Value = data[i].BloodGroup;
                            worksheet.Cell(i + 2, 7).Value = data[i].RhFactor;
                            worksheet.Cell(i + 2, 8).Value = data[i].ContactPhone;
                            worksheet.Cell(i + 2, 9).Value = data[i].Email;
                            worksheet.Cell(i + 2, 10).Value = data[i].Address;
                            worksheet.Cell(i + 2, 11).Value = data[i].Status;
                            worksheet.Cell(i + 2, 12).Value = data[i].Notes;
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