using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InateckMauiApp.Services;
using System.Collections.ObjectModel;

namespace InateckMauiApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IScannerService _scannerService;

    public MainViewModel(IScannerService scannerService)
    {
        _scannerService = scannerService;

        // Suscribirse a eventos del servicio
        _scannerService.StatusChanged += OnStatusChanged;
        _scannerService.ErrorOccurred += OnErrorOccurred;
        _scannerService.DataReceived += OnDataReceived;
        _scannerService.DeviceDiscovered += OnDeviceDiscovered;
        _scannerService.BatteryLevelChanged += OnBatteryLevelChanged;
    }


    // =====================================================
    // PROPIEDADES OBSERVABLES
    // =====================================================

    [ObservableProperty]
    private string _statusMessage = "Presione 'Inicializar' para comenzar";

    [ObservableProperty]
    private string _lastDataRead = "Esperando lectura...";

    [ObservableProperty]
    private string _connectedDeviceName = "Ninguno";

    [ObservableProperty]
    private string _batteryLevel = "--";

    [ObservableProperty]
    private string _deviceVersion = "--";

    [ObservableProperty]
    private bool _isInitialized = false;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private int _volumeLevel = 2;

    [ObservableProperty]
    private DeviceInfo? _selectedDevice;

    public ObservableCollection<DeviceInfo> DiscoveredDevices { get; } = new();


    // =====================================================
    // COMANDOS
    // =====================================================

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Inicializando...";

            var success = await _scannerService.InitializeAsync();

            if (success)
            {
                IsInitialized = true;
                StatusMessage = "Servicio inicializado correctamente";
            }
            else
            {
                StatusMessage = "Error al inicializar el servicio";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Excepción al inicializar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        if (IsBusy || IsScanning) return;

        try
        {
            IsBusy = true;
            IsScanning = true;
            DiscoveredDevices.Clear();
            StatusMessage = "Escaneando dispositivos BLE...";

            var devices = await _scannerService.ScanForDevicesAsync(10);

            if (devices.Count == 0)
            {
                StatusMessage = "No se encontraron dispositivos";
            }
            else
            {
                StatusMessage = $"{devices.Count} dispositivo(s) encontrado(s)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al escanear: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            IsBusy = false;
        }
    }

    private bool CanScan() => IsInitialized && !IsConnected;

    [RelayCommand(CanExecute = nameof(CanStopScan))]
    private void StopScan()
    {
        _scannerService.StopScan();
        IsScanning = false;
        StatusMessage = "Escaneo detenido";
    }

    private bool CanStopScan() => IsScanning;

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (IsBusy || SelectedDevice == null) return;

        try
        {
            IsBusy = true;
            StatusMessage = $"Conectando a {SelectedDevice.Name}...";

            var success = await _scannerService.ConnectAsync(SelectedDevice.Mac);

            if (success)
            {
                IsConnected = true;
                ConnectedDeviceName = SelectedDevice.Name;
                StatusMessage = $"Conectado a {SelectedDevice.Name}";

                // Actualizar comandos
                ScanCommand.NotifyCanExecuteChanged();
                ConnectCommand.NotifyCanExecuteChanged();
                DisconnectCommand.NotifyCanExecuteChanged();
                GetInfoCommand.NotifyCanExecuteChanged();
                SetVolumeCommand.NotifyCanExecuteChanged();
                ConfigureDataMatrixCommand.NotifyCanExecuteChanged();

                // Obtener información inicial del dispositivo
                await GetInfoAsync();

                // Configurar automáticamente para DataMatrix SOLO
                StatusMessage = "Configurando escáner para DataMatrix...";
                await Task.Delay(500); // Pequeña pausa para estabilidad
                await ConfigureDataMatrixAsync();
            }
            else
            {
                StatusMessage = $"Falló la conexión a {SelectedDevice.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al conectar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanConnect() => IsInitialized && !IsConnected && SelectedDevice != null;

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Desconectando...";

            var success = await _scannerService.DisconnectAsync();

            if (success)
            {
                IsConnected = false;
                ConnectedDeviceName = "Ninguno";
                BatteryLevel = "--";
                DeviceVersion = "--";
                StatusMessage = "Desconectado";

                // Actualizar comandos
                ScanCommand.NotifyCanExecuteChanged();
                ConnectCommand.NotifyCanExecuteChanged();
                DisconnectCommand.NotifyCanExecuteChanged();
                GetInfoCommand.NotifyCanExecuteChanged();
                SetVolumeCommand.NotifyCanExecuteChanged();
                ConfigureDataMatrixCommand.NotifyCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al desconectar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDisconnect() => IsConnected;

    [RelayCommand(CanExecute = nameof(CanGetInfo))]
    private async Task GetInfoAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Obteniendo información del dispositivo...";

            // Obtener versión
            var version = await _scannerService.GetDeviceVersionAsync();
            DeviceVersion = version ?? "Error";

            // Obtener batería
            var battery = await _scannerService.GetBatteryInfoAsync();
            BatteryLevel = battery ?? "Error";

            StatusMessage = "Información actualizada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al obtener info: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanGetInfo() => IsConnected;

    [RelayCommand(CanExecute = nameof(CanSetVolume))]
    private async Task SetVolumeAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = $"Configurando volumen a {VolumeLevel}...";

            var success = await _scannerService.SetVolumeAsync(VolumeLevel);

            if (success)
            {
                StatusMessage = $"Volumen configurado a {VolumeLevel}";
            }
            else
            {
                StatusMessage = "Error al configurar volumen";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSetVolume() => IsConnected;

    [RelayCommand(CanExecute = nameof(CanConfigureDataMatrix))]
    private async Task ConfigureDataMatrixAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Configurando escáner para SOLO DataMatrix...";

            var success = await _scannerService.ConfigureForDataMatrixOnlyAsync();

            if (success)
            {
                StatusMessage = "✓ Escáner configurado: SOLO DataMatrix habilitado";
            }
            else
            {
                StatusMessage = "Error: No se pudo configurar el escáner";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanConfigureDataMatrix() => IsConnected;


    // =====================================================
    // MANEJADORES DE EVENTOS DEL SERVICIO
    // =====================================================

    private void OnStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = status;
        });
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"ERROR: {error}";
        });
    }

    private void OnDataReceived(object? sender, string data)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LastDataRead = $"{DateTime.Now:HH:mm:ss} - {data}";
            StatusMessage = "Código recibido!";
        });
    }

    private void OnDeviceDiscovered(object? sender, DeviceInfo device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!DiscoveredDevices.Any(d => d.Mac == device.Mac))
            {
                DiscoveredDevices.Add(device);
            }
        });
    }

    private void OnBatteryLevelChanged(object? sender, int level)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BatteryLevel = $"{level}%";
        });
    }
}
