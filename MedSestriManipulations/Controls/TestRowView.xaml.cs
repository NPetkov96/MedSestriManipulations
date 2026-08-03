using System.ComponentModel;
using MedSestriManipulations.Models;

namespace MedSestriManipulations.Controls;

public partial class TestRowView : ContentView
{
    private BloodTest? _boundTest;

    public TestRowView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_boundTest != null)
            _boundTest.PropertyChanged -= OnTestPropertyChanged;

        _boundTest = BindingContext as BloodTest;

        if (_boundTest != null)
        {
            _boundTest.PropertyChanged += OnTestPropertyChanged;
            ApplySelectionState(_boundTest.IsSelected);
        }
    }

    private void OnTestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BloodTest.IsSelected) && _boundTest != null)
            ApplySelectionState(_boundTest.IsSelected);
    }

    private void ApplySelectionState(bool isSelected)
    {
        VisualStateManager.GoToState(RowBorder, isSelected ? "Selected" : "Unselected");
    }
}
