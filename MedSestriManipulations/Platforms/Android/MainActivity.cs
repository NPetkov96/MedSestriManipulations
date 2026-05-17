using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Firebase.Messaging;
using MedSestriManipulations.Services;
using Plugin.Firebase.CloudMessaging;
using Color = Android.Graphics.Color;

namespace MedSestriManipulations.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private CachedDataService? _cacheData;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _cacheData = IPlatformApplication.Current?.Services.GetService<CachedDataService>();

            HandleIntent(Intent);

            FirebaseMessaging.Instance.SubscribeToTopic("all");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop && Window != null)
            {
                Window.SetStatusBarColor(Color.ParseColor("#007BFF"));
            }

        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        private void HandleIntent(Intent? intent)
        {
            if (intent?.Extras != null && intent.Extras.ContainsKey("navigate"))
            {
                var destination = intent.Extras.GetString("navigate");
                if (destination == "catheter")
                {
                    _cacheData?.InvalidateCatheters();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Shell.Current.GoToAsync("//CatheterPage");
                    });
                }
            }
            FirebaseCloudMessagingImplementation.OnNewIntent(intent);

        }
    }
}
