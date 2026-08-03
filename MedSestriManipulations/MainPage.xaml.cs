using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Diagnostics;

namespace MedSestriManipulations
{
    public partial class MainPage : ContentPage
    {
        private CancellationTokenSource? _filterCts;
        private int _filterVersion;
        private List<BloodTest> BloodTestsList = new();
        private readonly Helpers.ObservableRangeCollection<BloodTest> _filteredProcedures
            = new Helpers.ObservableRangeCollection<BloodTest>();

        private List<BloodTest> _allFilteredCache = new();
        private int _displayCount = 0;
        private bool _isAppendingPage;
        private const int PageSize = 40;

        private readonly API _api;
        private readonly CachedDataService _cachedData;

        public MainPage(API api, CachedDataService cachedData)
        {
            InitializeComponent();
            _api = api;
            _cachedData = cachedData;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                SkeletonView.IsLoading = true;
                ProcedureList.IsVisible = false;

                BloodTestsList = await _cachedData.GetBloodTestsAsync();

                SkeletonView.IsLoading = false;
                ProcedureList.IsVisible = true;

                ProcedureList.ItemsSource = _filteredProcedures;
                ApplyFilter();

                var reusedPatient = SelectedPatientService.PatientToReuse;
                if (reusedPatient != null)
                {
                    foreach (var test in BloodTestsList.Where(t => t.IsSelected))
                        test.IsSelected = false;

                    if (await DisplayAlert("Потвърждение", "Искаш ли да се заредят лабораторните изследвания?", "ДА", "НЕ"))
                    {
                        foreach (var test in reusedPatient.BloodTests)
                        {
                            var match = BloodTestsList.FirstOrDefault(x => x.Name == test.Name);
                            if (match != null) match.IsSelected = true;
                        }
                    }

                    SelectedPatientService.PatientToReuse = null;
                }

                UpdateSelectionHeader();
            }
            catch (Exception ex)
            {
                SkeletonView.IsLoading = false;
                await DisplayAlert("Грешка", $"{ex.Message}", "OK");
            }
        }

        private void ProcedureList_RemainingItemsThresholdReached(object sender, EventArgs e)
        {
            if (_isAppendingPage || _displayCount >= _allFilteredCache.Count) return;

            _isAppendingPage = true;
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), AppendNextPage);
        }

        private void AppendNextPage()
        {
            try
            {
                if (_displayCount >= _allFilteredCache.Count) return;

                int remaining = _allFilteredCache.Count - _displayCount;
                int take = Math.Min(PageSize, remaining);
                var page = _allFilteredCache.Skip(_displayCount).Take(take).ToList();
                _displayCount += take;
                _filteredProcedures.AddRange(page);
                Debug.WriteLine($"Appended page: new display count {_displayCount}");
            }
            finally
            {
                _isAppendingPage = false;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            SearchClearButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);

            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;
            var text = e.NewTextValue;

            Task.Delay(250, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                    MainThread.BeginInvokeOnMainThread(() => ApplyFilter(text));
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        private void OnSearchClearClicked(object sender, EventArgs e) => ClearSearch();

        private void OnClearSearchClicked(object sender, EventArgs e) => ClearSearch();

        private void ClearSearch()
        {
            SearchEntry.Text = string.Empty;
            SearchEntry.Unfocus();
        }

        private void OnBloodTestRowTapped(object sender, TappedEventArgs e)
        {
            if (sender is BindableObject { BindingContext: BloodTest test })
                test.IsSelected = !test.IsSelected;

            UpdateSelectionHeader();
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            foreach (var b in BloodTestsList.Where(p => p.IsSelected))
                b.IsSelected = false;

            UpdateSelectionHeader();
        }

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            if (!BloodTestsList.Any(p => p.IsSelected))
                return;

            await Shell.Current.GoToAsync(nameof(PatientDetailsPage));
        }

        private void UpdateSelectionHeader()
        {
            var selected = BloodTestsList.Where(p => p.IsSelected).ToList();
            var totalEur = selected.Sum(p => p.EuroPrice);
            int count = selected.Count;

            SelectionHeader.IsVisible = count > 0;
            SelectionSummaryLabel.Text = count == 1
                ? $"1 изследване · {totalEur:F2} €"
                : $"{count} изследвания · {totalEur:F2} €";
        }

        private async void ApplyFilter(string? searchText = null)
        {
            searchText ??= SearchEntry.Text?.Trim() ?? string.Empty;

            var token = _filterCts?.Token ?? CancellationToken.None;
            var filterVersion = Interlocked.Increment(ref _filterVersion);

            try
            {
                var sw = Stopwatch.StartNew();
                var pairedList = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var filtered = string.IsNullOrEmpty(searchText)
                        ? (IEnumerable<BloodTest>)BloodTestsList
                        : BloodTestsList.Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

                    var list = filtered
                        .OrderByDescending(p => p.Name.Contains("НЗОК", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return list;
                }, token).ConfigureAwait(false);
                if (token.IsCancellationRequested || filterVersion != Volatile.Read(ref _filterVersion))
                    return;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (token.IsCancellationRequested || filterVersion != Volatile.Read(ref _filterVersion))
                        return;

                    _allFilteredCache = pairedList;
                    _displayCount = Math.Min(PageSize, _allFilteredCache.Count);
                    var firstPage = _allFilteredCache.Take(_displayCount).ToList();

                    _filteredProcedures.ReplaceRange(firstPage);

                    sw.Stop();
                    Debug.WriteLine($"ApplyFilter completed in {sw.ElapsedMilliseconds} ms (search='{searchText}', totalItems={_allFilteredCache.Count}, displayed={_displayCount})");
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFilter error: {ex}");
            }
        }
    }
}
