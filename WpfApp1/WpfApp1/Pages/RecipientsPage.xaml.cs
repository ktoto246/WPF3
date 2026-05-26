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
    /// <summary>
    /// Логика взаимодействия для RecipientsPage.xaml
    /// </summary>
    public partial class RecipientsPage : Page
    {
        private Recipient _selectedRecipient;

        public RecipientsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyRoleRestrictions();
            LoadData();
        }

        private void ApplyRoleRestrictions()
        {
            if (AppSession.CurrentEmployee?.Role != "Заведующий")
            {
                BtnAdd.Visibility = Visibility.Collapsed;
                BtnUpdate.Visibility = Visibility.Collapsed;
                BtnDelete.Visibility = Visibility.Collapsed;

                BtnAdd.IsEnabled = false;
                BtnUpdate.IsEnabled = false;
                BtnDelete.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                RecipientsGrid.ItemsSource = db.Recipients.ToList();
            }
        }

        private void RecipientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecipientsGrid.SelectedItem is Recipient recipient)
            {
                _selectedRecipient = recipient;
                TxtName.Text = recipient.Name;
                TxtAddress.Text = recipient.Address;
                TxtPhone.Text = recipient.ContactPhone;
                TxtPerson.Text = recipient.ContactPerson;
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _selectedRecipient = null;
            TxtName.Clear();
            TxtAddress.Clear();
            TxtPhone.Clear();
            TxtPerson.Clear();
            RecipientsGrid.SelectedItem = null;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) || TxtName.Text.Length < 3 || TxtName.Text.Length > 200)
            {
                MessageBox.Show("Поле 'Название' обязательно для заполнения и должно содержать от 3 до 200 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TxtAddress.Text) && TxtAddress.Text.Length > 300)
            {
                MessageBox.Show("Длина поля 'Адрес' не должна превышать 300 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TxtPhone.Text))
            {
                if (!Regex.IsMatch(TxtPhone.Text, @"^\+7-\d{3}-\d{3}-\d{4}$"))
                {
                    MessageBox.Show("Формат телефона должен быть +7-XXX-XXX-XXXX.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(TxtPerson.Text) && TxtPerson.Text.Length > 150)
            {
                MessageBox.Show("Длина поля 'Контактное лицо' не должна превышать 150 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            using (var db = new BloodBankContext())
            {
                var newRecipient = new Recipient
                {
                    Name = TxtName.Text.Trim(),
                    Address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim(),
                    ContactPhone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim(),
                    ContactPerson = string.IsNullOrWhiteSpace(TxtPerson.Text) ? null : TxtPerson.Text.Trim()
                };

                db.Recipients.Add(newRecipient);
                db.SaveChanges();
            }
            LoadData();
            BtnClear_Click(null, null);
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipient == null)
            {
                MessageBox.Show("Выберите запись для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!ValidateForm()) return;

            using (var db = new BloodBankContext())
            {
                var recipient = db.Recipients.Find(_selectedRecipient.RecipientId);
                if (recipient != null)
                {
                    recipient.Name = TxtName.Text.Trim();
                    recipient.Address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim();
                    recipient.ContactPhone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim();
                    recipient.ContactPerson = string.IsNullOrWhiteSpace(TxtPerson.Text) ? null : TxtPerson.Text.Trim();
                    db.SaveChanges();
                }
            }
            LoadData();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipient == null)
            {
                MessageBox.Show("Выберите запись для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using (var db = new BloodBankContext())
            {
                bool hasIssues = db.ComponentIssues.Any(i => i.RecipientId == _selectedRecipient.RecipientId);
                if (hasIssues)
                {
                    MessageBox.Show("Нельзя удалить получателя — за ним закреплены записи о выдаче компонентов.", "Отказ в удалении", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show("Вы уверены?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var recipient = db.Recipients.Find(_selectedRecipient.RecipientId);
                    if (recipient != null)
                    {
                        db.Recipients.Remove(recipient);
                        db.SaveChanges();
                    }
                    LoadData();
                    BtnClear_Click(null, null);
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Получатели.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.Recipients.ToList();
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Получатели");

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Название";
                        worksheet.Cell(1, 3).Value = "Адрес";
                        worksheet.Cell(1, 4).Value = "Телефон";
                        worksheet.Cell(1, 5).Value = "Контактное лицо";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].RecipientId;
                            worksheet.Cell(i + 2, 2).Value = data[i].Name;
                            worksheet.Cell(i + 2, 3).Value = data[i].Address;
                            worksheet.Cell(i + 2, 4).Value = data[i].ContactPhone;
                            worksheet.Cell(i + 2, 5).Value = data[i].ContactPerson;
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