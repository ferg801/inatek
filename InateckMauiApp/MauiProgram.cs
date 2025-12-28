using InateckMauiApp.Services;
using Microsoft.Extensions.Logging;

#if ANDROID
using InateckMauiApp.Platforms.Android;
#endif

namespace InateckMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services
        #if ANDROID
        builder.Services.AddSingleton<IScannerService>(sp =>
        {
            var context = Android.App.Application.Context 
                ?? throw new InvalidOperationException("Android Context not available");
            return new AndroidScannerService(context);
        });
        #endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

