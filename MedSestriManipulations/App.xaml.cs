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
            // Show the main shell immediately to reduce perceived startup time.
            return new Window(new AppShell());
        }
    }
}
