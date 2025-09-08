using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Color = Android.Graphics.Color;

namespace MedSestriManipulations.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop && Window != null)
            {
                Window.SetStatusBarColor(Color.ParseColor("#007BFF"));
            }

            TryIgnoreBatteryOptimizations();
            RequestSmsPermissions();
        }

        private void TryIgnoreBatteryOptimizations()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                PowerManager? pm = (PowerManager?)GetSystemService(PowerService);
                string packageName = PackageName;

                if (pm != null && !pm.IsIgnoringBatteryOptimizations(packageName))
                {
                    Intent intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(global::Android.Net.Uri.Parse("package:" + packageName));
                    StartActivity(intent);
                }
            }
        }

        private void RequestSmsPermissions()
        {
            string[] permissions = new[]
            {
                Manifest.Permission.ReceiveSms,
                Manifest.Permission.ReadSms,
                Manifest.Permission.SendSms
            };

            const int RequestId = 1001;

            foreach (var permission in permissions)
            {
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, permissions, RequestId);
                    break;
                }
            }
        }
    }
}
