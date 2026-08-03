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
                fonts.AddFont("CormorantGaramond-SemiBold.ttf", "CormorantGaramondSemiBold");
                fonts.AddFont("Lora-Regular.ttf", "LoraRegular");
            });

            builder.Services.AddHttpClient<API>();
            builder.Services.AddSingleton<CachedDataService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            _ = Task.Run(async () =>
            {
                try
                {
                    if (DeviceInfo.Platform != DevicePlatform.Android && DeviceInfo.Platform != DevicePlatform.iOS)
                        return;

                    if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                        return;

                    var profiles = Connectivity.ConnectionProfiles;
                    if (profiles.Contains(ConnectionProfile.Cellular))
                        return;

                    using var scope = app.Services.CreateScope();
                    var cached = scope.ServiceProvider.GetService<CachedDataService>();
                    if (cached != null)
                    {
                        _ = cached.GetPatientsAsync();
                        _ = cached.GetBloodTestsAsync();
                        _ = cached.GetCathetersAsync();
                    }
                }
                catch {  }
            });

            return app;
        }
    }
}
