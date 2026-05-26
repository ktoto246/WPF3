using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Логика взаимодействия для ReportsPage.xaml
    /// </summary>
    public partial class ReportsPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Свойства для привязки графиков
        private IEnumerable<ISeries> _bloodGroupSeries;
        public IEnumerable<ISeries> BloodGroupSeries
        {
            get => _bloodGroupSeries;
            set { _bloodGroupSeries = value; OnPropertyChanged(nameof(BloodGroupSeries)); }
        }

        private IEnumerable<ISeries> _dynamicsSeries;
        public IEnumerable<ISeries> DynamicsSeries
        {
            get => _dynamicsSeries;
            set { _dynamicsSeries = value; OnPropertyChanged(nameof(DynamicsSeries)); }
        }
        public Axis[] DynamicsXAxes { get; set; }

        private IEnumerable<ISeries> _expirationSeries;
        public IEnumerable<ISeries> ExpirationSeries
        {
            get => _expirationSeries;
            set { _expirationSeries = value; OnPropertyChanged(nameof(ExpirationSeries)); }
        }
        public Axis[] ExpirationXAxes { get; set; }

        private IEnumerable<ISeries> _topDonorsSeries;
        public IEnumerable<ISeries> TopDonorsSeries
        {
            get => _topDonorsSeries;
            set { _topDonorsSeries = value; OnPropertyChanged(nameof(TopDonorsSeries)); }
        }
        public Axis[] TopDonorsXAxes { get; set; }
        public Axis[] TopDonorsYAxes { get; set; }

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
                var stock = db.BloodComponents
                    .Include(c => c.Donation.Donor)
                    .Where(c => c.Status == "В наличии")
                    .GroupBy(c => c.Donation.Donor.BloodGroup + " " + c.Donation.Donor.RhFactor)
                    .Select(g => new { Group = g.Key, TotalVolume = g.Sum(c => c.VolumeMl) })
                    .ToList();

                var series = new List<PieSeries<int>>();
                foreach (var item in stock)
                {
                    series.Add(new PieSeries<int>
                    {
                        Values = new int[] { item.TotalVolume },
                        Name = item.Group,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 15,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => $"{point.Context.Series.Name}\n{point.Model} мл"
                    });
                }
                BloodGroupSeries = series;
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
                    .GroupBy(d => d.DonationDate)
                    .Select(g => new { Date = g.Key, Volume = g.Sum(d => d.VolumeMl) })
                    .OrderBy(g => g.Date)
                    .ToList();

                var values = dynamics.Select(d => d.Volume).ToArray();
                var labels = dynamics.Select(d => d.Date.ToString("dd.MM.yyyy")).ToArray();

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
                OnPropertyChanged(nameof(DynamicsXAxes));
            }
        }
        private void BtnRefreshTab2_Click(object sender, RoutedEventArgs e) => LoadTab2Data();

        private void LoadTab3Data()
        {
            using (var db = new BloodBankContext())
            {
                DateTime today = DateTime.Today;
                DateTime nextWeek = today.AddDays(7);

                int expired = db.BloodComponents.Count(c => c.Status == "В наличии" && c.ExpirationDate < today);
                int expiringSoon = db.BloodComponents.Count(c => c.Status == "В наличии" && c.ExpirationDate >= today && c.ExpirationDate <= nextWeek);
                int good = db.BloodComponents.Count(c => c.Status == "В наличии" && c.ExpirationDate > nextWeek);

                ExpirationSeries = new ISeries[]
                {
                    new ColumnSeries<int>
                    {
                        Values = new int[] { expired, expiringSoon, good },
                        Name = "Количество компонентов",
                        Stroke = null
                    }
                };

                ExpirationXAxes = new Axis[] { new Axis { Labels = new string[] { "Просрочено", "Истекает (<7 дней)", "В норме" } } };
                OnPropertyChanged(nameof(ExpirationXAxes));
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
                    .GroupBy(d => d.Donor.FullName)
                    .Select(g => new { Name = g.Key, TotalVolume = g.Sum(d => d.VolumeMl) })
                    .OrderByDescending(g => g.TotalVolume)
                    .Take(5)
                    .ToList();

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

                OnPropertyChanged(nameof(TopDonorsYAxes));
                OnPropertyChanged(nameof(TopDonorsXAxes));
            }
        }
        private void BtnRefreshTab4_Click(object sender, RoutedEventArgs e) => LoadTab4Data();
    }
}