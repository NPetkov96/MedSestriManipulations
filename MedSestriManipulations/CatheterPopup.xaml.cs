using CommunityToolkit.Maui.Views;
using MedSestriManipulations.Models;

namespace MedSestriManipulations;

public partial class CatheterPopup : Popup
{
    public event EventHandler<object> Check;
    public event EventHandler<object> Update;

    public CatheterPopup(object context)
    {
        InitializeComponent();
        BindingContext = context;
    }

    private async void CheckCatheterAppointment(object sender, EventArgs e)
    {
        if (BindingContext is Catheter catheter) Check?.Invoke(this, catheter);
        Close();
    }

    private async void UpdateCatheterAppointment(object sender, EventArgs e)
    {
        if (BindingContext is Catheter catheter) Update?.Invoke(this, catheter);
        Close();
    }

    private async void CallCatheter(object sender, EventArgs e)
    {
        if (BindingContext is Catheter catheter) PhoneDialer.Default.Open(catheter.PhoneNumber);
        Close();
    }
}