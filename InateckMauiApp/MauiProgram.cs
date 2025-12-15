using InateckMauiApp.Services;
using InateckMauiApp.ViewModels;
using InateckMauiApp.Views;
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

        // =====================================================
        // REGISTRO DE SERVICIOS
        // =====================================================

        // Servicio del escáner (específico por plataforma)
#if ANDROID
        builder.Services.AddSingleton<IScannerService, AndroidScannerService>();
#else
        // Para otras plataformas, registrar un servicio mock
        builder.Services.AddSingleton<IScannerService, MockScannerService>();
#endif

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Views
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}


#if !ANDROID
// =====================================================
// MOCK SERVICE PARA OTRAS PLATAFORMAS
// =====================================================

/// <summary>
/// Implementación mock del servicio del escáner para plataformas no Android.
/// </summary>
public class MockScannerService : IScannerService
{
    public event EventHandler<DeviceInfo>? DeviceDiscovered;
    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<int>? BatteryLevelChanged;

    public bool IsInitialized => false;
    public bool IsConnected => false;
    public string? ConnectedDeviceName => null;
    public string? ConnectedDeviceMac => null;
    public IReadOnlyList<DeviceInfo> DiscoveredDevices => new List<DeviceInfo>();

    public Task<bool> InitializeAsync()
    {
        StatusChanged?.Invoke(this, "Servicio mock (solo disponible en Android)");
        return Task.FromResult(false);
    }

    public Task<List<DeviceInfo>> ScanForDevicesAsync(int durationSeconds = 10)
    {
        ErrorOccurred?.Invoke(this, "Esta funcionalidad solo está disponible en Android");
        return Task.FromResult(new List<DeviceInfo>());
    }

    public void StopScan() { }
    public Task<bool> ConnectAsync(string deviceMac) => Task.FromResult(false);
    public Task<bool> DisconnectAsync() => Task.FromResult(false);
    public Task<string?> GetDeviceVersionAsync() => Task.FromResult<string?>(null);
    public Task<string?> GetBatteryInfoAsync() => Task.FromResult<string?>(null);
    public Task<bool> SetVolumeAsync(int level) => Task.FromResult(false);
    public Task<bool> ConfigureForDataMatrixOnlyAsync() => Task.FromResult(false);
}
#endif
