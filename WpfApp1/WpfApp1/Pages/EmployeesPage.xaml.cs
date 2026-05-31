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
    public partial class EmployeesPage : Page
    {
        private Employee _selectedEmployee;

        public EmployeesPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentEmployee == null || AppSession.CurrentEmployee.Role != "Заведующий")
            {
                MessageBox.Show("Доступ запрещен. Страница доступна только Заведующему.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadData();
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                EmployeesGrid.ItemsSource = db.Employees
                    .OrderByDescending(emp => emp.IsActive)
                    .ThenBy(emp => emp.FullName)
                    .ToList();
            }
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmployeesGrid.SelectedItem is Employee emp)
            {
                _selectedEmployee = emp;
                TxtFullName.Text = emp.FullName;
                TxtPosition.Text = emp.Position;
                TxtContactInfo.Text = emp.ContactInfo;
                TxtLogin.Text = emp.Login;
                TxtPassword.Text = emp.Password;

                foreach (ComboBoxItem item in CmbRole.Items)
                {
                    if (item.Content.ToString() == emp.Role)
                    {
                        CmbRole.SelectedItem = item;
                        break;
                    }
                }

                BtnAdd.IsEnabled = false;
                BtnSave.IsEnabled = true;
                BtnToggleActive.IsEnabled = true;

                // Умная смена цвета и текста кнопки
                if (emp.IsActive)
                {
                    BtnToggleActive.Content = "Деактивировать";
                    BtnToggleActive.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DC2626"));
                    BtnToggleActive.Foreground = System.Windows.Media.Brushes.White;
                }
                else
                {
                    BtnToggleActive.Content = "Активировать";
                    BtnToggleActive.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
                    BtnToggleActive.Foreground = System.Windows.Media.Brushes.White;
                }
            }
            else
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            _selectedEmployee = null;
            TxtFullName.Clear();
            TxtPosition.Clear();
            TxtContactInfo.Clear();
            TxtLogin.Clear();
            TxtPassword.Clear();
            CmbRole.SelectedIndex = -1;
            EmployeesGrid.SelectedItem = null;

            BtnAdd.IsEnabled = true;
            BtnSave.IsEnabled = false;
            BtnToggleActive.IsEnabled = false;

            BtnToggleActive.Content = "Деактив.";
            BtnToggleActive.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"));
            BtnToggleActive.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151"));
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TxtFullName.Text) || TxtFullName.Text.Length < 5)
            {
                MessageBox.Show("ФИО должно содержать минимум 5 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtPosition.Text) || TxtPosition.Text.Length < 3)
            {
                MessageBox.Show("Должность должна содержать минимум 3 символа.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (CmbRole.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать роль сотрудника.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TxtContactInfo.Text) && !Regex.IsMatch(TxtContactInfo.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Email введен в неверном формате.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtLogin.Text) || string.IsNullOrWhiteSpace(TxtPassword.Text))
            {
                MessageBox.Show("Поля Логин и Пароль обязательны для заполнения.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool CheckLastAdminConstraint(BloodBankContext db, Employee currentRecord, string newRole, bool newIsActive)
        {
            if (currentRecord.Role == "Заведующий" && currentRecord.IsActive == true)
            {
                if (newRole != "Заведующий" || newIsActive == false)
                {
                    int activeAdminsCount = db.Employees.Count(e => e.Role == "Заведующий" && e.IsActive == true);
                    if (activeAdminsCount <= 1)
                    {
                        MessageBox.Show("В системе должен оставаться хотя бы один активный Заведующий. Добавьте замену.", "Ограничение системы", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (db.Employees.Any(emp => emp.Login == TxtLogin.Text.Trim()))
                {
                    MessageBox.Show("Сотрудник с таким логином уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newEmp = new Employee
                {
                    FullName = TxtFullName.Text.Trim(),
                    Position = TxtPosition.Text.Trim(),
                    Role = (CmbRole.SelectedItem as ComboBoxItem).Content.ToString(),
                    ContactInfo = string.IsNullOrWhiteSpace(TxtContactInfo.Text) ? null : TxtContactInfo.Text.Trim(),
                    Login = TxtLogin.Text.Trim(),
                    Password = TxtPassword.Text,
                    IsActive = true
                };

                db.Employees.Add(newEmp);
                db.SaveChanges();
            }
            LoadData();
            ClearForm();
            MessageBox.Show("Сотрудник успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null) return;
            if (!ValidateForm()) return;

            using (var db = new BloodBankContext())
            {
                if (db.Employees.Any(emp => emp.Login == TxtLogin.Text.Trim() && emp.EmployeeId != _selectedEmployee.EmployeeId))
                {
                    MessageBox.Show("Этот логин уже занят другим сотрудником.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var emp = db.Employees.Find(_selectedEmployee.EmployeeId);
                if (emp != null)
                {
                    string newRole = (CmbRole.SelectedItem as ComboBoxItem).Content.ToString();

                    // Проверяем, не пытаемся ли мы изменить роль у последнего админа
                    if (!CheckLastAdminConstraint(db, emp, newRole, emp.IsActive)) return;

                    emp.FullName = TxtFullName.Text.Trim();
                    emp.Position = TxtPosition.Text.Trim();
                    emp.Role = newRole;
                    emp.ContactInfo = string.IsNullOrWhiteSpace(TxtContactInfo.Text) ? null : TxtContactInfo.Text.Trim();
                    emp.Login = TxtLogin.Text.Trim();
                    emp.Password = TxtPassword.Text;

                    db.SaveChanges();
                }
            }
            LoadData();
            MessageBox.Show("Данные сотрудника обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnToggleActive_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null) return;

            if (_selectedEmployee.EmployeeId == AppSession.CurrentEmployee.EmployeeId)
            {
                MessageBox.Show("Нельзя деактивировать собственную учётную запись.", "Ограничение системы", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var db = new BloodBankContext())
            {
                var emp = db.Employees.Find(_selectedEmployee.EmployeeId);
                if (emp != null)
                {
                    // Если мы пытаемся его выключить (уволить) - проверяем, не последний ли он админ
                    if (emp.IsActive)
                    {
                        if (!CheckLastAdminConstraint(db, emp, emp.Role, false)) return;
                    }

                    emp.IsActive = !emp.IsActive; // Переворачиваем статус
                    db.SaveChanges();

                    string actionMsg = emp.IsActive ? "восстановлен в должности" : "деактивирован (уволен)";
                    MessageBox.Show($"Сотрудник успешно {actionMsg}.", "Статус изменен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            LoadData();
            ClearForm();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Сотрудники.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.Employees.OrderByDescending(e => e.IsActive).ThenBy(e => e.FullName).ToList();
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Сотрудники");

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "ФИО";
                        worksheet.Cell(1, 3).Value = "Должность";
                        worksheet.Cell(1, 4).Value = "Роль";
                        worksheet.Cell(1, 5).Value = "Логин";
                        worksheet.Cell(1, 6).Value = "Контакт (Email)";
                        worksheet.Cell(1, 7).Value = "Активен";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].EmployeeId;
                            worksheet.Cell(i + 2, 2).Value = data[i].FullName;
                            worksheet.Cell(i + 2, 3).Value = data[i].Position;
                            worksheet.Cell(i + 2, 4).Value = data[i].Role;
                            worksheet.Cell(i + 2, 5).Value = data[i].Login;
                            worksheet.Cell(i + 2, 6).Value = data[i].ContactInfo;
                            worksheet.Cell(i + 2, 7).Value = data[i].IsActive ? "Да" : "Нет";
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