using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using Firebase.Messaging;
using MedSestriManipulations.Services;
using Plugin.Firebase.CloudMessaging;
using Color = Android.Graphics.Color;

namespace MedSestriManipulations.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        WindowSoftInputMode = SoftInput.AdjustResize,
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


            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop && Window != null)
            {
                // Matches WarmSurface from WarmDesign.xaml - the top app bar's background -
                // so the status bar blends into the header instead of showing the old blue.
                Window.SetStatusBarColor(Color.ParseColor("#EAE9E9"));

                // Light background needs dark status bar icons/text (clock, battery, signal),
                // otherwise they stay white and are invisible against it.
                var insetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
                if (insetsController != null)
                    insetsController.AppearanceLightStatusBars = true;
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
