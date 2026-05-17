namespace MedSestriManipulations;

public partial class LaunchPage : ContentPage
{
    private bool _animationStarted;

    public LaunchPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_animationStarted)
        {
            return;
        }

        _animationStarted = true;

        await Task.WhenAll(
            GlowRing.FadeTo(0.7, 280, Easing.CubicOut),
            GlowRing.ScaleTo(1, 520, Easing.CubicOut),
            LogoMark.FadeTo(1, 320, Easing.CubicOut),
            LogoMark.ScaleTo(1, 520, Easing.CubicOut));

        await Task.WhenAll(
            GlowRing.ScaleTo(1.1, 580, Easing.SinInOut),
            LogoMark.TranslateTo(0, -26, 580, Easing.SinInOut));

        await Task.Delay(180);

        if (Application.Current is App app)
        {
            await app.ShowMainShellAsync();
        }
    }
}
