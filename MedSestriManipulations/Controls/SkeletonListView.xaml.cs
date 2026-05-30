namespace MedSestriManipulations.Controls;

public partial class SkeletonListView : ContentView
{
    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(SkeletonListView),
            false,
            propertyChanged: OnIsLoadingChanged);

    private bool _isAnimating;

    public SkeletonListView()
    {
        InitializeComponent();
    }

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    private static void OnIsLoadingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (SkeletonListView)bindable;

        if ((bool)newValue)
        {
            view.Start();
        }
        else
        {
            view.Stop();
        }
    }

    private void Start()
    {
        IsVisible = true;

        if (_isAnimating)
        {
            return;
        }

        _isAnimating = true;
        _ = AnimateAsync();
    }

    private void Stop()
    {
        _isAnimating = false;
        SkeletonRoot.CancelAnimations();
        SkeletonRoot.Opacity = 1;
        IsVisible = false;
    }

    private async Task AnimateAsync()
    {
        while (_isAnimating)
        {
            await SkeletonRoot.FadeTo(0.35, 700);
            if (!_isAnimating)
            {
                break;
            }

            await SkeletonRoot.FadeTo(1, 700);
        }
    }
}
