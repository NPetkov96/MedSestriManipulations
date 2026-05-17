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

    private List<Catheter> _cathers;
    private ObservableCollection<Catheter> cathers = new();

    public ICommand ShowPopupCommand => new Command<object>(item =>
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

            var model = new Catheter()
            {
                ClientName = PatientNameEntry.Text?.Trim() ?? string.Empty,
                PhoneNumber = phone,
                Date = CatheterDatePicker.Date,
                Address = AddressEntry.Text?.Trim() ?? string.Empty,
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
                await _api.CreateCatheterappointment(model);
            }

            cathers.Add(model);
            _cacheData.InvalidateCatheters();
            ClearFields(null!, null!);

        }
        catch (Exception ex)
        {
            await DisplayAlert("Грешка", $"Неуспешно записване: {ex.Message}", "OK");
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
        _cacheData.InvalidateCatheters();
    }

    private void UpdateCatheterAppointment(Catheter catheter)
    {
        PatientNameEntry.Text = catheter.ClientName;
        PhoneEntry.Text = catheter.PhoneNumber;
        CatheterDatePicker.Date = catheter.Date;
        AddressEntry.Text = catheter.Address;
    }
}
