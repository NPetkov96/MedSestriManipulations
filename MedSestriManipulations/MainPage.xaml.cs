using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Collections.ObjectModel;
using System.Text;


namespace MedSestriManipulations
{
    public partial class MainPage : ContentPage
    {
        private bool _isMenuOpen = false;
        private CancellationTokenSource _filterCts;

        public ObservableCollection<BloodTest> BloodTestsList = new();
        public ObservableCollection<BloodTest> BloodTests = new();

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
                var bloodTestsListResponse = await _cachedData.GetBloodTestsAsync();
                BloodTestsList = new ObservableCollection<BloodTest>(bloodTestsListResponse);
                ProcedureList.ItemsSource = BloodTests;

                await FilterBloodTests();

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
                            BloodTestsList.FirstOrDefault(x => x.Name == test.Name)!.IsSelected = true;
                            //await LoadMorePaginationProceduresAsync();
                            UpdateTotalSum();
                        }
                    }

                    SelectedPatientService.PatientToReuse = null;
                    _isMenuOpen = true;
                    OnMainFabClicked(null, null);
                }
                else
                {
                    ClearAllFeald();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"{ex.Message}", "OK");
#if ANDROID
                Java.Lang.JavaSystem.Exit(0);
#endif

            }

        }
        private async void ShowsPopupDetailsBloodTest(object sender, EventArgs e)
        {
            if (sender is StackLayout layout && layout.Children.FirstOrDefault() is Label label && label.Text is string text)
            {
                await DisplayAlert("Пълна информация", text, "Затвори");
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
                _cachedData._isPatientsLoaded = false;

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Грешка", "Неуспешно записване в историята", "ОК");
                }

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

        private async void OnLoadMore(object sender, EventArgs e)
        {
            await LoadMorePaginationProceduresAsync();
        }
        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _ = FilterBloodTests(); //Dont change
        }

        private void ClearAllFeald()
        {
            CurrentName.Text = "";
            EGNEntry.Text = "";
            PhoneEntry.Text = "";
            UIN.Text = "";

            foreach (var proc in BloodTestsList.Where(p => p.IsSelected == true))
                proc.IsSelected = false;

            UpdateTotalSum();
        }
        private void UpdateTotalSum()
        {
            var totalBNG = BloodTestsList.Where(p => p.IsSelected).Sum(p => p.BngPrice);
            TotalBGNLabel.Text = $"{totalBNG:F2} лв";

            var totalEURO = BloodTestsList.Where(p => p.IsSelected).Sum(p => p.EuroPrice);
            TotalEURLabel.Text = $"{totalEURO:F2} €";
        }

        private async Task FilterBloodTests()
        {
            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;

            await Task.Delay(300, token);
            BloodTests.Clear();
            _paginationState.Reset();
            await LoadMorePaginationProceduresAsync();
        }
        private async Task LoadMorePaginationProceduresAsync()
        {
            if (_paginationState.IsLoading) return;
            _paginationState.IsLoading = true;

            var matching = await Task.Run(() => BloodTestsList
                    .Where(p => p.Name.ToLower().Contains(SearchBar.Text?.ToLower() ?? ""))
                    .Skip(_paginationState.CurrentIndex)
                    .Take(_paginationState.VisibleThreshold)
                    .ToList());


            var toAdd = matching.Except(BloodTests).ToList();
            foreach (var item in toAdd)
                BloodTests.Add(item);

            _paginationState.CurrentIndex += matching.Count;
            _paginationState.IsLoading = false;
        }


        private void OnMainFabClicked(object sender, EventArgs e)
        {
            _isMenuOpen = !_isMenuOpen;

            CatheterButton.IsVisible = _isMenuOpen;
            HistoryButton.IsVisible = _isMenuOpen;
        }

    }
}
