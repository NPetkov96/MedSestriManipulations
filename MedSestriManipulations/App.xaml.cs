#if ANDROID 
using MedSestriManipulations.Platforms.Android.Services;
#endif


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

        public async Task CheckNotificationPermissionAsync()
        {
#if ANDROID
            await CheckNotificationPermissionAndroidAsync();
#else
            await Task.CompletedTask;
#endif
        }

#if ANDROID
        private async Task CheckNotificationPermissionAndroidAsync()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                var status = await Permissions.CheckStatusAsync<Notifications>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.CheckStatusAsync<Notifications>();

                    if (status != PermissionStatus.Granted)
                    {
                        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (page == null) return;

                        await page.DisplayAlert(
                            "Разрешение за известия",
                            "Известията са изключени. Моля, разрешете ги ръчно от настройките на телефона.",
                            "ОК");

                        OpenAppSettings();
                    }
                }
            }
        }
#endif

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
