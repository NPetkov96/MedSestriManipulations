using MedSestriManipulations.Services;

namespace MedSestriManipulations;

public partial class LaunchPage : ContentPage
{
    private bool _animationStarted;

    // Minimum time the brand splash stays visible, even if data loads instantly.
    private const int MinBrandTimeMs = 1300;

    public LaunchPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_animationStarted)
            return;

        _animationStarted = true;

        // Kick off data preload immediately, in the background. CachedDataService
        // is a singleton, so on cached runs MainPage gets the data instantly.
        // We deliberately do NOT block the splash on it: on a first run (empty
        // cache + slow API) MainPage's skeleton covers the wait instead.
        var cachedData = IPlatformApplication.Current?.Services.GetService<CachedDataService>();
        if (cachedData != null)
            _ = SafePreloadAsync(cachedData);

        // Dismiss when both the entrance animation and the minimum brand time
        // have completed — no fixed dead-time hold.
        var minTimeTask = Task.Delay(MinBrandTimeMs);
        var animationTask = RunEntranceAnimationAsync();

        await Task.WhenAll(animationTask, minTimeTask);

        await this.FadeTo(0, 260, Easing.CubicIn);

        if (Application.Current is App app)
            await app.ShowMainShellAsync();
    }

    private static async Task SafePreloadAsync(CachedDataService cachedData)
    {
        try
        {
            await cachedData.GetBloodTestsAsync();
        }
        catch
        {
            // Network/cache errors are handled on MainPage; never block the splash.
        }
    }

    private async Task RunEntranceAnimationAsync()
    {
        await Task.WhenAll(
            MainGlow.FadeTo(0.85, 420, Easing.CubicOut),
            MainGlow.ScaleTo(1, 700, Easing.SpringOut),

            SecondaryGlow.FadeTo(0.45, 520, Easing.CubicOut),
            SecondaryGlow.ScaleTo(1, 900, Easing.SinOut),

            OuterGlow.FadeTo(0.22, 620, Easing.CubicOut),
            OuterGlow.ScaleTo(1, 1100, Easing.SinOut));

        await Task.WhenAll(
            LogoMark.FadeTo(1, 480, Easing.CubicOut),
            LogoMark.ScaleTo(1, 720, Easing.SpringOut),
            LogoMark.TranslateTo(0, -18, 720, Easing.CubicOut));

        // Subtle settle (no fixed dead-time hold afterwards).
        await Task.WhenAll(
            LogoMark.TranslateTo(0, -22, 220, Easing.SinInOut),
            MainGlow.ScaleTo(1.02, 320, Easing.SinInOut));
    }
}
