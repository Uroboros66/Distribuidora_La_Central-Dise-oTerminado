using Microsoft.Extensions.Logging;
using Distribuidora_La_Central.Shared.Services;
using Distribuidora_La_Central.Services;
using Distribuidora_La_Central.Shared.Helpers;

namespace Distribuidora_La_Central;

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
            });

        // Add Blazor WebView
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Servicio compartido (por ejemplo, para detectar el dispositivo)
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        // Agregar HttpClient con base address apuntando al backend (.Web)
#if ANDROID
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5282")
        });
#else
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5282")
            });
#endif
        // REGISTRAR HttpService
        builder.Services.AddScoped<HttpService>();

        return builder.Build();
    }

}
