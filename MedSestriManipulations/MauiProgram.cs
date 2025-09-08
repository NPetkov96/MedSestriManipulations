using CommunityToolkit.Maui;
using MedSestriManipulations.ApiHandler;
using MedSestriManipulations.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MedSestriManipulations
{
    public static class MauiProgram
    {
        public static MauiApp AppInstance { get; private set; }

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

            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://medsestribackendapi20250430210231-d9b9grdkdnecc0aw.italynorth-01.azurewebsites.net/");
            });

            builder.Services.AddSingleton<PaginationState>();
            builder.Services.AddSingleton<API>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            AppInstance = app;

            return app;
        }
    }
}
