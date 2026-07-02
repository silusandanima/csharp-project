using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CraftConnectPOS.Models;
using CraftConnectPOS.Services;

namespace CraftConnectPOS.ViewModels
{
    public class StatisticsViewModel : ViewModelBase, IRefreshable
    {
        private readonly IReportingService _reportingService;
        private string _errorMessage;

        public StatisticsViewModel(IReportingService reportingService)
        {
            _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
            Metrics = new ObservableCollection<MetricCard>();
            Bars = new ObservableCollection<RevenueBar>();
            Breakdown = new ObservableCollection<MetricCard>();
        }

        public ObservableCollection<MetricCard> Metrics { get; }
        public ObservableCollection<RevenueBar> Bars { get; }
        public ObservableCollection<MetricCard> Breakdown { get; }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set { SetProperty(ref _errorMessage, value); }
        }

        public async Task RefreshAsync()
        {
            try
            {
                var data = await _reportingService.GetStatisticsAsync();
                Replace(Metrics, data.Metrics);
                Replace(Bars, data.Bars);
                Replace(Breakdown, data.Breakdown);
                ErrorMessage = string.Empty;
            }
            catch (Exception exception)
            {
                ErrorMessage = "Statistics could not be loaded. " + exception.Message;
            }
        }

        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }
    }
}
