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

namespace WpfApp1.Pages
{
    public partial class BloodComponentsPage : Page
    {
        private BloodComponent _selectedComponent;

        public BloodComponentsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadRecipients();
            ApplyRoleRestrictions();
        }

        private void ApplyRoleRestrictions()
        {
            string role = AppSession.CurrentEmployee?.Role;
            if (role != "Лаборант" && role != "Заведующий")
            {
                TxtStorageLocation.IsEnabled = false;
                BtnUpdateLocation.IsEnabled = false;
                RbIssue.IsEnabled = false;
                RbWriteOff.IsEnabled = false;
                CmbRecipient.IsEnabled = false;
                CmbWriteOffReason.IsEnabled = false;
                TxtComments.IsEnabled = false;
                BtnProcess.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                ComponentsGrid.ItemsSource = db.BloodComponents
                    .OrderByDescending(c => c.CollectionDate)
                    .ThenByDescending(c => c.ComponentId)
                    .ToList();
            }
        }

        private void LoadRecipients()
        {
            using (var db = new BloodBankContext())
            {
                CmbRecipient.ItemsSource = db.Recipients.OrderBy(r => r.Name).ToList();
            }
        }

        private void ComponentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComponentsGrid.SelectedItem is BloodComponent component)
            {
                _selectedComponent = component;
                TxtStorageLocation.Text = component.StorageLocation;

                string role = AppSession.CurrentEmployee?.Role;
                bool isAuthorized = (role == "Лаборант" || role == "Заведующий");

                // 1. Место хранения можно менять у того, что физически лежит у нас:
                if (isAuthorized && (component.Status == "В наличии" || component.Status == "Забронировано" || component.Status == "На карантине"))
                {
                    BtnUpdateLocation.IsEnabled = true;
                    TxtStorageLocation.IsEnabled = true;
                }
                else
                {
                    BtnUpdateLocation.IsEnabled = false;
                    TxtStorageLocation.IsEnabled = false;
                }

                // 2. Выдавать в больницы или Списывать можно ТОЛЬКО готовое:
                if (isAuthorized && (component.Status == "В наличии" || component.Status == "Забронировано"))
                {
                    BtnProcess.IsEnabled = true;
                    RbIssue.IsEnabled = true;
                    RbWriteOff.IsEnabled = true;
                    TxtComments.IsEnabled = true;
                }
                else
                {
                    BtnProcess.IsEnabled = false;
                    RbIssue.IsEnabled = false;
                    RbWriteOff.IsEnabled = false;
                    TxtComments.IsEnabled = false;
                }
            }
            else
            {
                _selectedComponent = null;
                TxtStorageLocation.Clear();
                BtnUpdateLocation.IsEnabled = false;
                TxtStorageLocation.IsEnabled = false;
                BtnProcess.IsEnabled = false;
                RbIssue.IsEnabled = false;
                RbWriteOff.IsEnabled = false;
                TxtComments.IsEnabled = false;
            }
        }

        private void RbOperation_Checked(object sender, RoutedEventArgs e)
        {
            if (PnlRecipient == null || PnlWriteOff == null) return;

            if (RbIssue.IsChecked == true)
            {
                PnlRecipient.Visibility = Visibility.Visible;
                PnlWriteOff.Visibility = Visibility.Collapsed;
            }
            else if (RbWriteOff.IsChecked == true)
            {
                PnlRecipient.Visibility = Visibility.Collapsed;
                PnlWriteOff.Visibility = Visibility.Visible;
            }
        }

        private void BtnUpdateLocation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedComponent == null) return;

            string newLocation = TxtStorageLocation.Text.Trim();
            if (newLocation.Length > 100)
            {
                MessageBox.Show("Место хранения не должно превышать 100 символов.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new BloodBankContext())
            {
                var component = db.BloodComponents.Find(_selectedComponent.ComponentId);
                if (component != null)
                {
                    component.StorageLocation = string.IsNullOrWhiteSpace(newLocation) ? null : newLocation;
                    db.SaveChanges();
                }
            }

            LoadData();
            MessageBox.Show("Местоположение успешно обновлено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedComponent == null) return;

            bool isIssue = RbIssue.IsChecked == true;

            if (isIssue)
            {
                if (CmbRecipient.SelectedItem == null)
                {
                    MessageBox.Show("Необходимо выбрать получателя для выдачи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_selectedComponent.ExpirationDate.Date < DateTime.Today)
                {
                    MessageBox.Show("Компонент просрочен. Выдача невозможна, только списание.", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                if (CmbWriteOffReason.SelectedItem == null)
                {
                    MessageBox.Show("Необходимо указать причину списания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            using (var db = new BloodBankContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var issue = new ComponentIssue
                        {
                            ComponentId = _selectedComponent.ComponentId,
                            EmployeeId = AppSession.CurrentEmployee.EmployeeId,
                            IssueDate = DateTime.Today,
                            IssueType = isIssue ? "Выдача" : "Списание",
                            RecipientId = isIssue ? (CmbRecipient.SelectedItem as Recipient).RecipientId : (int?)null,
                            WriteOffReason = !isIssue ? (CmbWriteOffReason.SelectedItem as ComboBoxItem).Content.ToString() : null,
                            Comments = string.IsNullOrWhiteSpace(TxtComments.Text) ? null : TxtComments.Text.Trim()
                        };

                        db.ComponentIssues.Add(issue);

                        var componentToUpdate = db.BloodComponents.Find(_selectedComponent.ComponentId);
                        if (componentToUpdate != null)
                        {
                            componentToUpdate.Status = isIssue ? "Выдано" : "Утилизировано";
                        }

                        db.SaveChanges();
                        transaction.Commit();
                        MessageBox.Show("Операция успешно проведена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Ошибка при сохранении данных.", "Сбой", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            TxtComments.Clear();
            CmbRecipient.SelectedIndex = -1;
            CmbWriteOffReason.SelectedIndex = -1;
            LoadData();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Склад_компонентов.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                using (var db = new BloodBankContext())
                {
                    var data = db.BloodComponents.OrderByDescending(c => c.CollectionDate).ToList();
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Компоненты");

                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Номер партии (LOT)";
                        worksheet.Cell(1, 3).Value = "Тип компонента";
                        worksheet.Cell(1, 4).Value = "Объем (мл)";
                        worksheet.Cell(1, 5).Value = "Дата заготовки";
                        worksheet.Cell(1, 6).Value = "Годен до";
                        worksheet.Cell(1, 7).Value = "Место хранения";
                        worksheet.Cell(1, 8).Value = "Статус";

                        for (int i = 0; i < data.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = data[i].ComponentId;
                            worksheet.Cell(i + 2, 2).Value = data[i].LotNumber;
                            worksheet.Cell(i + 2, 3).Value = data[i].ComponentType;
                            worksheet.Cell(i + 2, 4).Value = data[i].VolumeMl;
                            worksheet.Cell(i + 2, 5).Value = data[i].CollectionDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 6).Value = data[i].ExpirationDate.ToString("yyyy-MM-dd");
                            worksheet.Cell(i + 2, 7).Value = data[i].StorageLocation;
                            worksheet.Cell(i + 2, 8).Value = data[i].Status;
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