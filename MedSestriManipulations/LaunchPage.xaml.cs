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
            MainGlow.FadeTo(0.85, 420, Easing.CubicOut),
            MainGlow.ScaleTo(1, 700, Easing.SpringOut),

            SecondaryGlow.FadeTo(0.45, 520, Easing.CubicOut),
            SecondaryGlow.ScaleTo(1, 900, Easing.SinOut),

            OuterGlow.FadeTo(0.22, 620, Easing.CubicOut),
            OuterGlow.ScaleTo(1, 1100, Easing.SinOut));

        await Task.Delay(150);

        await Task.WhenAll(
            LogoMark.FadeTo(1, 520, Easing.CubicOut),
            LogoMark.ScaleTo(1, 820, Easing.SpringOut),
            LogoMark.TranslateTo(0, -18, 820, Easing.CubicOut));

        await Task.WhenAll(
            LogoMark.TranslateTo(0, -26, 260, Easing.SinOut),
            MainGlow.ScaleTo(1.05, 420, Easing.SinInOut));

        await Task.WhenAll(
            LogoMark.TranslateTo(0, -22, 220, Easing.SinInOut),
            MainGlow.ScaleTo(1.02, 320, Easing.SinInOut));

        await Task.Delay(500);
        await this.FadeTo(0, 260, Easing.CubicIn);

        if (Application.Current is App app)
        {
            await app.ShowMainShellAsync();
        }
    }
}
