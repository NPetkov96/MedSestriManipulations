// Notification platform services removed; no platform-specific using here.

namespace MedSestriManipulations
{
    public partial class App : Application
    {
        private bool _mainShellShown;

        public App()
        {
            InitializeComponent();
        }
        protected override void OnStart()
        {
        }

    public Task CheckNotificationPermissionAsync() => Task.CompletedTask;

        public void OpenAppSettings()
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.SetData(Android.Net.Uri.Parse("package:" + context.PackageName));
            intent.SetFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new LaunchPage());
        }

        public async Task ShowMainShellAsync()
        {
            if (_mainShellShown)
            {
                return;
            }

            _mainShellShown = true;

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window == null)
            {
                return;
            }

            window.Page = new AppShell();
            await CheckNotificationPermissionAsync();
        }
    }
}
