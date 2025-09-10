using MedSestriManipulations.Services;

namespace MedSestriManipulations
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //AppLifetimeManager.StartTracking();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}