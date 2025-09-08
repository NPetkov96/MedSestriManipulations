using System.Windows.Input;

namespace MedSestriManipulations.Services
{
    public class NavigationService
    {        
        public static ICommand NavigateToHomeCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("//MainPage");
        });

        public static ICommand NavigateToHistoryCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("//HistoryPage");
        });

        public static ICommand NavigateToCatheterCommand => new Command(async () =>
        {
            await Shell.Current.GoToAsync("//CatheterPage");
        });
    }
}
