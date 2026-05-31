using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Models;

namespace WpfApp1.Pages
{
    public partial class ReportsPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private ISeries[] _bloodGroupSeries;
        public ISeries[] BloodGroupSeries
        {
            get => _bloodGroupSeries;
            set { _bloodGroupSeries = value; OnPropertyChanged(nameof(BloodGroupSeries)); }
        }

        private ISeries[] _dynamicsSeries;
        public ISeries[] DynamicsSeries
        {
            get => _dynamicsSeries;
            set { _dynamicsSeries = value; OnPropertyChanged(nameof(DynamicsSeries)); }
        }

        private Axis[] _dynamicsXAxes;
        public Axis[] DynamicsXAxes
        {
            get => _dynamicsXAxes;
            set { _dynamicsXAxes = value; OnPropertyChanged(nameof(DynamicsXAxes)); }
        }

        private ISeries[] _writeOffSeries;
        public ISeries[] WriteOffSeries
        {
            get => _writeOffSeries;
            set { _writeOffSeries = value; OnPropertyChanged(nameof(WriteOffSeries)); }
        }

        private ISeries[] _topDonorsSeries;
        public ISeries[] TopDonorsSeries
        {
            get => _topDonorsSeries;
            set { _topDonorsSeries = value; OnPropertyChanged(nameof(TopDonorsSeries)); }
        }

        private Axis[] _topDonorsXAxes;
        public Axis[] TopDonorsXAxes
        {
            get => _topDonorsXAxes;
            set { _topDonorsXAxes = value; OnPropertyChanged(nameof(TopDonorsXAxes)); }
        }

        private Axis[] _topDonorsYAxes;
        public Axis[] TopDonorsYAxes
        {
            get => _topDonorsYAxes;
            set { _topDonorsYAxes = value; OnPropertyChanged(nameof(TopDonorsYAxes)); }
        }

        public ReportsPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSession.CurrentEmployee?.Role != "Заведующий")
            {
                MessageBox.Show("Доступ запрещен. Раздел аналитики доступен только Заведующему.", "Отказ", MessageBoxButton.OK, MessageBoxImage.Error);
                MainTabControl.IsEnabled = false;
                return;
            }

            DateTime today = DateTime.Today;
            DpDynFrom.SelectedDate = today.AddMonths(-1);
            DpDynTo.SelectedDate = today;

            DpWriteOffFrom.SelectedDate = today.AddMonths(-6);
            DpWriteOffTo.SelectedDate = today;

            DpTopFrom.SelectedDate = today.AddMonths(-1);
            DpTopTo.SelectedDate = today;

            LoadTab1Data();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && AppSession.CurrentEmployee?.Role == "Заведующий")
            {
                int index = MainTabControl.SelectedIndex;
                switch (index)
                {
                    case 0: LoadTab1Data(); break;
                    case 1: LoadTab2Data(); break;
                    case 2: LoadTab3Data(); break;
                    case 3: LoadTab4Data(); break;
                }
            }
        }

        private void LoadTab1Data()
        {
            using (var db = new BloodBankContext())
            {
                var components = db.BloodComponents
                    .Include(c => c.Donation).ThenInclude(d => d.Donor)
                    .Where(c => c.Status == "В наличии")
                    .ToList();

                var stock = components
                    .Where(c => c.Donation?.Donor != null)
                    .GroupBy(c => $"{c.Donation.Donor.BloodGroup} {c.Donation.Donor.RhFactor}")
                    .Select(g => new { Group = g.Key, TotalVolume = g.Sum(c => c.VolumeMl) })
                    .ToList();

                if (stock.Count == 0)
                {
                    BloodGroupSeries = new ISeries[]
                    {
                        new PieSeries<int>
                        {
                            Values = new int[] { 1 },
                            Name = "Склад пуст",
                            Fill = new SolidColorPaint(SKColors.LightGray),
                            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                            DataLabelsSize = 14,
                            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                            DataLabelsFormatter = point => "Нет данных"
                        }
                    };
                    return;
                }

                var series = new List<ISeries>();
                foreach (var item in stock)
                {
                    series.Add(new PieSeries<int>
                    {
                        Values = new int[] { item.TotalVolume },
                        Name = item.Group,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 14,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => $"{point.Context.Series.Name}\n{point.Model} мл"
                    });
                }
                BloodGroupSeries = series.ToArray();
            }
        }
        private void BtnRefreshTab1_Click(object sender, RoutedEventArgs e) => LoadTab1Data();

        private void LoadTab2Data()
        {
            if (!DpDynFrom.SelectedDate.HasValue || !DpDynTo.SelectedDate.HasValue) return;
            DateTime from = DpDynFrom.SelectedDate.Value.Date;
            DateTime to = DpDynTo.SelectedDate.Value.Date;

            using (var db = new BloodBankContext())
            {
                var dynamics = db.Donations
                    .Where(d => d.DonationDate >= from && d.DonationDate <= to && d.MedicalStatus != "Брак")
                    .ToList()
                    .GroupBy(d => d.DonationDate.Date)
                    .Select(g => new { Date = g.Key, Volume = g.Sum(d => d.VolumeMl) })
                    .OrderBy(g => g.Date)
                    .ToList();

                if (!dynamics.Any())
                {
                    DynamicsSeries = new ISeries[]
                    {
                        new LineSeries<int> { Values = new int[] { 0 }, Name = "Нет донаций" }
                    };
                    DynamicsXAxes = new Axis[] { new Axis { Labels = new[] { "Нет данных" }, Name = "Дата" } };
                    return;
                }

                var values = dynamics.Select(d => d.Volume).ToArray();
                var labels = dynamics.Select(d => d.Date.ToString("dd.MM")).ToArray();

                DynamicsSeries = new ISeries[]
                {
                    new LineSeries<int>
                    {
                        Values = values,
                        Name = "Объем донаций (мл)",
                        Fill = new SolidColorPaint(SKColors.Red.WithAlpha(50)),
                        Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },
                        GeometrySize = 10
                    }
                };

                DynamicsXAxes = new Axis[] { new Axis { Labels = labels, Name = "Дата" } };
            }
        }
        private void BtnRefreshTab2_Click(object sender, RoutedEventArgs e) => LoadTab2Data();

        private void LoadTab3Data()
        {
            if (!DpWriteOffFrom.SelectedDate.HasValue || !DpWriteOffTo.SelectedDate.HasValue) return;
            DateTime from = DpWriteOffFrom.SelectedDate.Value.Date;
            DateTime to = DpWriteOffTo.SelectedDate.Value.Date;

            using (var db = new BloodBankContext())
            {
                var writeOffs = db.ComponentIssues
                    .Where(ci => ci.IssueType == "Списание" && ci.IssueDate >= from && ci.IssueDate <= to)
                    .ToList()
                    .GroupBy(ci => string.IsNullOrWhiteSpace(ci.WriteOffReason) ? "Иное" : ci.WriteOffReason)
                    .Select(g => new { Reason = g.Key, Count = g.Count() })
                    .ToList();

                if (!writeOffs.Any())
                {
                    WriteOffSeries = new ISeries[]
                    {
                        new PieSeries<int>
                        {
                            Values = new int[] { 1 },
                            Name = "Брака нет",
                            Fill = new SolidColorPaint(SKColors.LightGray),
                            DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                            DataLabelsSize = 14,
                            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                            DataLabelsFormatter = point => "Списаний не было"
                        }
                    };
                    return;
                }

                var series = new List<ISeries>();
                foreach (var item in writeOffs)
                {
                    series.Add(new PieSeries<int>
                    {
                        Values = new int[] { item.Count },
                        Name = item.Reason,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 14,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => $"{point.Context.Series.Name}\n{point.Model} шт."
                    });
                }
                WriteOffSeries = series.ToArray();
            }
        }
        private void BtnRefreshTab3_Click(object sender, RoutedEventArgs e) => LoadTab3Data();

        private void LoadTab4Data()
        {
            if (!DpTopFrom.SelectedDate.HasValue || !DpTopTo.SelectedDate.HasValue) return;
            DateTime from = DpTopFrom.SelectedDate.Value.Date;
            DateTime to = DpTopTo.SelectedDate.Value.Date;

            using (var db = new BloodBankContext())
            {
                var topDonors = db.Donations
                    .Include(d => d.Donor)
                    .Where(d => d.DonationDate >= from && d.DonationDate <= to && d.MedicalStatus != "Брак")
                    .ToList()
                    .Where(d => d.Donor != null)
                    .GroupBy(d => d.Donor.FullName)
                    .Select(g => new { Name = g.Key, TotalVolume = g.Sum(d => d.VolumeMl) })
                    .OrderByDescending(g => g.TotalVolume)
                    .Take(5)
                    .ToList();

                if (!topDonors.Any())
                {
                    TopDonorsSeries = new ISeries[]
                    {
                        new RowSeries<int> { Values = new int[] { 0 }, Name = "Нет донаций" }
                    };
                    TopDonorsXAxes = new Axis[] { new Axis { Name = "Объем (мл)" } };
                    TopDonorsYAxes = new Axis[] { new Axis { Labels = new[] { "Пусто" } } };
                    return;
                }

                var values = topDonors.Select(d => d.TotalVolume).ToArray();
                var labels = topDonors.Select(d => d.Name).ToArray();

                TopDonorsSeries = new ISeries[]
                {
                    new RowSeries<int>
                    {
                        Values = values,
                        Name = "Объем (мл)",
                        DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End
                    }
                };

                TopDonorsYAxes = new Axis[] { new Axis { Labels = labels } };
                TopDonorsXAxes = new Axis[] { new Axis { Name = "Объем сданной крови (мл)" } };
            }
        }
        private void BtnRefreshTab4_Click(object sender, RoutedEventArgs e) => LoadTab4Data();
    }
}