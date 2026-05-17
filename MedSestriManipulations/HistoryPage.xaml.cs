using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MedSestriManipulations;

public partial class HistoryPage : ContentPage
{
    private readonly CachedDataService _cachedData;
    private readonly API _api;
    private List<Patient> _patients;

    private int HistoryCountPatients;

    private ObservableCollection<Patient> patients = new();

    public ICommand UpdateCard { get; }

    private Patient? selectedPatient;
    public Patient? SelectedPatient
    {
        get => selectedPatient;
        set
        {
            if (selectedPatient != value)
            {
                selectedPatient = value;
                UpdateSelectedPatientInCollection(selectedPatient);
                OnPropertyChanged();
            }
        }
    }


    public HistoryPage(API api, CachedDataService cachedData)
    {
        InitializeComponent();
        BindingContext = this;
        UpdateCard = new Command<Patient>(SendPatientToCard);

        _patients = new List<Patient>();
        _api = api;
        _cachedData = cachedData;
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            _patients = await _cachedData.GetPatientsAsync();

            HistoryCountPatients = _patients.Count(x => x.Date > DateTime.Now.AddMonths(-1));

            patients = new ObservableCollection<Patient>(_patients);
            HistoryList.ItemsSource = patients.Take(HistoryCountPatients);
            SelectedPatient = _patients.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Грешка", $"{ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
    private void SearchText(object sender, EventArgs e)
    {
        var searchText = InputEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(searchText)) return;

        var user = _patients.FirstOrDefault(p => p.EGN == searchText ||
                                                 p.PhoneNumber == searchText ||
                                                 p.FullName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
        SendPatientToCard(user);

        HistoryList.ItemsSource = patients
            .OrderByDescending(m => m.FullName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
            .Take(HistoryCountPatients);
        InputEntry.Text = "";
    }

    private void ClearSearchBar(object sender, EventArgs e)
    {
        InputEntry.Text = "";

        HistoryList.ItemsSource = patients.OrderByDescending(p => p.Date).Take(HistoryCountPatients);
        SelectedPatient = _patients.FirstOrDefault();
    }

    private async void Coppy(object sender, EventArgs e)
    {
        await Clipboard.SetTextAsync(SelectedPatient!.Note);
    }
    private async void ReUsePatient(object sender, EventArgs e)
    {
        SelectedPatientService.PatientToReuse = SelectedPatient;
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void Delete(object sender, EventArgs e)
    {
        if (await DisplayAlert("Потвърждение", $"Сигурни ли сте, че искате да изтриете пациента: {SelectedPatient!.FullName}?", "ДА", "НЕ"))
        {
            if (await DisplayAlert("Потвърждение", $"Пациент: {SelectedPatient!.FullName} - ЕГН: {SelectedPatient.EGN} \n ИЗТРИЙ?", "ДА", "НЕ"))
            {
                var result = await _api.DeletePatient(SelectedPatient.Date);

                if (result.IsSuccessStatusCode)
                {
                    if (SelectedPatient != null && patients.Contains(SelectedPatient))
                    {
                        patients.Remove(SelectedPatient);
                        _cachedData.InvalidatePatients();
                        HistoryList.ItemsSource = patients.Take(HistoryCountPatients);
                    }

                    SelectedPatient = patients.FirstOrDefault();
                }
            }
        }
    }

    private void SendPatientToCard(Patient? patient)
    {
        if (patient == null)
            return;

        patient.IsSelected = !patient.IsSelected;

        SelectedPatient = patient;

    }

    private void UpdateSelectedPatientInCollection(Patient? selected)
    {
        foreach (var patient in _patients)
        {
            patient.IsSelected = patient == selected;
        }
    }
}
