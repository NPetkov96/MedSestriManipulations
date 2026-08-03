using MedSestriManipulations.Models;
using MedSestriManipulations.Services;

namespace MedSestriManipulations
{
    public partial class RequestConfirmationPage : ContentPage
    {
        private bool _isNavigatingHome;

        public RequestConfirmationPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var patient = PendingConfirmationService.LastSubmitted;
            if (patient == null)
            {
                // Reached directly (e.g. resumed process) with nothing to show - nothing to confirm, go home.
                _ = GoHomeAsync();
                return;
            }

            PatientNameLabel.Text = patient.FullName;
            PatientPhoneLabel.Text = patient.PhoneNumber;
            PatientEgnLabel.Text = patient.EGN;
            TotalLabel.Text = $"{patient.TotalEuro:F2} €";

            var resources = Application.Current!.Resources;
            TestsLayout.Children.Clear();
            foreach (var test in patient.BloodTests)
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };

                row.Add(new Label
                {
                    Text = test.Name,
                    LineBreakMode = LineBreakMode.WordWrap,
                    FontFamily = "LoraRegular",
                    FontSize = 15,
                    TextColor = (Color)resources["WarmText"]
                }, 0, 0);

                row.Add(new Label
                {
                    Text = test.IsFree ? "Безплатно" : $"{test.EuroPrice:F2} €",
                    FontFamily = "LoraRegular",
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.End,
                    TextColor = (Color)resources["WarmTextMuted"]
                }, 1, 0);

                TestsLayout.Children.Add(row);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            _ = GoHomeAsync();
            return true;
        }

        private async void OnGoHomeClicked(object sender, EventArgs e) => await GoHomeAsync();

        private async Task GoHomeAsync()
        {
            if (_isNavigatingHome) return;
            _isNavigatingHome = true;

            PendingConfirmationService.LastSubmitted = null;
            // Absolute route: resets the whole tab's navigation stack back to its root,
            // so Back can never return to this confirmation screen or the submitted form.
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
