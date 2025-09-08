using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using System.Collections.ObjectModel;

namespace MedSestriManipulations;

public partial class CatheterPage : ContentPage
{
    private readonly API _api;
    private List<Catheter> _cathers;
    private ObservableCollection<Catheter> cathers;

    public CatheterPage(API api)
    {
        InitializeComponent();
        BindingContext = this;

        _api = api;
        _cathers = new List<Catheter>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _cathers = await _api.GetAllCatheterAppointments();
        cathers = new ObservableCollection<Catheter>(_cathers);
        CathetersListView.ItemsSource = cathers;
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

            var result = await _api.CreateCatheterappointment(model);
            if (result.IsSuccessStatusCode)
            {
                cathers.Add(model);
                CathetersListView.ItemsSource = cathers;
                ClearFields(null!, null!);
            }
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

    private async void UpdateCatheterAppointment_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Catheter catheter)
        {
            await _api.CheckCatheterAppointment(catheter);
            cathers.Remove(catheter);
            CathetersListView.ItemsSource = cathers.OrderBy(d => d.Date);
        }

    }

    private bool _fabOpen = false;

    private void OnMainFabClicked(object sender, EventArgs e)
    {
        _fabOpen = !_fabOpen;

        HistoryButton.IsVisible = _fabOpen;
        MainWindowButton.IsVisible = _fabOpen;
    }
}