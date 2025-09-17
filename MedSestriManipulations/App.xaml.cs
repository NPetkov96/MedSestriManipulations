#if ANDROID 
using MedSestriManipulations.Platforms.Android.Services;
#endif


namespace MedSestriManipulations
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //AppLifetimeManager.StartTracking();
        }
        protected override async void OnStart()
        {
            await CheckNotificationPermissionAsync();
        }

        public async Task CheckNotificationPermissionAsync()
        {
#if ANDROID
            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
            {
                var status = await Permissions.CheckStatusAsync<Notifications>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.CheckStatusAsync<Notifications>();

                    if (status != PermissionStatus.Granted)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Разрешение за известия",
                            "Известията са изключени. Моля, разрешете ги ръчно от настройките на телефона.",
                            "ОК");

                        OpenAppSettings();
                    }
                }
            }
#endif
        }

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
            return new Window(new AppShell());
        }
    }
}