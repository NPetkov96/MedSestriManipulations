using CommunityToolkit.Maui;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Services;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;

namespace MedSestriManipulations
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = MauiApp.CreateBuilder();

            builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

            builder.Services.AddHttpClient<API>();
            builder.Services.AddSingleton<CachedDataService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Conditional warm-up: only run on mobile platforms and when we have internet connectivity
            _ = Task.Run(async () =>
            {
                try
                {
                    // Platform check: only warm on Android/iOS where it helps mobile UX
                    if (DeviceInfo.Platform != DevicePlatform.Android && DeviceInfo.Platform != DevicePlatform.iOS)
                        return;

                    // Network check: require Internet access
                    if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                        return;

                    // Optionally skip warm-up on cellular to avoid using mobile data
                    var profiles = Connectivity.ConnectionProfiles;
                    if (profiles.Contains(ConnectionProfile.Cellular))
                        return;

                    using var scope = app.Services.CreateScope();
                    var cached = scope.ServiceProvider.GetService<CachedDataService>();
                    if (cached != null)
                    {
                        // Fire-and-forget loads; CachedDataService will reuse in-flight tasks.
                        _ = cached.GetPatientsAsync();
                        _ = cached.GetBloodTestsAsync();
                        _ = cached.GetCathetersAsync();
                    }
                }
                catch { /* best effort warm-up, swallow errors */ }
            });

            return app;
        }
    }
}
