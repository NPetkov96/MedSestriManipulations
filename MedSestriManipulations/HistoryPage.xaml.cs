using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;

namespace MedSestriManipulations
{
    public partial class HistoryPage : ContentPage
    {
        private readonly API _api;
        private readonly CachedDataService _cachedData;

        private List<Patient> _allPatients = new();
        private Patient? _selectedPatient;

        public HistoryPage(API api, CachedDataService cachedData)
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
                LoadingOverlay.IsVisible = true;

                var patients = await _cachedData.GetPatientsAsync();
                _allPatients = patients.OrderByDescending(p => p.Date).ToList();
                ApplyFilter();
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

        // ─── Search ───────────────────────────────────────────────────────────

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(e.NewTextValue);
        }

        private void ApplyFilter(string? searchText = null)
        {
            searchText = (searchText ?? SearchBar.Text)?.Trim() ?? string.Empty;

            IEnumerable<Patient> filtered = _allPatients;

            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = _allPatients.Where(p =>
                    p.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.EGN.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    p.PhoneNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            PatientsCollection.ItemsSource = filtered.ToList();
        }

        // ─── Bottom sheet ─────────────────────────────────────────────────────

        private async void OnPatientTapped(object sender, TappedEventArgs e)
        {
            if (sender is BindableObject { BindingContext: Patient patient })
            {
                _selectedPatient = patient;

                SheetNameLabel.Text = patient.FullName;
                SheetEgnLabel.Text = patient.EGN;
                SheetPhoneLabel.Text = patient.PhoneNumber;
                SheetDateLabel.Text = patient.DateText;
                SheetNoteLabel.Text = string.IsNullOrWhiteSpace(patient.Note)
                    ? "Няма записани детайли."
                    : patient.Note;

                await ShowSheet();
            }
        }

        private async Task ShowSheet()
        {
            SheetPanel.TranslationY = 600;
            SheetOverlay.Opacity = 0;
            SheetOverlay.IsVisible = true;
            SheetPanel.IsVisible = true;

            await Task.WhenAll(
                SheetOverlay.FadeTo(1, 250),
                SheetPanel.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }

        private async Task HideSheet()
        {
            await Task.WhenAll(
                SheetOverlay.FadeTo(0, 220),
                SheetPanel.TranslateTo(0, 600, 260, Easing.CubicIn)
            );
            SheetOverlay.IsVisible = false;
            SheetPanel.IsVisible = false;
        }

        private async void OnSheetOverlayTapped(object sender, TappedEventArgs e)
        {
            await HideSheet();
        }

        // ─── Actions ──────────────────────────────────────────────────────────

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
            catch { /* toast is best-effort */ }
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
                    ApplyFilter();
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
