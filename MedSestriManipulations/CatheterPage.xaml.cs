using CommunityToolkit.Maui.Views;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Windows.Input;

namespace MedSestriManipulations
{
    public partial class CatheterPage : ContentPage
    {
        private readonly API _api;
        private readonly CachedDataService _cacheData;
        private List<Catheter> _allCatheters = new();

        public ICommand ShowPopupCommand { get; }

        public CatheterPage(API api, CachedDataService cacheData)
        {
            InitializeComponent();
            BindingContext = this;

            _api = api;
            _cacheData = cacheData;

            ShowPopupCommand = new Command<object>(item =>
            {
                if (item is Catheter catheter) ManipulateCatheter(catheter);
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                CathetersSkeleton.IsLoading = true;
                CathetersListView.IsVisible = false;

                _allCatheters = await _cacheData.GetCathetersAsync();
                ApplyFilter();

                var overdue = _allCatheters.FirstOrDefault(c => c.IsOverdue);
                if (overdue != null) ManipulateCatheter(overdue);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"{ex.Message}", "OK");
            }
            finally
            {
                CathetersSkeleton.IsLoading = false;
                CathetersListView.IsVisible = true;
            }
        }


        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            SearchClearButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
            ApplyFilter(e.NewTextValue);
        }

        private void OnSearchClearClicked(object sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            SearchEntry.Unfocus();
        }

        private void ApplyFilter(string? searchText = null)
        {
            searchText = (searchText ?? SearchEntry.Text)?.Trim() ?? string.Empty;

            IEnumerable<Catheter> filtered = _allCatheters;

            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = _allCatheters.Where(c =>
                    c.ClientName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.PhoneNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            CathetersListView.ItemsSource = filtered.ToList();
        }


        private async void OnOpenAddSheetClicked(object sender, EventArgs e)
        {
            SheetTitleLabel.Text = "Нов катетър";
            ClearFields(null!, null!);
            await ShowSheet();
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
            PatientNameEntry.Unfocus();
            PhoneEntry.Unfocus();
            AddressEntry.Unfocus();
            _sheetLifted = false;

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


        private const double SheetKeyboardLift = 240;
        private bool _sheetLifted = false;

        private void OnNameEntryCompleted(object sender, EventArgs e) => PhoneEntry.Focus();

        private async void OnSheetEntryFocused(object sender, FocusEventArgs e)
        {
            if (_sheetLifted) return;
            _sheetLifted = true;
            await SheetPanel.TranslateTo(0, -SheetKeyboardLift, 220, Easing.CubicOut);
        }

        private async void OnSheetEntryUnfocused(object sender, FocusEventArgs e)
        {
            await Task.Delay(120);
            if (PatientNameEntry.IsFocused || PhoneEntry.IsFocused || AddressEntry.IsFocused)
                return;

            _sheetLifted = false;
            await SheetPanel.TranslateTo(0, 0, 220, Easing.CubicIn);
        }


        private async void ManipulateCatheter(Catheter catheter)
        {
            var popup = new CatheterPopup(catheter);

            popup.Check += (s, obj) =>
            {
                if (obj is Catheter c) CheckCatheterAppointment(c);
            };

            popup.Update += (s, obj) =>
            {
                if (obj is Catheter c) UpdateCatheterAppointment(c);
            };

            await this.ShowPopupAsync(popup);
        }


        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var phone = PhoneEntry.Text?.Trim() ?? string.Empty;
                if (phone.Length != 10 && phone.Length != 13)
                {
                    await DisplayAlert("Грешка", "Телефонният номер трябва да съдържа точно 10 или 13 символа", "OK");
                    return;
                }

                LoadingOverlay.IsVisible = true;

                var model = new Catheter
                {
                    ClientName = PatientNameEntry.Text?.Trim() ?? string.Empty,
                    PhoneNumber = phone,
                    Date = CatheterDatePicker.Date,
                    Address = AddressEntry.Text?.Trim() ?? string.Empty,
                    IsChecked = false
                };

                var existing = _allCatheters.FirstOrDefault(c =>
                    c.ClientName == model.ClientName ||
                    c.Address == model.Address ||
                    c.PhoneNumber == model.PhoneNumber);

                if (existing != null)
                {
                    model.Id = existing.Id;
                    await _api.UpdateCatheterAppointment(model);
                }
                else
                {
                    await _api.CreateCatheterappointment(model);
                }

                _cacheData.InvalidateCatheters();
                _allCatheters = await _cacheData.GetCathetersAsync();
                ApplyFilter();

                ClearFields(null!, null!);
                await HideSheet();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Грешка", $"Неуспешно записване: {ex.Message}", "OK");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }

        private void ClearFields(object sender, EventArgs e)
        {
            PatientNameEntry.Text = string.Empty;
            PhoneEntry.Text = string.Empty;
            CatheterDatePicker.Date = DateTime.Today;
            AddressEntry.Text = string.Empty;
        }

        private async void CheckCatheterAppointment(Catheter catheter)
        {
            try
            {
                LoadingOverlay.IsVisible = true;
                await _api.CheckCatheterAppointment(catheter);
                _cacheData.InvalidateCatheters();
                _allCatheters = await _cacheData.GetCathetersAsync();
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

        private async void UpdateCatheterAppointment(Catheter catheter)
        {
            SheetTitleLabel.Text = "Редактирай катетър";
            PatientNameEntry.Text = catheter.ClientName;
            PhoneEntry.Text = catheter.PhoneNumber;
            CatheterDatePicker.Date = catheter.Date;
            AddressEntry.Text = catheter.Address;
            await ShowSheet();
        }
    }
}
