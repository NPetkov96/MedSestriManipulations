using MedSestriManipulations.Controls;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;

namespace MedSestriManipulations
{
    public partial class StatisticsPage : ContentPage
    {
        private readonly CachedDataService _cachedData;
        private MedSestriStatistics? _statistics;
        private DailyStatisticsChartDrawable? _dailyDrawable;
        private bool _isLoading;
        private bool _hasLoaded;
        private bool _showAllMonths;
        private int _loadedStatisticsVersion = -1;
        private int _comparisonDays = 7;
        private int _dailyChartDays = 30;

        public StatisticsPage(CachedDataService cachedData)
        {
            InitializeComponent();
            _cachedData = cachedData;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if ((!_hasLoaded || _loadedStatisticsVersion != _cachedData.StatisticsVersion) && !_isLoading)
                _ = LoadStatisticsAsync();
        }

        private async Task LoadStatisticsAsync(bool forceRefresh = false)
        {
            if (_isLoading)
                return;

            try
            {
                _isLoading = true;
                ErrorView.IsVisible = false;

                if (!_hasLoaded)
                {
                    LoadingView.IsVisible = true;
                    StatisticsContent.IsVisible = false;
                }

                var statistics = await _cachedData.GetStatisticsAsync(forceRefresh);
                RenderStatistics(statistics);
                _hasLoaded = true;
                _loadedStatisticsVersion = _cachedData.StatisticsVersion;
                StatisticsContent.IsVisible = true;
            }
            catch (Exception ex)
            {
                if (!_hasLoaded)
                {
                    StatisticsContent.IsVisible = false;
                    ErrorLabel.Text = ex.Message;
                    ErrorView.IsVisible = true;
                }
                else
                {
                    await DisplayAlert("Грешка", ex.Message, "OK");
                }
            }
            finally
            {
                _isLoading = false;
                LoadingView.IsVisible = false;
                StatisticsRefreshView.IsRefreshing = false;
            }
        }

        private void RenderStatistics(MedSestriStatistics statistics)
        {
            _statistics = statistics;
            BindingContext = statistics;

            PeriodLabel.Text = statistics.PeriodText;
            TotalDaysLabel.Text = statistics.TotalDays.ToString();
            ActiveDaysLabel.Text = statistics.ActiveDays.ToString();
            AverageLabel.Text = statistics.AveragePerActiveDay.ToString("0.00");
            MaximumLabel.Text = statistics.MaximumPerDay.ToString();

            var activeRatio = statistics.TotalDays > 0
                ? statistics.ActiveDays / (double)statistics.TotalDays
                : 0;
            ActiveDaysProgress.Progress = Math.Clamp(activeRatio, 0, 1);
            ActiveDaysSummaryLabel.Text = $"{activeRatio:P0} от всички дни имат поне един запис";

            UpdateComparison(_comparisonDays);
            UpdateDailyChart(_dailyChartDays);

            var lastTwelveMonths = statistics.Monthly.TakeLast(12).ToList();
            MonthlyChart.Drawable = new MonthlyStatisticsChartDrawable(lastTwelveMonths);
            WeekdayChart.Drawable = new WeekdayStatisticsChartDrawable(statistics.ByWeekday);
            MonthlyChart.Invalidate();
            WeekdayChart.Invalidate();

            RefreshMonthlyCards();
        }

        private void UpdateComparison(int numberOfDays)
        {
            if (_statistics == null)
                return;

            _comparisonDays = numberOfDays;
            var period = numberOfDays == 7 ? _statistics.Last7Days : _statistics.Last30Days;

            ComparisonCountLabel.Text = period.RecordCount.ToString();
            ComparisonPeriodLabel.Text = $"{GetRecordWord(period.RecordCount)} за последните {numberOfDays} дни";
            ComparisonPreviousLabel.Text =
                $"Предходните {numberOfDays} дни: {period.PreviousRecordCount} {GetRecordWord(period.PreviousRecordCount)}";

            if (!period.ChangePercent.HasValue)
            {
                ComparisonChangeLabel.Text = "Няма предходни записи за сравнение";
                ComparisonChangeLabel.TextColor = GetResourceColor("WarmTextMuted");
            }
            else if (period.ChangePercent.Value > 0)
            {
                ComparisonChangeLabel.Text = $"↑ {period.ChangePercent.Value:0.0}% спрямо предходния период";
                ComparisonChangeLabel.TextColor = GetResourceColor("WarmAccent700");
            }
            else if (period.ChangePercent.Value < 0)
            {
                ComparisonChangeLabel.Text = $"↓ {Math.Abs(period.ChangePercent.Value):0.0}% спрямо предходния период";
                ComparisonChangeLabel.TextColor = GetResourceColor("WarmError");
            }
            else
            {
                ComparisonChangeLabel.Text = "Без промяна спрямо предходния период";
                ComparisonChangeLabel.TextColor = GetResourceColor("WarmTextMuted");
            }

            SetSegmentButtonState(Comparison7Button, numberOfDays == 7);
            SetSegmentButtonState(Comparison30Button, numberOfDays == 30);
        }

        private void UpdateDailyChart(int numberOfDays)
        {
            if (_statistics == null)
                return;

            _dailyChartDays = numberOfDays;
            var dailyData = numberOfDays == 0
                ? _statistics.Daily.ToList()
                : _statistics.Daily.TakeLast(numberOfDays).ToList();

            _dailyDrawable = new DailyStatisticsChartDrawable(dailyData);
            DailyChart.Drawable = _dailyDrawable;
            DailyPointDetails.IsVisible = false;

            if (dailyData.Count > 0)
                DailyRangeLabel.Text = $"{dailyData[0].Date:dd.MM.yyyy} – {dailyData[^1].Date:dd.MM.yyyy}";
            else
                DailyRangeLabel.Text = "Няма налични данни";

            SetSegmentButtonState(Daily30Button, numberOfDays == 30);
            SetSegmentButtonState(Daily90Button, numberOfDays == 90);
            SetSegmentButtonState(DailyAllButton, numberOfDays == 0);
            DailyChart.Invalidate();
        }

        private void RefreshMonthlyCards()
        {
            if (_statistics == null)
                return;

            var monthlyItems = (_showAllMonths
                    ? _statistics.Monthly
                    : _statistics.Monthly.TakeLast(6))
                .OrderByDescending(month => month.Month)
                .ToList();

            BindableLayout.SetItemsSource(MonthlyCards, monthlyItems);
            MonthsToggleButton.IsVisible = _statistics.Monthly.Count > 6;
            MonthsToggleButton.Text = _showAllMonths ? "Покажи по-малко" : "Покажи всички";
        }

        private void OnDailyChartInteraction(object sender, TouchEventArgs e)
        {
            if (_dailyDrawable == null || e.Touches.Length == 0 || DailyChart.Width <= 0)
                return;

            var selected = _dailyDrawable.SelectNearest(e.Touches[0].X, (float)DailyChart.Width);
            if (selected == null)
                return;

            SelectedDateLabel.Text = selected.Date.ToString("dd.MM.yyyy");
            SelectedRecordsLabel.Text = $"{selected.RecordCount} зап.";
            SelectedAverageLabel.Text = $"ср. {selected.SevenDayAverage:0.00}";
            DailyPointDetails.IsVisible = true;
            DailyChart.Invalidate();
        }

        private static void SetSegmentButtonState(Button button, bool isSelected)
        {
            button.BackgroundColor = isSelected ? GetResourceColor("WarmAccent100") : Colors.Transparent;
            button.BorderColor = isSelected ? GetResourceColor("WarmAccent") : GetResourceColor("WarmDivider");
            button.TextColor = isSelected ? GetResourceColor("WarmAccent700") : GetResourceColor("WarmTextMuted");
        }

        private static Color GetResourceColor(string key) =>
            (Color)Application.Current!.Resources[key];

        private static string GetRecordWord(int count) => count == 1 ? "запис" : "записа";

        private void OnComparison7Clicked(object sender, EventArgs e) => UpdateComparison(7);

        private void OnComparison30Clicked(object sender, EventArgs e) => UpdateComparison(30);

        private void OnDaily30Clicked(object sender, EventArgs e) => UpdateDailyChart(30);

        private void OnDaily90Clicked(object sender, EventArgs e) => UpdateDailyChart(90);

        private void OnDailyAllClicked(object sender, EventArgs e) => UpdateDailyChart(0);

        private void OnMonthsToggleClicked(object sender, EventArgs e)
        {
            _showAllMonths = !_showAllMonths;
            RefreshMonthlyCards();
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadStatisticsAsync(forceRefresh: true);
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            StatisticsRefreshView.IsRefreshing = true;
            await LoadStatisticsAsync(forceRefresh: true);
        }

        private async void OnRetryClicked(object sender, EventArgs e)
        {
            await LoadStatisticsAsync(forceRefresh: true);
        }
    }
}
