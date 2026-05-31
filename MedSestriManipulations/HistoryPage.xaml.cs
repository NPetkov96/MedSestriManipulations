using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Collections.ObjectModel;
using System.Threading;
using Microsoft.Maui.ApplicationModel;

namespace MedSestriManipulations
{
    public partial class HistoryPage : ContentPage
    {
        private readonly API _api;
        private readonly CachedDataService _cachedData;

        private List<Patient> _allPatients = new();
        private Patient? _selectedPatient;
        private readonly ObservableCollection<PatientGroup> _groupCollection = new();
        private CancellationTokenSource? _searchCts;

        public HistoryPage(API api, CachedDataService cachedData)
        {
            InitializeComponent();
            _api = api;
            _cachedData = cachedData;
            // keep the same collection instance to avoid reassigning ItemsSource frequently
            PatientsCollection.ItemsSource = _groupCollection;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Start loading in background so the UI appears quickly
            _ = LoadPatientsAndApplyFilterAsync();
        }

        private async Task LoadPatientsAndApplyFilterAsync()
        {
            try
            {
                PatientsSkeleton.IsLoading = true;
                PatientsCollection.IsVisible = false;

                var patients = await _cachedData.GetPatientsAsync();
                _allPatients = patients.OrderByDescending(p => p.Date).ToList();
                await ApplyFilter();
            }
            catch (Exception ex)
            {
                // Ensure alert runs on main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Грешка", $"{ex.Message}", "OK");
                });
            }
            finally
            {
                PatientsSkeleton.IsLoading = false;
                PatientsCollection.IsVisible = true;
            }
        }

        // ─── Search ───────────────────────────────────────────────────────────

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // debounce input to avoid heavy work on every keystroke
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

            await ApplyFilter(e.NewTextValue);
        }

        private async Task ApplyFilter(string? searchText = null)
        {
            searchText = (searchText ?? SearchBar.Text)?.Trim() ?? string.Empty;
            // perform filtering & grouping off the UI thread
            var groupedData = await Task.Run(() =>
            {
                IEnumerable<Patient> filtered = _allPatients;

                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = _allPatients.Where(p =>
                        p.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.EGN.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.PhoneNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase));
                }

                // Group by month/year (Bulgarian month names) and order groups by year/month descending
                var bulgarianMonths = new[]
                {
                    "Януари","Февруари","Март","Април","Май","Юни",
                    "Юли","Август","Септември","Октомври","Ноември","Декември"
                };

                var groups = filtered
                    .GroupBy(p => new { p.Date.Year, p.Date.Month })
                    .OrderByDescending(g => g.Key.Year)
                    .ThenByDescending(g => g.Key.Month)
                    .Select(g =>
                    {
                        var monthName = bulgarianMonths[g.Key.Month - 1];
                        var key = $"{monthName} {g.Key.Year}";
                        var patientsInGroup = g.OrderByDescending(p => p.Date).ToList();
                        return (Key: key, Patients: patientsInGroup);
                    })
                    .ToList();

                return groups;
            });

            // update UI collection on main thread
            bool expand = !string.IsNullOrEmpty(searchText);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _groupCollection.Clear();
                foreach (var g in groupedData)
                {
                    var pg = new PatientGroup(g.Key, g.Patients);
                    pg.IsExpanded = expand;
                    _groupCollection.Add(pg);
                }
            });
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
