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
    public class IssueRecord
    {
        public DateTime IssueDate { get; set; }
        public string IssueType { get; set; }
        public string LotNumber { get; set; }
        public string ComponentType { get; set; }
        public int VolumeMl { get; set; }
        public string BloodGroup { get; set; }
        public string RhFactor { get; set; }
        public string BloodInfo => $"{BloodGroup} {RhFactor}";
        public string RecipientName { get; set; }
        public string WriteOffReason { get; set; }
        public string EmployeeName { get; set; }
        public string Comments { get; set; }
    }

    public partial class ComponentIssuesPage : Page
    {
        public ComponentIssuesPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyRoleRestrictions();
            LoadRecipients();
            LoadData();
        }

        private void ApplyRoleRestrictions()
        {
            string role = AppSession.CurrentEmployee?.Role;
            if (role != "Лаборант" && role != "Заведующий")
            {
                BtnExport.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadRecipients()
        {
            using (var db = new BloodBankContext())
            {
                var recipients = db.Recipients.OrderBy(r => r.Name).ToList();
                recipients.Insert(0, new Recipient { RecipientId = 0, Name = "Все" });
                CmbRecipient.ItemsSource = recipients;
                CmbRecipient.SelectedIndex = 0;
            }
        }

        private void LoadData()
        {
            using (var db = new BloodBankContext())
            {
                var query = db.ComponentIssues.Select(ci => new IssueRecord
                {
                    IssueDate = ci.IssueDate,
                    IssueType = ci.IssueType,
                    LotNumber = ci.BloodComponent.LotNumber,
                    ComponentType = ci.BloodComponent.ComponentType,
                    VolumeMl = ci.BloodComponent.VolumeMl,
                    BloodGroup = ci.BloodComponent.Donation.Donor.BloodGroup,
                    RhFactor = ci.BloodComponent.Donation.Donor.RhFactor,
                    RecipientName = ci.Recipient != null ? ci.Recipient.Name : "",
                    WriteOffReason = ci.WriteOffReason,
                    EmployeeName = ci.Employee.FullName,
                    Comments = ci.Comments
                });

                if (DpFrom.SelectedDate.HasValue)
                {
                    DateTime from = DpFrom.SelectedDate.Value.Date;
                    query = query.Where(q => q.IssueDate >= from);
                }

                if (DpTo.SelectedDate.HasValue)
                {
                    DateTime to = DpTo.SelectedDate.Value.Date;
                    query = query.Where(q => q.IssueDate <= to);
                }

                if (CmbType.SelectedItem is ComboBoxItem typeItem && typeItem.Content.ToString() != "Все")
                {
                    string selectedType = typeItem.Content.ToString();
                    query = query.Where(q => q.IssueType == selectedType);
                }

                if (CmbRecipient.SelectedItem is Recipient rec && rec.RecipientId != 0)
                {
                    query = query.Where(q => q.RecipientName == rec.Name);
                }

                if (!string.IsNullOrWhiteSpace(TxtLotNumber.Text))
                {
                    string lotSearch = TxtLotNumber.Text.Trim();
                    query = query.Where(q => q.LotNumber.Contains(lotSearch));
                }

                var resultList = query.OrderByDescending(q => q.IssueDate).ToList();
                IssuesGrid.ItemsSource = resultList;
            }
        }

        private void BtnApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            DpFrom.SelectedDate = null;
            DpTo.SelectedDate = null;
            CmbType.SelectedIndex = 0;
            CmbRecipient.SelectedIndex = 0;
            TxtLotNumber.Clear();
            LoadData();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (IssuesGrid.Items.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Журнал_выдачи_списания.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                var data = IssuesGrid.ItemsSource.Cast<IssueRecord>().ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Журнал операций");

                    worksheet.Cell(1, 1).Value = "Дата операции";
                    worksheet.Cell(1, 2).Value = "Тип операции";
                    worksheet.Cell(1, 3).Value = "LOT Номер";
                    worksheet.Cell(1, 4).Value = "Тип компонента";
                    worksheet.Cell(1, 5).Value = "Объем (мл)";
                    worksheet.Cell(1, 6).Value = "Группа крови";
                    worksheet.Cell(1, 7).Value = "Резус-фактор";
                    worksheet.Cell(1, 8).Value = "Получатель (ЛПУ)";
                    worksheet.Cell(1, 9).Value = "Причина списания";
                    worksheet.Cell(1, 10).Value = "Ответственный сотрудник";
                    worksheet.Cell(1, 11).Value = "Комментарии";

                    for (int i = 0; i < data.Count; i++)
                    {
                        worksheet.Cell(i + 2, 1).Value = data[i].IssueDate.ToString("yyyy-MM-dd");
                        worksheet.Cell(i + 2, 2).Value = data[i].IssueType;
                        worksheet.Cell(i + 2, 3).Value = data[i].LotNumber;
                        worksheet.Cell(i + 2, 4).Value = data[i].ComponentType;
                        worksheet.Cell(i + 2, 5).Value = data[i].VolumeMl;
                        worksheet.Cell(i + 2, 6).Value = data[i].BloodGroup;
                        worksheet.Cell(i + 2, 7).Value = data[i].RhFactor;
                        worksheet.Cell(i + 2, 8).Value = data[i].RecipientName;
                        worksheet.Cell(i + 2, 9).Value = data[i].WriteOffReason;
                        worksheet.Cell(i + 2, 10).Value = data[i].EmployeeName;
                        worksheet.Cell(i + 2, 11).Value = data[i].Comments;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(sfd.FileName);
                }

                MessageBox.Show("Данные успешно экспортированы.", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}