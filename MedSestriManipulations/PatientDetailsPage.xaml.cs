using CommunityToolkit.Maui.Alerts;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace MedSestriManipulations
{
    public partial class PatientDetailsPage : ContentPage
    {
        // Rough allowance for a software keyboard (incl. suggestion bar) so the ScrollView
        // has enough runway to bring the last field fully above it - see OnEntryFocused.
        private const double KeyboardSpacerHeight = 280;
        private const int FocusScrollDelayMs = 250;
        private const int UnfocusCollapseDelayMs = 150;

        private readonly API _api;
        private readonly CachedDataService _cachedData;
        private List<BloodTest> _selectedTests = new();
        private bool _isNavigating;
        private bool _isUpdatingEgnText;
        private bool _summaryExpanded;

        public PatientDetailsPage(API api, CachedDataService cachedData)
        {
            InitializeComponent();
            _api = api;
            _cachedData = cachedData;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var allTests = await _cachedData.GetBloodTestsAsync();
            _selectedTests = allTests.Where(t => t.IsSelected).ToList();

            if (_selectedTests.Count == 0)
            {
                // Nothing selected (e.g. deep-linked back after a submit) - nothing to fill in, go back to the list.
                await Shell.Current.GoToAsync("..");
                return;
            }

            var reusedContact = SelectedPatientService.ContactInfoToReuse;
            if (reusedContact != null)
            {
                NameEntry.Text = reusedContact.FullName;
                EgnEntry.Text = reusedContact.EGN;
                PhoneEntry.Text = reusedContact.PhoneNumber;
                SelectedPatientService.ContactInfoToReuse = null;
            }

            PopulateSummary();
        }

        private void PopulateSummary()
        {
            var total = _selectedTests.Sum(t => t.EuroPrice);

            SummaryTitleLabel.Text = _selectedTests.Count == 1
                ? "1 изследване"
                : $"{_selectedTests.Count} изследвания";

            var visibleCount = _summaryExpanded ? _selectedTests.Count : Math.Min(3, _selectedTests.Count);

            SummaryTestsLayout.Children.Clear();
            foreach (var test in _selectedTests.Take(visibleCount))
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };

                var resources = Application.Current!.Resources;

                var nameLabel = new Label
                {
                    Text = test.Name,
                    LineBreakMode = LineBreakMode.WordWrap,
                    FontFamily = "LoraRegular",
                    FontSize = 15,
                    TextColor = (Color)resources["WarmText"]
                };

                var priceLabel = new Label
                {
                    Text = test.IsFree ? "Безплатно" : $"{test.EuroPrice:F2} €",
                    FontFamily = test.IsFree ? "LoraRegular" : "OpenSansRegular",
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.End,
                    TextColor = (Color)resources["WarmTextMuted"]
                };

                row.Add(nameLabel, 0, 0);
                row.Add(priceLabel, 1, 0);
                SummaryTestsLayout.Children.Add(row);
            }

            if (_selectedTests.Count > 3)
            {
                SummaryMoreLabel.Text = _summaryExpanded
                    ? "Покажи по-малко"
                    : $"и още {_selectedTests.Count - 3} изследвания";
                SummaryMoreLabel.IsVisible = true;
            }
            else
            {
                SummaryMoreLabel.IsVisible = false;
            }

            SummaryTotalLabel.Text = $"{total:F2} €";
        }

        private void OnSummaryMoreTapped(object sender, TappedEventArgs e)
        {
            _summaryExpanded = !_summaryExpanded;
            PopulateSummary();
        }

        private async void OnBackTapped(object sender, TappedEventArgs e) => await GoBackToListAsync();

        private async void OnEditSelectionTapped(object sender, TappedEventArgs e) => await GoBackToListAsync();

        private async Task GoBackToListAsync()
        {
            if (_isNavigating) return;
            try
            {
                _isNavigating = true;
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void OnNameTextChanged(object sender, TextChangedEventArgs e)
        {
            NameErrorRow.IsVisible = false;
            NameFieldBorder.Stroke = (Color)Application.Current!.Resources["WarmDivider"];
        }

        private void OnEgnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingEgnText) return;

            var digitsOnly = new string((e.NewTextValue ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digitsOnly != e.NewTextValue)
            {
                _isUpdatingEgnText = true;
                EgnEntry.Text = digitsOnly;
                _isUpdatingEgnText = false;
            }

            EgnErrorRow.IsVisible = false;
            //EgnHelperLabel.IsVisible = true;
            EgnFieldBorder.Stroke = (Color)Application.Current!.Resources["WarmDivider"];
        }

        private void OnPhoneTextChanged(object sender, TextChangedEventArgs e)
        {
            PhoneErrorRow.IsVisible = false;
            PhoneFieldBorder.Stroke = (Color)Application.Current!.Resources["WarmDivider"];
        }

        private void OnNameEntryCompleted(object sender, EventArgs e) => PhoneEntry.Focus();

        private void OnPhoneEntryCompleted(object sender, EventArgs e) => EgnEntry.Focus();

        // Tapping empty background (root Grid) dismisses whichever field is focused.
        // Entries/Buttons/the "Промени избора" link all consume their own taps first,
        // so this only fires for taps that land outside any of them.
        private void OnRootTapped(object sender, TappedEventArgs e)
        {
            NameEntry.Unfocus();
            PhoneEntry.Unfocus();
            EgnEntry.Unfocus();
        }

        private async void OnEntryFocused(object sender, FocusEventArgs e)
        {
            KeyboardSpacer.HeightRequest = KeyboardSpacerHeight;

            VisualElement? target = null;
            if (sender == NameEntry) target = NameFieldGroup;
            else if (sender == PhoneEntry) target = PhoneFieldGroup;
            else if (sender == EgnEntry) target = EgnFieldGroup;

            if (target == null)
                return;

            // Give the soft keyboard (and Android's AdjustResize layout pass) time to finish
            // animating in before measuring where "in view" actually is.
            await Task.Delay(FocusScrollDelayMs);
            await FormScrollView.ScrollToAsync(target, ScrollToPosition.Center, true);
        }

        private async void OnEntryUnfocused(object sender, FocusEventArgs e)
        {
            await Task.Delay(UnfocusCollapseDelayMs);

            if (NameEntry.IsFocused || PhoneEntry.IsFocused || EgnEntry.IsFocused)
                return;

            KeyboardSpacer.HeightRequest = 0;
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            var name = NameEntry.Text?.Trim() ?? string.Empty;
            var egn = EgnEntry.Text?.Trim() ?? string.Empty;
            var phone = PhoneEntry.Text?.Trim() ?? string.Empty;

            bool isValid = true;
            var errorColor = (Color)Application.Current!.Resources["WarmError"];

            if (name.Length < 3)
            {
                NameErrorRow.IsVisible = true;
                NameFieldBorder.Stroke = errorColor;
                isValid = false;
            }

            if (egn.Length != 10 || !egn.All(char.IsDigit))
            {
                EgnErrorRow.IsVisible = true;
                //EgnHelperLabel.IsVisible = false;
                EgnFieldBorder.Stroke = errorColor;
                isValid = false;
            }

            if (!Regex.IsMatch(phone, @"^(0\d{9}|\+359\d{9})$"))
            {
                PhoneErrorRow.IsVisible = true;
                PhoneFieldBorder.Stroke = errorColor;
                isValid = false;
            }

            if (!isValid)
                return;

            SubmitButton.IsEnabled = false;
            SubmitButton.Text = "Изпращане…";

            try
            {
                var totalEur = _selectedTests.Sum(p => p.EuroPrice);
                decimal discountTotalEur = totalEur * 0.8m;
                var manipulationsList = string.Join("\n", _selectedTests.Select((p, i) => $"{i + 1}. {p.Name} - {p.EuroPrice:F2} €"));

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"Пациент: {name}");
                messageBuilder.AppendLine($"ЕГН: {egn}");
                messageBuilder.AppendLine($"Телефон: {phone}");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"Избрани манипулации {_selectedTests.Count} бр:");
                messageBuilder.AppendLine(manipulationsList);
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"Общо сума: {totalEur:F2} €");
                messageBuilder.AppendLine("--------------------");
                messageBuilder.AppendLine($"Сума с отстъпка: {discountTotalEur:F2} €");
                messageBuilder.AppendLine("https://medsestri.com/");
                string message = messageBuilder.ToString().Trim();

                var patient = new Patient
                {
                    FullName = name,
                    Note = message,
                    EGN = egn,
                    PhoneNumber = phone,
                    Date = DateTime.Now,
                    BloodTests = _selectedTests
                };

                await Share.RequestAsync(new ShareTextRequest { Text = message, Title = "Изпрати чрез Viber" });

                var response = await _api.CreateNewPatient(patient);
                _cachedData.InvalidatePatients();

                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Грешка", "Неуспешно записване в историята", "ОК");
                    return;
                }

                foreach (var test in _selectedTests)
                    test.IsSelected = false;

                PendingConfirmationService.LastSubmitted = patient;
                await Shell.Current.GoToAsync(nameof(RequestConfirmationPage));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"Неуспешно изпращане: {ex.Message}", "OK");
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Text = "Изпрати заявка";
            }
        }
    }
}
