using CommunityToolkit.Maui.Alerts;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Text;
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
        private const string OddRowBackground = "Transparent";
        private const string EvenRowBackground = "#f0f5f5";

        private readonly API _api;
        private readonly CachedDataService _cachedData;

        public MainPage(API api, CachedDataService cachedData)
        {
            InitializeComponent();
            BindingContext = this;
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
                    await ClearAllFeald(showUndo: false);
                    CurrentName.Text = reusedPatient.FullName;
                    EGNEntry.Text = reusedPatient.EGN;
                    PhoneEntry.Text = reusedPatient.PhoneNumber;

                    if (await DisplayAlert("Потвърждение", "Искаш ли да се заредят лабораторните изследвания?", "ДА", "НЕ"))
                    {
                        foreach (var test in reusedPatient.BloodTests)
                        {
                            var match = BloodTestsList.FirstOrDefault(x => x.Name == test.Name);
                            if (match != null) match.IsSelected = true;
                        }
                        UpdateTotalSum();
                    }

                    SelectedPatientService.PatientToReuse = null;
                }
                else
                {
                    await ClearAllFeald(showUndo: false);
                }
            }
            catch (Exception ex)
            {
                SkeletonView.IsLoading = false;
                await DisplayAlert("Грешка", $"{ex.Message}", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
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
                int startIndex = _displayCount;
                var page = _allFilteredCache.Skip(_displayCount).Take(take).ToList();
                ApplyRowBackgrounds(page, startIndex);
                _displayCount += take;
                _filteredProcedures.AddRange(page);
                Debug.WriteLine($"Appended page: new display count {_displayCount}");
            }
            finally
            {
                _isAppendingPage = false;
            }
        }


        private async Task ShowBottomSheet()
        {
            BottomSheetPanel.TranslationY = 600;
            BottomSheetOverlay.Opacity = 0;
            BottomSheetOverlay.IsVisible = true;
            BottomSheetPanel.IsVisible = true;

            await Task.WhenAll(
                BottomSheetOverlay.FadeTo(1, 250),
                BottomSheetPanel.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }

        private async Task HideBottomSheet()
        {
            CurrentName.Unfocus();
            EGNEntry.Unfocus();
            PhoneEntry.Unfocus();
            _sheetLifted = false;

            await Task.WhenAll(
                BottomSheetOverlay.FadeTo(0, 220),
                BottomSheetPanel.TranslateTo(0, 600, 260, Easing.CubicIn)
            );
            BottomSheetOverlay.IsVisible = false;
            BottomSheetPanel.IsVisible = false;
        }

        private async void OnBottomSheetOverlayTapped(object sender, TappedEventArgs e)
        {
            await HideBottomSheet();
        }

        private const double SheetKeyboardLift = 260;
        private bool _sheetLifted = false;

        private async void OnSheetEntryFocused(object sender, FocusEventArgs e)
        {
            if (_sheetLifted) return;
            _sheetLifted = true;
            await BottomSheetPanel.TranslateTo(0, -SheetKeyboardLift, 220, Easing.CubicOut);
        }

        private async void OnSheetEntryUnfocused(object sender, FocusEventArgs e)
        {
            await Task.Delay(120);
            if (CurrentName.IsFocused || EGNEntry.IsFocused || PhoneEntry.IsFocused)
                return;

            _sheetLifted = false;
            await BottomSheetPanel.TranslateTo(0, 0, 220, Easing.CubicIn);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
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

        private void OnSearchBarFocused(object sender, FocusEventArgs e)
        {
            SearchCloseButton.IsVisible = true;
            SearchDismissOverlay.IsVisible = true;
        }

        private void OnSearchBarUnfocused(object sender, FocusEventArgs e)
        {
            SearchCloseButton.IsVisible = false;
            SearchDismissOverlay.IsVisible = false;
        }

        private void OnSearchCloseClicked(object sender, EventArgs e)
        {
            SearchBar.Text = string.Empty;
            SearchBar.Unfocus();
        }

        private void OnSearchDismissOverlayTapped(object sender, TappedEventArgs e)
        {
            SearchBar.Unfocus();
        }

        private async void ShowsPopupDetailsBloodTest(object sender, EventArgs e)
        {
            if (sender is BindableObject { BindingContext: BloodTest test })
                await DisplayAlert("Пълна информация", test.Name, "Затвори");
        }

        private void OnBloodTestRowTapped(object sender, TappedEventArgs e)
        {
            if (sender is BindableObject { BindingContext: BloodTest test })
                test.IsSelected = !test.IsSelected;

            UpdateTotalSum();
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            var selected = BloodTestsList.Where(p => p.IsSelected).ToList();
            var totalEur = selected.Sum(p => p.EuroPrice);

            string name = CurrentName.Text?.Trim()!;
            string egn = EGNEntry.Text?.Trim()!;
            string phone = PhoneEntry.Text?.Trim()!;

            if (selected.Count == 0)
            {
                await DisplayAlert("Грешка", "Моля, избери поне едно изследване.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(egn) || string.IsNullOrWhiteSpace(phone))
            {
                await DisplayAlert("Грешка", "Моля, попълни Им, ЕГН и телефонен номер.", "OK");
                return;
            }
            else if (egn.Length != 10 || !egn.All(char.IsDigit))
            {
                await DisplayAlert("Грешка", "ЕГН трябва да съдържа точно 10 цифри.", "OK");
                return;
            }
            else if (phone.Length != 10 && phone.Length != 13)
            {
                await DisplayAlert("Грешка", "Телефонният номер трябва да съдържа точно 10 или 13 символа", "OK");
                return;
            }

            decimal discountTotalEur = totalEur * 0.8m;
            var manipulationsList = string.Join("\n", selected.Select((p, i) => $"{i + 1}. {p.Name} - {p.EuroPrice:F2} €"));

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Пациент: {name}");
            messageBuilder.AppendLine($"ЕГН: {egn}");
            messageBuilder.AppendLine($"Телефон: {phone}");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Избрани манипулации {selected.Count} бр:");
            messageBuilder.AppendLine(manipulationsList);
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Общо сума: {totalEur} €");
            messageBuilder.AppendLine("--------------------");
            messageBuilder.AppendLine($"Сума с отстъпка: {discountTotalEur} €");
            messageBuilder.AppendLine("https://medsestri.com/");
            string message = messageBuilder.ToString().Trim();

            try
            {
                await Share.RequestAsync(new ShareTextRequest { Text = message, Title = "Изпрати чрез Viber" });

                var response = await _api.CreateNewPatient(new Patient
                {
                    FullName = name, Note = message, EGN = egn,
                    PhoneNumber = phone, Date = DateTime.Now, BloodTests = selected
                });
                _cachedData.InvalidatePatients();

                if (!response.IsSuccessStatusCode)
                    await DisplayAlert("Грешка", "Неуспешно записване в историята", "ОК");

                await HideBottomSheet();
                await ClearAllFeald(showUndo: false);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"Неуспешно изпращане: {ex.Message}", "OK");
            }
        }

        private async void OnClearClicked(object sender, EventArgs e)
        {
            await ClearAllFeald();
        }

        private async Task ClearAllFeald(bool showUndo = true)
        {
            var savedName = CurrentName.Text ?? string.Empty;
            var savedEGN = EGNEntry.Text ?? string.Empty;
            var savedPhone = PhoneEntry.Text ?? string.Empty;
            var savedSelected = BloodTestsList.Where(p => p.IsSelected).ToList();
            bool hadAnything = savedSelected.Count > 0 || !string.IsNullOrEmpty(savedName);

            CurrentName.Text = "";
            EGNEntry.Text = "";
            PhoneEntry.Text = "";

            foreach (var b in BloodTestsList.Where(p => p.IsSelected))
                b.IsSelected = false;

            UpdateTotalSum();

            if (showUndo && hadAnything)
            {
                var snackbar = Snackbar.Make(
                    "Изчистено",
                    () =>
                    {
                        CurrentName.Text = savedName;
                        EGNEntry.Text = savedEGN;
                        PhoneEntry.Text = savedPhone;
                        foreach (var item in savedSelected) item.IsSelected = true;
                        UpdateTotalSum();
                    },
                    "ВЪРНИ",
                    TimeSpan.FromSeconds(4));

                await snackbar.Show();
            }
        }

        private void UpdateTotalSum()
        {
            var selected = BloodTestsList.Where(p => p.IsSelected).ToList();
            var totalEUR = selected.Sum(p => p.EuroPrice);
            int count = selected.Count;

            TotalEURLabel.Text = $"{totalEUR:F2} €";

            SummaryLabel.Text = count > 0
                ? $"{count} {(count == 1 ? "изследване" : "изследвания")} · {totalEUR:F2} €"
                : "Избери изследвания";
            SummaryLabel.TextColor = count > 0
                ? Color.FromArgb("#0F766E")
                : Color.FromArgb("#6B7280");

            UpdateSelectionPill(count, totalEUR);
        }

        private bool _pillVisible = false;
        private async void UpdateSelectionPill(int count, decimal totalEUR)
        {
            PillCountLabel.Text = count == 1 ? "1 избрано" : $"{count} избрани";
            PillTotalLabel.Text = $"{totalEUR:F2} €";

            if (count > 0 && !_pillVisible)
            {
                _pillVisible = true;
                SelectionPill.IsVisible = true;
                SelectionPill.Opacity = 0;
                SelectionPill.TranslationY = 60;
                await Task.WhenAll(
                    SelectionPill.FadeTo(1, 220),
                    SelectionPill.TranslateTo(0, 0, 260, Easing.CubicOut)
                );
            }
            else if (count == 0 && _pillVisible)
            {
                _pillVisible = false;
                await Task.WhenAll(
                    SelectionPill.FadeTo(0, 180),
                    SelectionPill.TranslateTo(0, 60, 200, Easing.CubicIn)
                );
                SelectionPill.IsVisible = false;
            }
        }

        private async void OnSelectionPillTapped(object sender, TappedEventArgs e)
        {
            await ShowBottomSheet();
        }

        private async void ApplyFilter(string? searchText = null)
        {
            searchText ??= SearchBar.Text?.Trim() ?? string.Empty;

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
                    ApplyRowBackgrounds(firstPage);

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

        private static void ApplyRowBackgrounds(IReadOnlyList<BloodTest> items, int startIndex = 0)
        {
            for (int i = 0; i < items.Count; i++)
            {
                int rowIndex = startIndex + i;
                items[i].RowBackgroundColor = rowIndex % 2 == 0 ? OddRowBackground : EvenRowBackground;
            }
        }

    }
}
