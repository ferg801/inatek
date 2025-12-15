using Android.App;
using InateckBinding;
using InateckMauiApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InateckMauiApp.Platforms.Android
{
    /// <summary>
    /// Implementación específica de Android del servicio del escáner Inatek.
    /// </summary>
    public class AndroidScannerService : IScannerService
    {
        private InateckScannerWrapper? _wrapper;
        private readonly List<DeviceInfo> _discoveredDevices = new();


        // =====================================================
        // EVENTOS
        // =====================================================

        public event EventHandler<DeviceInfo>? DeviceDiscovered;
        public event EventHandler<string>? DataReceived;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<int>? BatteryLevelChanged;


        // =====================================================
        // PROPIEDADES
        // =====================================================

        public bool IsInitialized => _wrapper?.IsInitialized ?? false;

        public bool IsConnected => _wrapper?.IsConnected ?? false;

        public string? ConnectedDeviceName => _wrapper?.CurrentDevice?.Name;

        public string? ConnectedDeviceMac => _wrapper?.CurrentDevice?.Mac;

        public IReadOnlyList<DeviceInfo> DiscoveredDevices => _discoveredDevices.AsReadOnly();


        // =====================================================
        // MÉTODOS PÚBLICOS
        // =====================================================

        public Task<bool> InitializeAsync()
        {
            try
            {
                // Obtener el contexto de la aplicación Android
                var application = Application.Context as Application
                    ?? throw new InvalidOperationException("No se pudo obtener el contexto de la aplicación Android.");

                // Crear el wrapper si no existe
                _wrapper ??= new InateckScannerWrapper();

                // Suscribirse a eventos del wrapper
                SubscribeToWrapperEvents();

                // Inicializar el SDK
                _wrapper.Initialize(application);

                OnStatusChanged("Servicio inicializado correctamente");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al inicializar: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public async Task<List<DeviceInfo>> ScanForDevicesAsync(int durationSeconds = 10)
        {
            if (_wrapper == null || !IsInitialized)
            {
                OnErrorOccurred("El servicio no está inicializado. Llame a InitializeAsync() primero.");
                return new List<DeviceInfo>();
            }

            try
            {
                _discoveredDevices.Clear();
                OnStatusChanged("Escaneando dispositivos...");

                await _wrapper.ScanAsync(durationSeconds);

                OnStatusChanged($"Escaneo completo. {_discoveredDevices.Count} dispositivo(s) encontrado(s).");
                return _discoveredDevices.ToList();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error durante el escaneo: {ex.Message}");
                return new List<DeviceInfo>();
            }
        }

        public void StopScan()
        {
            try
            {
                _wrapper?.StopScan();
                OnStatusChanged("Escaneo detenido");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al detener escaneo: {ex.Message}");
            }
        }

        public async Task<bool> ConnectAsync(string deviceMac)
        {
            if (_wrapper == null || !IsInitialized)
            {
                OnErrorOccurred("El servicio no está inicializado.");
                return false;
            }

            try
            {
                // Buscar el dispositivo en la lista
                var nativeDevice = _wrapper.DiscoveredDevices.FirstOrDefault(d => d.Mac == deviceMac);

                if (nativeDevice == null)
                {
                    OnErrorOccurred($"Dispositivo con MAC {deviceMac} no encontrado.");
                    return false;
                }

                OnStatusChanged($"Conectando a {nativeDevice.Name}...");

                var success = await _wrapper.ConnectAsync(nativeDevice);

                if (success)
                {
                    OnStatusChanged($"Conectado a {nativeDevice.Name}");
                }
                else
                {
                    OnErrorOccurred($"Falló la conexión a {nativeDevice.Name}");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al conectar: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            if (_wrapper == null)
                return true;

            try
            {
                OnStatusChanged("Desconectando...");

                var success = await _wrapper.DisconnectAsync();

                if (success)
                {
                    OnStatusChanged("Desconectado");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al desconectar: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GetDeviceVersionAsync()
        {
            if (_wrapper == null || !IsConnected)
            {
                OnErrorOccurred("No hay dispositivo conectado.");
                return null;
            }

            try
            {
                var version = await _wrapper.GetVersionAsync();
                return version;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al obtener versión: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetBatteryInfoAsync()
        {
            if (_wrapper == null || !IsConnected)
            {
                OnErrorOccurred("No hay dispositivo conectado.");
                return null;
            }

            try
            {
                var batteryInfo = await _wrapper.GetBatteryInfoAsync();

                // Intentar parsear el nivel de batería si es posible
                if (!string.IsNullOrEmpty(batteryInfo) && int.TryParse(batteryInfo, out int level))
                {
                    OnBatteryLevelChanged(level);
                }

                return batteryInfo;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al obtener batería: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SetVolumeAsync(int level)
        {
            if (_wrapper == null || !IsConnected)
            {
                OnErrorOccurred("No hay dispositivo conectado.");
                return false;
            }

            if (level < 0 || level > 4)
            {
                OnErrorOccurred("El nivel de volumen debe estar entre 0 y 4.");
                return false;
            }

            try
            {
                OnStatusChanged($"Configurando volumen a {level}...");

                var success = await _wrapper.SetVolumeAsync(level);

                if (success)
                {
                    OnStatusChanged($"Volumen configurado a {level}");
                }
                else
                {
                    OnErrorOccurred("No se pudo configurar el volumen");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al configurar volumen: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConfigureForDataMatrixOnlyAsync()
        {
            if (_wrapper == null || !IsConnected)
            {
                OnErrorOccurred("No hay dispositivo conectado.");
                return false;
            }

            try
            {
                OnStatusChanged("Configurando escáner para leer SOLO DataMatrix...");

                var success = await _wrapper.ConfigureForDataMatrixOnlyAsync();

                if (success)
                {
                    OnStatusChanged("Escáner configurado: SOLO códigos DataMatrix habilitados");
                }
                else
                {
                    OnErrorOccurred("No se pudo aplicar la configuración de DataMatrix");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error al configurar DataMatrix: {ex.Message}");
                return false;
            }
        }


        // =====================================================
        // MÉTODOS PRIVADOS
        // =====================================================

        private void SubscribeToWrapperEvents()
        {
            if (_wrapper == null)
                return;

            _wrapper.DeviceDiscovered += (sender, args) =>
            {
                if (args.Device != null)
                {
                    var deviceInfo = new DeviceInfo
                    {
                        Name = args.Device.Name ?? "Desconocido",
                        Mac = args.Device.Mac ?? string.Empty,
                        ConnectionState = args.Device.ConnectState?.ToString() ?? "Unknown"
                    };

                    // Agregar a la lista si no existe
                    if (!_discoveredDevices.Any(d => d.Mac == deviceInfo.Mac))
                    {
                        _discoveredDevices.Add(deviceInfo);
                        OnDeviceDiscovered(deviceInfo);
                    }
                }
            };

            _wrapper.DeviceConnected += (sender, args) =>
            {
                OnStatusChanged($"Dispositivo conectado: {args.Device?.Name}");
            };

            _wrapper.DeviceDisconnected += (sender, args) =>
            {
                var reason = args.IsUserInitiated ? "desconexión del usuario" : "desconexión inesperada";
                OnStatusChanged($"Dispositivo desconectado: {args.Device?.Name} ({reason})");
            };

            _wrapper.DataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    OnDataReceived(args.Data);
                }
            };

            _wrapper.ErrorOccurred += (sender, args) =>
            {
                OnErrorOccurred(args.Message);
            };

            _wrapper.BatteryInfoReceived += (sender, args) =>
            {
                OnBatteryLevelChanged(args.BatteryLevel);
            };

            _wrapper.ScanStarted += (sender, args) =>
            {
                OnStatusChanged("Escaneo iniciado");
            };

            _wrapper.ScanCompleted += (sender, args) =>
            {
                OnStatusChanged($"Escaneo completado: {args.Devices?.Count ?? 0} dispositivos encontrados");
            };
        }


        // =====================================================
        // DISPARADORES DE EVENTOS
        // =====================================================

        private void OnDeviceDiscovered(DeviceInfo device)
        {
            DeviceDiscovered?.Invoke(this, device);
        }

        private void OnDataReceived(string data)
        {
            DataReceived?.Invoke(this, data);
        }

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        private void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        private void OnBatteryLevelChanged(int level)
        {
            BatteryLevelChanged?.Invoke(this, level);
        }
    }
}
