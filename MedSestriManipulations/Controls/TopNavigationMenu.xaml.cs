namespace MedSestriManipulations.Controls;

public partial class TopNavigationMenu : ContentView
{
    private bool _isNavigating;

    public TopNavigationMenu()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, EventArgs e)
    {
        UpdateActivePage();
    }

    private async void GoHome(object sender, EventArgs e)
    {
        await NavigateToAsync("//MainPage", "MainPage");
    }

    private async void GoHistory(object sender, EventArgs e)
    {
        await NavigateToAsync("//HistoryPage", "HistoryPage");
    }

    private async void GoCatheters(object sender, EventArgs e)
    {
        await NavigateToAsync("//CatheterPage", "CatheterPage");
    }

    private async Task NavigateToAsync(string route, string routeName)
    {
        var shell = Shell.Current;
        if (_isNavigating || shell == null)
            return;

        var currentRoute = shell.CurrentState?.Location.ToString() ?? string.Empty;
        if (currentRoute.Contains(routeName, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            _isNavigating = true;
            await shell.GoToAsync(route);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private void UpdateActivePage()
    {
        var route = Shell.Current?.CurrentState?.Location.ToString() ?? string.Empty;

        SetTabState(HomeTabLabel, HomeUnderline, route.Contains("MainPage", StringComparison.OrdinalIgnoreCase));
        SetTabState(HistoryTabLabel, HistoryUnderline, route.Contains("HistoryPage", StringComparison.OrdinalIgnoreCase));
        SetTabState(CathetersTabLabel, CathetersUnderline, route.Contains("CatheterPage", StringComparison.OrdinalIgnoreCase));
    }

    private static void SetTabState(Label label, BoxView underline, bool isActive)
    {
        var activeColor = (Color)Application.Current!.Resources["WarmAccent700"];
        var inactiveColor = (Color)Application.Current!.Resources["WarmTextMuted"];

        label.TextColor = isActive ? activeColor : inactiveColor;
        underline.IsVisible = isActive;
    }
}
