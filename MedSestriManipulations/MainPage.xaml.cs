using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Text;


namespace MedSestriManipulations
{
    public partial class MainPage : ContentPage
    {
        private CancellationTokenSource? _filterCts;
        private List<BloodTest> BloodTestsList = new();

        private readonly PaginationState _paginationState;
        private readonly API _api;
        private readonly CachedDataService _cachedData;

        public MainPage(PaginationState paginationState, API api, CachedDataService cachedData)
        {
            InitializeComponent();
            BindingContext = this;

            _paginationState = paginationState;
            _api = api;
            _cachedData = cachedData;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                BloodTestsList = await _cachedData.GetBloodTestsAsync();
                ApplyFilter();

                var reusedPatient = SelectedPatientService.PatientToReuse;
                if (reusedPatient != null)
                {
                    ClearAllFeald();
                    CurrentName.Text = reusedPatient.FullName;
                    EGNEntry.Text = reusedPatient.EGN;
                    PhoneEntry.Text = reusedPatient.PhoneNumber;

                    if (await DisplayAlert("Потвърждение", $"Искаш ли да се заредят лабораторните изследвания?", "ДА", "НЕ"))
                    {
                        foreach (var test in reusedPatient.BloodTests)
                        {
                            var match = BloodTestsList.FirstOrDefault(x => x.Name == test.Name);
                            if (match != null) match.IsSelected = true;
                            UpdateTotalSum();
                        }
                    }

                    SelectedPatientService.PatientToReuse = null;
                }
                else
                {
                    ClearAllFeald();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"{ex.Message}", "OK");
            }
        }

        private async void ShowsPopupDetailsBloodTest(object sender, EventArgs e)
        {
            if (sender is BindableObject { BindingContext: BloodTest test })
            {
                await DisplayAlert("Пълна информация", test.Name, "Затвори");
            }
        }

        private void AddSumWhenBloodTestChecked(object sender, CheckedChangedEventArgs e)
        {
            UpdateTotalSum();
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            var selected = BloodTestsList.Where(p => p.IsSelected).ToList();
            var totalBng = selected.Sum(p => p.BngPrice);
            var totalEur = selected.Sum(p => p.EuroPrice);

            string name = CurrentName.Text?.Trim()!;
            string egn = EGNEntry.Text?.Trim()!;
            string phone = PhoneEntry.Text?.Trim()!;
            string uin = UIN?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(egn) || string.IsNullOrWhiteSpace(phone))
            {
                await DisplayAlert("Грешка", "Моля, попълни Име, ЕГН и телефонен номер.", "OK");
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
            else if (uin.Length != 10 && uin.Length != 0)
            {
                await DisplayAlert("Грешка", "УИН номерът трябва да съдържа точно 10 цифри.", "OK");
                return;
            }

            decimal discountTotalBng = totalBng * 0.8m;
            decimal discountTotalEur = totalEur * 0.8m;

            var manipulationsList = string.Join("\n", selected.Select((p, index) => $"{index + 1}. {p.Name} - {p.EuroPrice:F2} €"));

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Пациент: {name}");
            messageBuilder.AppendLine($"ЕГН: {egn}");
            messageBuilder.AppendLine($"Телефон: {phone}");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Избрани манипулации {selected.Count} бр:");
            messageBuilder.AppendLine(manipulationsList);

            if (!string.IsNullOrEmpty(uin)) messageBuilder.AppendLine($"УИН: {uin}");

            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Общо сума: {totalEur} € / {totalBng:F2} лв");
            messageBuilder.AppendLine("--------------------");
            messageBuilder.AppendLine($"Сума с отстъпка: {discountTotalEur} € / {discountTotalBng:F2} лв");
            messageBuilder.AppendLine("https://medsestri.com/");
            string message = messageBuilder.ToString().Trim();

            try
            {
                await Share.RequestAsync(new ShareTextRequest
                {
                    Text = message,
                    Title = "Изпрати чрез Viber"
                });

                var createPatient = new Patient()
                {
                    FullName = name,
                    Note = message,
                    EGN = egn,
                    PhoneNumber = phone,
                    Date = DateTime.Now,
                    BloodTests = selected
                };

                var response = await _api.CreateNewPatient(createPatient);
                _cachedData.InvalidatePatients();

                if (!response.IsSuccessStatusCode)
                    await DisplayAlert("Грешка", "Неуспешно записване в историята", "ОК");

                ClearAllFeald();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"Неуспешно изпращане: {ex.Message}", "OK");
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            ClearAllFeald();
        }

        private void OnLoadMore(object sender, EventArgs e)
        {
            // No-op: outer ScrollView owns scrolling; this event won't fire.
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

        private async void OnSearchBarFocused(object sender, FocusEventArgs e)
        {
            SearchCloseButton.IsVisible = true;
            SearchDismissOverlay.IsVisible = true;
            await FormSection.FadeTo(0, 180);
            FormSection.IsVisible = false;
        }

        private async void OnSearchBarUnfocused(object sender, FocusEventArgs e)
        {
            SearchCloseButton.IsVisible = false;
            SearchDismissOverlay.IsVisible = false;
            FormSection.IsVisible = true;
            await FormSection.FadeTo(1, 180);
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

        private void ClearAllFeald()
        {
            CurrentName.Text = "";
            EGNEntry.Text = "";
            PhoneEntry.Text = "";
            UIN.Text = "";

            foreach (var b in BloodTestsList.Where(p => p.IsSelected))
                b.IsSelected = false;

            UpdateTotalSum();
        }

        private void UpdateTotalSum()
        {
            var selected = BloodTestsList.Where(p => p.IsSelected).ToList();
            TotalEURLabel.Text = $"{selected.Sum(p => p.EuroPrice):F2} €";
            TotalBGNLabel.Text = $"{selected.Sum(p => p.BngPrice):F2} лв";
        }

        private void ApplyFilter(string? searchText = null)
        {
            searchText ??= SearchBar.Text?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(searchText)
                ? (IEnumerable<BloodTest>)BloodTestsList
                : BloodTestsList.Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            ProcedureList.ItemsSource = filtered.ToList();
        }
    }
}
