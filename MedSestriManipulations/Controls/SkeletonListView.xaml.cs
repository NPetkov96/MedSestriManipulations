using Microsoft.Maui.Controls.Shapes;

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

    public static readonly BindableProperty SkeletonTypeProperty =
        BindableProperty.Create(
            nameof(SkeletonType),
            typeof(SkeletonType),
            typeof(SkeletonListView),
            SkeletonType.Patient,
            propertyChanged: OnSkeletonTypeChanged);

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

    public SkeletonType SkeletonType
    {
        get => (SkeletonType)GetValue(SkeletonTypeProperty);
        set => SetValue(SkeletonTypeProperty, value);
    }

    private static void OnSkeletonTypeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (SkeletonListView)bindable;
        view.GenerateSkeletonChildren();
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

        GenerateSkeletonChildren();

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

    private void GenerateSkeletonChildren()
    {
        if (SkeletonRoot == null)
            return;

        SkeletonRoot.Children.Clear();

        var resources = Application.Current!.Resources;
        Color placeholderColor = (Color)resources["WarmNeutral300"];
        Color cardBackground = (Color)resources["WarmBg"];
        Color cardStroke = (Color)resources["WarmDivider"];

        Border CreateCard(View content, double height)
        {
            return new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) },
                BackgroundColor = cardBackground,
                Stroke = cardStroke,
                StrokeThickness = 1,
                HeightRequest = height,
                Content = content
            };
        }

        switch (SkeletonType)
        {
            case SkeletonType.BloodTest:
            {
                for (int i = 0; i < 7; i++)
                {
                    var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(14,8) };

                    var square = new BoxView { WidthRequest = 24, HeightRequest = 24, BackgroundColor = placeholderColor, CornerRadius = 12, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center };
                    grid.Add(square);

                    var center = new BoxView { WidthRequest = 180, HeightRequest = 14, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center };
                    grid.Add(center, 1, 0);

                    var pill = new BoxView { WidthRequest = 50, HeightRequest = 22, BackgroundColor = placeholderColor, CornerRadius = 10, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
                    grid.Add(pill, 2, 0);

                    SkeletonRoot.Children.Add(CreateCard(grid, 52));
                }

                break;
            }
            case SkeletonType.Patient:
            {
                for (int i = 0; i < 6; i++)
                {
                    var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(14,10) };

                    var left = new VerticalStackLayout { Spacing = 6 };
                    left.Add(new BoxView { WidthRequest = 160, HeightRequest = 14, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.Start });
                    left.Add(new BoxView { WidthRequest = 100, HeightRequest = 10, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.Start });
                    grid.Add(left);

                    var right = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
                    right.Add(new BoxView { WidthRequest = 55, HeightRequest = 18, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.End });
                    right.Add(new BoxView { WidthRequest = 70, HeightRequest = 14, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.End });
                    grid.Add(right, 1, 0);

                    SkeletonRoot.Children.Add(CreateCard(grid, 68));
                }

                break;
            }
            case SkeletonType.Catheter:
            {
                for (int i = 0; i < 6; i++)
                {
                    var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(14,12) };

                    var left = new VerticalStackLayout { Spacing = 6 };
                    left.Add(new BoxView { WidthRequest = 150, HeightRequest = 14, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.Start });
                    left.Add(new BoxView { WidthRequest = 100, HeightRequest = 10, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.Start });
                    left.Add(new BoxView { WidthRequest = 120, HeightRequest = 10, BackgroundColor = placeholderColor, CornerRadius = 4, HorizontalOptions = LayoutOptions.Start });
                    grid.Add(left);

                    var right = new VerticalStackLayout { Spacing = 0, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
                    right.Add(new BoxView { WidthRequest = 75, HeightRequest = 28, BackgroundColor = placeholderColor, CornerRadius = 6, HorizontalOptions = LayoutOptions.End });
                    grid.Add(right, 1, 0);

                    SkeletonRoot.Children.Add(CreateCard(grid, 78));
                }

                break;
            }
        }
    }
}
