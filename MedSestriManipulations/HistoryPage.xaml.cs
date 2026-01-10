using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Models;
using MedSestriManipulations.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MedSestriManipulations;

public partial class HistoryPage : ContentPage, INotifyPropertyChanged
{
    private readonly CachedDataService _cachedData;
    private readonly API _api;
    private List<Patient> _patients;
    private bool _isMenuOpen = false;

    private int HistoryCountPatients;

    public new event PropertyChangedEventHandler? PropertyChanged;
    private ObservableCollection<Patient> patients;

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

            HistoryCountPatients = _patients.Where(x=>x.Date > DateTime.Now.AddMonths(-1)).Count();

            patients = new ObservableCollection<Patient>(_patients);
            HistoryList.ItemsSource = patients.Take(HistoryCountPatients);
            SelectedPatient = _patients.First();

            _isMenuOpen = true;
            OnMainFabClicked(null, null);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Грешка", $"{ex.Message}", "OK");
#if ANDROID
            Java.Lang.JavaSystem.Exit(0);
#endif
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SearchText(object sender, EventArgs e)
    {
        var user = _patients.FirstOrDefault(p => p.EGN == InputEntry.Text!.Trim() ||
                                                 p.PhoneNumber == InputEntry.Text!.Trim() ||
                                                 p.FullName.ToLower().Trim().Contains(InputEntry.Text!.ToLower().Trim()))!;
        SendPatientToCard(user);

        HistoryList.ItemsSource = patients
            .OrderByDescending(m => m.FullName.ToLower().Trim().Contains(InputEntry.Text!.ToLower().Trim()))
            .Take(HistoryCountPatients);
        InputEntry.Text = "";
    }

    private void ClearSearchBar(object sender, EventArgs e)
    {
        InputEntry.Text = "";

        HistoryList.ItemsSource = patients.OrderByDescending(p => p.Date).Take(HistoryCountPatients);
        SelectedPatient = _patients.First();
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
                        _cachedData._patients.Remove(SelectedPatient);

                        patients.Remove(SelectedPatient);
                        HistoryList.ItemsSource = patients.Take(HistoryCountPatients);
                    }

                    SelectedPatient = patients.FirstOrDefault();
                }
            }
        }
    }

    private void SendPatientToCard(Patient patient)
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


    private void OnMainFabClicked(object sender, EventArgs e)
    {
        _isMenuOpen = !_isMenuOpen;

        CatheterButton.IsVisible = _isMenuOpen;
        MainWindowButton.IsVisible = _isMenuOpen;
    }
}