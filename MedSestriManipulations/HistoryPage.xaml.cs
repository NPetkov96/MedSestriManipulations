using CommunityToolkit.Maui.Alerts;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Diagnostics;

namespace MedSestriManipulations
{
    public partial class HistoryPage : ContentPage
    {
        private readonly API _api;
        private readonly CachedDataService _cachedData;

        private List<Patient> _allPatients = new();
        private Patient? _selectedPatient;
        private CancellationTokenSource? _searchCts;
        private int _loadedPatientsVersion = -1;
        private int _filterVersion;
        private bool _isLoadingPatients;
        private bool _isSheetClosing;

        public HistoryPage(API api, CachedDataService cachedData)
        {
            InitializeComponent();
            _api = api;
            _cachedData = cachedData;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Switching tabs away and back should never leave a stale detail sheet open.
            SheetOverlay.IsVisible = false;
            SheetPanel.IsVisible = false;
            SheetPanel.TranslationY = 0;

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

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            SearchClearButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);

            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            var text = e.NewTextValue;

            Task.Delay(300, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                    MainThread.BeginInvokeOnMainThread(() => _ = ApplyFilter(text, token));
            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        private void OnSearchClearClicked(object sender, EventArgs e) => ClearSearch();

        private void OnClearSearchClicked(object sender, EventArgs e) => ClearSearch();

        private void ClearSearch()
        {
            SearchEntry.Text = string.Empty;
            SearchEntry.Unfocus();
        }

        private async Task ApplyFilter(string? searchText = null, CancellationToken cancellationToken = default)
        {
            searchText = (searchText ?? SearchEntry.Text)?.Trim() ?? string.Empty;
            var filterVersion = Interlocked.Increment(ref _filterVersion);
            var sw = Stopwatch.StartNew();

            var groups = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                IEnumerable<Patient> filtered = _allPatients;

                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = _allPatients.Where(p =>
                        p.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.EGN.Contains(searchText, StringComparison.OrdinalIgnoreCase));
                }

                return filtered
                    .GroupBy(p => GetWeekStart(p.Date))
                    .OrderByDescending(g => g.Key)
                    .Select(g => new VisitWeekGroup(
                        FormatWeekRange(g.Key),
                        g.OrderByDescending(p => p.Date)))
                    .ToList();
            }, cancellationToken);

            if (cancellationToken.IsCancellationRequested || filterVersion != Volatile.Read(ref _filterVersion))
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested || filterVersion != Volatile.Read(ref _filterVersion))
                    return;

                PatientsCollection.ItemsSource = groups;

                sw.Stop();
                Debug.WriteLine($"History filter completed in {sw.ElapsedMilliseconds} ms (search='{searchText}', groups={groups.Count})");
            });
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
            if (sender is not BindableObject { BindingContext: Patient patient })
                return;

            _selectedPatient = patient;

            var resources = Application.Current!.Resources;
            var accent700 = (Color)resources["WarmAccent700"];

            SheetNameLabel.Text = patient.FullName;
            SheetEgnLabel.Text = $"№ {patient.EGN}";
            SheetPhoneLabel.Text = patient.PhoneNumber;
            SheetDateOnlyLabel.Text = patient.Date.ToString("dd.MM.yyyy");
            SheetTimeLabel.Text = patient.Date.ToString("HH:mm");
            SheetInfoNameLabel.Text = patient.FullName;
            SheetInfoEgnLabel.Text = patient.EGN;
            SheetInfoPhoneLabel.Text = patient.PhoneNumber;

            SheetTestsCountLabel.FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = patient.TestsCount.ToString(), FontFamily = "OpenSansRegular", FontSize = 12, TextColor = accent700 },
                    new Span { Text = " бр.", FontFamily = "LoraRegular", FontSize = 12, TextColor = accent700 }
                }
            };

            SheetTotalLabel.Text = $"{patient.TotalEuro:F2} €";

            PopulateTestsList(patient);

            await ShowSheet();
        }

        private void PopulateTestsList(Patient patient)
        {
            var resources = Application.Current!.Resources;
            var textColor = (Color)resources["WarmText"];
            var mutedColor = (Color)resources["WarmTextMuted"];
            var dividerColor = (Color)resources["WarmDivider"];
            var accentColor = (Color)resources["WarmAccent700"];

            var tests = patient.BloodTests ?? new List<BloodTest>();

            SheetTestsLayout.Children.Clear();
            for (int i = 0; i < tests.Count; i++)
            {
                var test = tests[i];

                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(new GridLength(28)),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    Padding = new Thickness(0, 9)
                };

                row.Add(new Label
                {
                    Text = $"{i + 1}.",
                    FontFamily = "OpenSansRegular",
                    FontSize = 13,
                    TextColor = mutedColor,
                    HorizontalTextAlignment = TextAlignment.Center
                }, 0, 0);

                row.Add(new Label
                {
                    Text = test.Name,
                    FontFamily = "LoraRegular",
                    FontSize = 14,
                    TextColor = textColor,
                    LineBreakMode = LineBreakMode.WordWrap
                }, 1, 0);

                row.Add(new Label
                {
                    Text = test.IsFree ? "Безплатно" : $"{test.EuroPrice:F2} €",
                    FontFamily = test.IsFree ? "LoraRegular" : "OpenSansRegular",
                    FontSize = 14,
                    TextColor = accentColor,
                    HorizontalOptions = LayoutOptions.End
                }, 2, 0);

                var container = new VerticalStackLayout { Spacing = 0 };
                container.Children.Add(row);

                if (i < tests.Count - 1)
                    container.Children.Add(new BoxView { HeightRequest = 1, Color = dividerColor });

                SheetTestsLayout.Children.Add(container);
            }
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

        private async void OnSheetCloseClicked(object sender, TappedEventArgs e)
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
            await ShowCopiedToast();
        }

        private async Task ShowCopiedToast()
        {
            try
            {
                await Toast.Make("Копирано", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
            }
            catch { }
        }

        private async void OnReuseClicked(object sender, EventArgs e)
        {
            if (_selectedPatient == null) return;

            SelectedPatientService.PatientToReuse = _selectedPatient;
            SelectedPatientService.ContactInfoToReuse = _selectedPatient;
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
