using CommunityToolkit.Maui.Views;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MedSestriManipulations;

public partial class CatheterPage : ContentPage
{
    private readonly API _api;
    private readonly CachedDataService _cacheData;
    private bool _isMenuOpen = false;

    private List<Catheter> _cathers;
    private ObservableCollection<Catheter> cathers;

    public ICommand ShowPopupCommand => new Command<object>(async (item) =>
    {
        if (item == null) return;

        ManipulateCatheter((Catheter)item);
    });

    public CatheterPage(API api, CachedDataService cacheData)
    {
        InitializeComponent();
        BindingContext = this;

        _api = api;
        _cacheData = cacheData;
        _cathers = new List<Catheter>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _cathers = await _cacheData.GetCathetersAsync();
        cathers = new ObservableCollection<Catheter>(_cathers);
        CathetersListView.ItemsSource = cathers;

        _isMenuOpen = true;
        OnMainFabClicked(null, null);

        var overdueCatheter = _cathers.FirstOrDefault(c=>c.IsOverdue == true);

        if(overdueCatheter != null) ManipulateCatheter(overdueCatheter!);
    }

    private async void ManipulateCatheter(Catheter catheter)
    {
        var popup = new CatheterPopup(catheter);

        popup.Check += (s, catheterObj) =>
        {
            if (catheterObj is Catheter catheter)
            {
                CheckCatheterAppointment(catheter);
            }
        };

        popup.Update += (s, catheterObj) =>
        {
            if (catheterObj is Catheter catheter)
            {
                UpdateCatheterAppointment(catheter);
            }
        };

        await Application.Current.MainPage.ShowPopupAsync(popup);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            if (PhoneEntry.Text.Length != 10 && PhoneEntry.Text.Length != 13)
            {
                await DisplayAlert("Грешка", "Телефонният номер трябва да съдържа точно 10 или 13 символа", "OK");
                return;
            }

            var model = new Catheter()
            {
                ClientName = PatientNameEntry.Text,
                PhoneNumber = PhoneEntry.Text,
                Date = CatheterDatePicker.Date,
                Address = AddressEntry.Text,
                IsChecked = false
            };

            var existingCatheter = _cathers.FirstOrDefault(c => c.ClientName == model.ClientName ||
                               c.Address == model.Address ||
                               c.PhoneNumber == model.PhoneNumber);

            if (existingCatheter != null)
            {
                model.Id = existingCatheter.Id;
                await _api.UpdateCatheterAppointment(model);
                cathers.Remove(existingCatheter);
            }
            else
            {
                var result = await _api.CreateCatheterappointment(model);
            }

            cathers.Add(model);
            CathetersListView.ItemsSource = cathers;
            _cacheData._isCathetersLoaded = false;
            ClearFields(null!, null!);

        }
        catch
        {

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
        await _api.CheckCatheterAppointment(catheter);
        cathers.Remove(catheter);
        CathetersListView.ItemsSource = cathers.OrderBy(d => d.Date);
        _cacheData._isCathetersLoaded = false;
    }

    private async void UpdateCatheterAppointment(Catheter catheter)
    {
        PatientNameEntry.Text = catheter.ClientName;
        PhoneEntry.Text = catheter.PhoneNumber;
        CatheterDatePicker.Date = catheter.Date;
        AddressEntry.Text = catheter.Address;
    }

    private void OnMainFabClicked(object sender, EventArgs e)
    {
        _isMenuOpen = !_isMenuOpen;

        HistoryButton.IsVisible = _isMenuOpen;
        MainWindowButton.IsVisible = _isMenuOpen;
    }
}