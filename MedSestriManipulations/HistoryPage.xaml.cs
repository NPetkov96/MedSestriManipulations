using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Helpers;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Threading;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace MedSestriManipulations
{
    public partial class HistoryPage : ContentPage
    {
        private readonly API _api;
        private readonly CachedDataService _cachedData;

        private List<Patient> _allPatients = new();
        private Patient? _selectedPatient;
        private readonly ObservableRangeCollection<HistoryListItem> _historyItems = new();
        private List<HistoryListItem> _allHistoryItemsCache = new();
        private CancellationTokenSource? _searchCts;
        private int _loadedPatientsVersion = -1;
        private int _filterVersion;
        private bool _isLoadingPatients;
        private bool _isAppendingPage;
        private bool _isSheetClosing;
        private int _displayCount;
        private const int PageSize = 50;

        public HistoryPage(API api, CachedDataService cachedData)
        {
            InitializeComponent();
            _api = api;
            _cachedData = cachedData;
            PatientsCollection.ItemsSource = _historyItems;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_isLoadingPatients)
                return;

            if (_allPatients.Count > 0 && _loadedPatientsVersion == _cachedData.PatientsVersion)
            {
                PatientsSkeleton.IsLoading = false;
                PatientsCollection.IsVisible = true;
                return;
            }

            _ = LoadPatientsAndApplyFilterAsync();
        }

        private async Task LoadPatientsAndApplyFilterAsync()
        {
            try
            {
                _isLoadingPatients = true;

                if (_allPatients.Count == 0)
                {
                    PatientsSkeleton.IsLoading = true;
                    PatientsCollection.IsVisible = false;
                }

                var sw = Stopwatch.StartNew();
                var patients = await _cachedData.GetPatientsAsync();
                _allPatients = patients.OrderByDescending(p => p.Date).ToList();
                _loadedPatientsVersion = _cachedData.PatientsVersion;
                await ApplyFilter();
                Debug.WriteLine($"History load/filter completed in {sw.ElapsedMilliseconds} ms (patients={_allPatients.Count})");
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Грешка", $"{ex.Message}", "OK");
                });
            }
            finally
            {
                _isLoadingPatients = false;
                PatientsSkeleton.IsLoading = false;
                PatientsCollection.IsVisible = true;
            }
        }


        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            try
            {
                await Task.Delay(300, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            try
            {
                await ApplyFilter(e.NewTextValue, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ApplyFilter(string? searchText = null, CancellationToken cancellationToken = default)
        {
            searchText = (searchText ?? SearchBar.Text)?.Trim() ?? string.Empty;
            var filterVersion = Interlocked.Increment(ref _filterVersion);
            var sw = Stopwatch.StartNew();

            var filterResult = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                IEnumerable<Patient> filtered = _allPatients;

                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = _allPatients.Where(p =>
                        p.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.EGN.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.PhoneNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filtered.ToList();

                var weeks = filteredList
                    .GroupBy(p => GetWeekStart(p.Date))
                    .OrderByDescending(g => g.Key)
                    .Select(g =>
                    {
                        var key = FormatWeekRange(g.Key);
                        var patientsInGroup = g.OrderByDescending(p => p.Date).ToList();
                        return (Key: key, Patients: patientsInGroup);
                    })
                    .ToList();

                var displayItems = new List<HistoryListItem>(filteredList.Count + weeks.Count);
                foreach (var week in weeks)
                {
                    displayItems.Add(new HistoryListItem { HeaderText = week.Key });
                    displayItems.AddRange(week.Patients.Select(patient => new HistoryListItem { Patient = patient }));
                }

                return (Items: displayItems, TotalCount: filteredList.Count, WeekCount: weeks.Count);
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested || filterVersion != Volatile.Read(ref _filterVersion))
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested || filterVersion != Volatile.Read(ref _filterVersion))
                    return;

                _allHistoryItemsCache = filterResult.Items;
                _displayCount = Math.Min(PageSize, _allHistoryItemsCache.Count);
                _historyItems.ReplaceRange(_allHistoryItemsCache.Take(_displayCount));

                sw.Stop();
                Debug.WriteLine($"History filter completed in {sw.ElapsedMilliseconds} ms (search='{searchText}', patients={filterResult.TotalCount}, weeks={filterResult.WeekCount}, displayed={_displayCount})");
            });
        }

        private void PatientsCollection_RemainingItemsThresholdReached(object sender, EventArgs e)
        {
            if (_isAppendingPage || _displayCount >= _allHistoryItemsCache.Count)
                return;

            _isAppendingPage = true;
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), AppendNextPage);
        }

        private void AppendNextPage()
        {
            try
            {
                var nextCount = Math.Min(_displayCount + PageSize, _allHistoryItemsCache.Count);
                if (nextCount <= _displayCount)
                    return;

                _historyItems.AddRange(_allHistoryItemsCache.Skip(_displayCount).Take(nextCount - _displayCount));
                _displayCount = nextCount;
                Debug.WriteLine($"History appended page: displayed={_displayCount}, totalItems={_allHistoryItemsCache.Count}");
            }
            finally
            {
                _isAppendingPage = false;
            }
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var dayOffset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.Date.AddDays(-dayOffset);
        }

        private static string FormatWeekRange(DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(6);
            return $"{weekStart:dd.MM.yyyy} - {weekEnd:dd.MM.yyyy}";
        }


        private async void OnPatientTapped(object sender, TappedEventArgs e)
        {
            if (sender is BindableObject { BindingContext: HistoryListItem { Patient: Patient patient } })
            {
                _selectedPatient = patient;

                SheetNameLabel.Text = patient.FullName;
                SheetEgnLabel.Text = patient.EGN;
                SheetPhoneLabel.Text = patient.PhoneNumber;
                SheetDateOnlyLabel.Text = patient.Date.ToString("dd.MM.yyyy");
                SheetTimeLabel.Text = patient.Date.ToString("HH:mm");
                SheetInfoNameLabel.Text = patient.FullName;
                SheetInfoEgnLabel.Text = patient.EGN;
                SheetInfoPhoneLabel.Text = patient.PhoneNumber;
                SheetTestsCountLabel.Text = $"{patient.TestsCount} бр.";
                SheetTotalLabel.Text = $"{patient.TotalEuro:F2} €";
                BindableLayout.SetItemsSource(SheetTestsLayout, CreateProcedureRows(patient));

                await ShowSheet();
            }
        }

        private static List<PatientProcedureRow> CreateProcedureRows(Patient patient)
        {
            var tests = patient.BloodTests ?? new List<BloodTest>();
            return tests.Select((test, index) => new PatientProcedureRow
            {
                Number = index + 1,
                Name = test.Name,
                EuroPrice = test.EuroPrice,
                HasDivider = index < tests.Count - 1
            }).ToList();
        }

        private async Task ShowSheet()
        {
            _isSheetClosing = false;
            SheetPanel.TranslationY = 600;
            SheetOverlay.Opacity = 0;
            SheetOverlay.IsVisible = true;
            SheetPanel.IsVisible = true;

            await Task.WhenAll(
                SheetOverlay.FadeTo(1, 250),
                SheetPanel.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
            await SheetScrollView.ScrollToAsync(0, 0, false);
        }

        private async Task HideSheet()
        {
            if (_isSheetClosing)
                return;

            _isSheetClosing = true;
            await Task.WhenAll(
                SheetOverlay.FadeTo(0, 220),
                SheetPanel.TranslateTo(0, 600, 260, Easing.CubicIn)
            );
            SheetOverlay.IsVisible = false;
            SheetPanel.IsVisible = false;
            SheetPanel.TranslationY = 0;
            _isSheetClosing = false;
        }

        private async void OnSheetOverlayTapped(object sender, TappedEventArgs e)
        {
            await HideSheet();
        }

        private async void OnSheetCloseClicked(object sender, EventArgs e)
        {
            await HideSheet();
        }

        private async void OnSheetSwipeDown(object sender, SwipedEventArgs e)
        {
            await HideSheet();
        }

        private async void OnSheetPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (_isSheetClosing || !SheetPanel.IsVisible)
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    if (e.TotalY > 0)
                    {
                        SheetPanel.TranslationY = e.TotalY;
                        SheetOverlay.Opacity = Math.Max(0.25, 1 - (e.TotalY / 420));
                    }
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    if (SheetPanel.TranslationY > 110)
                    {
                        await HideSheet();
                    }
                    else
                    {
                        await Task.WhenAll(
                            SheetPanel.TranslateTo(0, 0, 160, Easing.CubicOut),
                            SheetOverlay.FadeTo(1, 160)
                        );
                    }
                    break;
            }
        }


        private async void OnCopyClicked(object sender, EventArgs e)
        {
            if (_selectedPatient == null) return;
            await Clipboard.SetTextAsync(_selectedPatient.Note);
            await Toast();
        }

        private async Task Toast()
        {
            try
            {
                await CommunityToolkit.Maui.Alerts.Toast
                    .Make("Копирано", CommunityToolkit.Maui.Core.ToastDuration.Short)
                    .Show();
            }
            catch {  }
        }

        private async void OnReuseClicked(object sender, EventArgs e)
        {
            if (_selectedPatient == null) return;

            SelectedPatientService.PatientToReuse = _selectedPatient;
            await HideSheet();
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (_selectedPatient == null) return;

            var patient = _selectedPatient;

            bool confirm = await DisplayAlert(
                "Потвърждение",
                $"Сигурни ли сте, че искате да изтриете пациента:\n{patient.FullName} — ЕГН: {patient.EGN}?",
                "ДА", "НЕ");

            if (!confirm) return;

            try
            {
                LoadingOverlay.IsVisible = true;

                var result = await _api.DeletePatient(patient.Date);

                if (result.IsSuccessStatusCode)
                {
                    _allPatients.Remove(patient);
                    _cachedData.InvalidatePatients();
                    await ApplyFilter();
                    await HideSheet();
                }
                else
                {
                    await DisplayAlert("Грешка", "Неуспешно изтриване.", "OK");
                    await ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"{ex.Message}", "OK");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }
    }
}
