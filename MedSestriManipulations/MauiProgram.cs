using CommunityToolkit.Maui;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Services;
using Microsoft.Extensions.Logging;
using System.Text;

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

            return builder.Build();
        }
    }
}
