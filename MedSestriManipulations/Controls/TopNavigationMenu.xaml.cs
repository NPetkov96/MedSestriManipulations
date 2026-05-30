namespace MedSestriManipulations.Controls;

public partial class TopNavigationMenu : ContentView
{
    public TopNavigationMenu()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, EventArgs e)
    {
        UpdateActivePage();
    }

    private void ToggleMenu(object sender, EventArgs e)
    {
        NavigationPanel.IsVisible = !NavigationPanel.IsVisible;
        MenuButton.Text = NavigationPanel.IsVisible ? "Затвори" : "Меню";
        UpdateActivePage();
    }

    private async void GoHome(object sender, EventArgs e)
    {
        CloseMenu();
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void GoHistory(object sender, EventArgs e)
    {
        CloseMenu();
        await Shell.Current.GoToAsync("//HistoryPage");
    }

    private async void GoCatheters(object sender, EventArgs e)
    {
        CloseMenu();
        await Shell.Current.GoToAsync("//CatheterPage");
    }

    private void CloseMenu()
    {
        NavigationPanel.IsVisible = false;
        MenuButton.Text = "Меню";
    }

    private void UpdateActivePage()
    {
        var route = Shell.Current?.CurrentState?.Location.ToString() ?? string.Empty;

        SetButtonState(HomeButton, route.Contains("MainPage", StringComparison.OrdinalIgnoreCase));
        SetButtonState(HistoryButton, route.Contains("HistoryPage", StringComparison.OrdinalIgnoreCase));
        SetButtonState(CathetersButton, route.Contains("CatheterPage", StringComparison.OrdinalIgnoreCase));
    }

    private static void SetButtonState(Button button, bool isActive)
    {
        button.BackgroundColor = isActive ? Color.FromArgb("#0066CC") : Colors.Transparent;
        button.TextColor = isActive ? Colors.White : Color.FromArgb("#1A1A1A");
        button.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
    }
}
