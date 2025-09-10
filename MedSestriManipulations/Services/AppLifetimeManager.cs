using System.Timers;

namespace MedSestriManipulations.Services
{
    public static class AppLifetimeManager
    {

        private const string StartTimeKey = "AppStartTime";
        private static System.Timers.Timer checkTimer;

        public static void StartTracking()
        {
            if (!Preferences.ContainsKey(StartTimeKey))
            {
                Preferences.Set(StartTimeKey, DateTime.UtcNow);
            }

            checkTimer = new System.Timers.Timer(TimeSpan.FromMinutes(120).TotalMilliseconds);
            checkTimer.Elapsed += CheckAppUptime;
            checkTimer.AutoReset = true;
            checkTimer.Start();
        }
        private static void CheckAppUptime(object sender, ElapsedEventArgs e)
        {
            var startTime = Preferences.Get(StartTimeKey, DateTime.UtcNow);
            var now = DateTime.UtcNow;

            var uptime = now - startTime;
            if (uptime.TotalMinutes >= 120)
            {
                ShutdownApp();
            }
        }

        private static void ShutdownApp()
        {
#if ANDROID
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif WINDOWS
        System.Environment.Exit(0);
#endif
        }

        public static void Reset()
        {
            Preferences.Remove(StartTimeKey);
            checkTimer?.Stop();
            checkTimer?.Dispose();
        }
    }
}
