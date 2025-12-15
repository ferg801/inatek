using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace InateckBinding
{
    /// <summary>
    /// Wrapper C# para facilitar el uso del SDK de Inatek desde .NET.
    /// Convierte callbacks de Kotlin a eventos y Tasks de C#.
    /// </summary>
    public class InateckScannerWrapper : IDisposable
    {
        private Application? _application;
        private Com.Inateck.Scanner.Ble.InateckScannerDevice? _currentDevice;
        private bool _isInitialized;
        private bool _disposed;

        // =====================================================
        // EVENTOS C#
        // =====================================================

        /// <summary>
        /// Se dispara cuando se inicia un escaneo de dispositivos.
        /// </summary>
        public event EventHandler<ScanStartedEventArgs>? ScanStarted;

        /// <summary>
        /// Se dispara cuando se encuentra un dispositivo durante el escaneo.
        /// </summary>
        public event EventHandler<DeviceDiscoveredEventArgs>? DeviceDiscovered;

        /// <summary>
        /// Se dispara cuando el escaneo finaliza.
        /// </summary>
        public event EventHandler<ScanCompletedEventArgs>? ScanCompleted;

        /// <summary>
        /// Se dispara cuando un dispositivo se conecta exitosamente.
        /// </summary>
        public event EventHandler<DeviceConnectedEventArgs>? DeviceConnected;

        /// <summary>
        /// Se dispara cuando un dispositivo se desconecta.
        /// </summary>
        public event EventHandler<DeviceDisconnectedEventArgs>? DeviceDisconnected;

        /// <summary>
        /// Se dispara cuando se reciben datos del escáner (código de barras leído).
        /// </summary>
        public event EventHandler<DataReceivedEventArgs>? DataReceived;

        /// <summary>
        /// Se dispara cuando ocurre un error.
        /// </summary>
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// Se dispara cuando cambia el estado de la batería.
        /// </summary>
        public event EventHandler<BatteryInfoEventArgs>? BatteryInfoReceived;


        // =====================================================
        // PROPIEDADES
        // =====================================================

        /// <summary>
        /// Indica si el wrapper está inicializado.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Indica si hay un dispositivo conectado actualmente.
        /// </summary>
        public bool IsConnected => _currentDevice?.ConnectState == Com.Inateck.Scanner.Ble.BleScannerConnectState.Connected;

        /// <summary>
        /// Dispositivo actualmente conectado (null si no hay conexión).
        /// </summary>
        public Com.Inateck.Scanner.Ble.InateckScannerDevice? CurrentDevice => _currentDevice;

        /// <summary>
        /// Lista de dispositivos encontrados durante el último escaneo.
        /// </summary>
        public IReadOnlyList<Com.Inateck.Scanner.Ble.InateckScannerDevice> DiscoveredDevices
        {
            get
            {
                var devices = Com.Inateck.Scanner.Ble.InateckBleListManager.ScannerDevices;
                return devices?.ToArray() ?? Array.Empty<Com.Inateck.Scanner.Ble.InateckScannerDevice>();
            }
        }


        // =====================================================
        // MÉTODOS PÚBLICOS
        // =====================================================

        /// <summary>
        /// Inicializa el SDK de Inatek.
        /// </summary>
        /// <param name="application">Instancia de la aplicación Android.</param>
        public void Initialize(Application application)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            if (_isInitialized)
                return;

            _application = application ?? throw new ArgumentNullException(nameof(application));

            try
            {
                Com.Inateck.Scanner.Ble.InateckBleListManager.Init(_application);

                // Configurar handler de desconexiones
                Com.Inateck.Scanner.Ble.InateckBleListManager.DisconnectHandler = (device, isUserInitiated) =>
                {
                    OnDeviceDisconnected(new DeviceDisconnectedEventArgs
                    {
                        Device = device,
                        IsUserInitiated = isUserInitiated,
                        Timestamp = DateTime.Now
                    });
                };

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Error al inicializar el SDK: {ex.Message}", ex));
                throw;
            }
        }

        /// <summary>
        /// Inicia el escaneo de dispositivos BLE.
        /// </summary>
        /// <param name="scanDurationSeconds">Duración del escaneo en segundos (por defecto 10).</param>
        /// <returns>Task que se completa cuando el escaneo finaliza.</returns>
        public Task<List<Com.Inateck.Scanner.Ble.InateckScannerDevice>> ScanAsync(int scanDurationSeconds = 10)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            if (!_isInitialized)
                throw new InvalidOperationException("Debe llamar a Initialize() antes de escanear.");

            var tcs = new TaskCompletionSource<List<Com.Inateck.Scanner.Ble.InateckScannerDevice>>();

            try
            {
                Com.Inateck.Scanner.Ble.InateckBleListManager.Scan(new ScanCallback(
                    onStarted: devices =>
                    {
                        OnScanStarted(new ScanStartedEventArgs { Devices = devices });
                    },
                    onScanning: device =>
                    {
                        OnDeviceDiscovered(new DeviceDiscoveredEventArgs { Device = device });
                    },
                    onFinished: devices =>
                    {
                        OnScanCompleted(new ScanCompletedEventArgs { Devices = devices });
                        tcs.TrySetResult(devices?.ToList() ?? new List<Com.Inateck.Scanner.Ble.InateckScannerDevice>());
                    }
                ));

                // Auto-detener escaneo después del tiempo especificado
                Task.Delay(TimeSpan.FromSeconds(scanDurationSeconds)).ContinueWith(_ =>
                {
                    StopScan();
                });
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Error al iniciar escaneo: {ex.Message}", ex));
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Detiene el escaneo de dispositivos en curso.
        /// </summary>
        public void StopScan()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            try
            {
                Com.Inateck.Scanner.Ble.InateckBleListManager.StopScan();
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Error al detener escaneo: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Conecta a un dispositivo específico.
        /// </summary>
        /// <param name="device">Dispositivo al que conectar.</param>
        /// <returns>Task que retorna true si la conexión fue exitosa.</returns>
        public Task<bool> ConnectAsync(Com.Inateck.Scanner.Ble.InateckScannerDevice device)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            if (device == null)
                throw new ArgumentNullException(nameof(device));

            var tcs = new TaskCompletionSource<bool>();

            try
            {
                device.Connect(result =>
                {
                    if (result.IsSuccess)
                    {
                        _currentDevice = device;
                        OnDeviceConnected(new DeviceConnectedEventArgs
                        {
                            Device = device,
                            Timestamp = DateTime.Now
                        });
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        var exception = new Exception($"Error al conectar: {result.ExceptionOrNull()?.Message}");
                        OnError(new ErrorEventArgs("Error de conexión", exception));
                        tcs.TrySetResult(false);
                    }
                });
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Error al conectar: {ex.Message}", ex));
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Desconecta el dispositivo actual.
        /// </summary>
        /// <returns>Task que retorna true si la desconexión fue exitosa.</returns>
        public Task<bool> DisconnectAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            if (_currentDevice == null)
                return Task.FromResult(true);

            var tcs = new TaskCompletionSource<bool>();

            try
            {
                _currentDevice.Disconnect(result =>
                {
                    if (result.IsSuccess)
                    {
                        _currentDevice = null;
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        var exception = new Exception($"Error al desconectar: {result.ExceptionOrNull()?.Message}");
                        OnError(new ErrorEventArgs("Error de desconexión", exception));
                        tcs.TrySetResult(false);
                    }
                });
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Error al desconectar: {ex.Message}", ex));
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Obtiene la versión del firmware del dispositivo conectado.
        /// </summary>
        public Task<string?> GetVersionAsync()
        {
            return ExecuteMessengerCommand(
                messenger => messenger.GetVersion,
                "Error al obtener versión"
            );
        }

        /// <summary>
        /// Obtiene información de batería del dispositivo conectado.
        /// </summary>
        public Task<string?> GetBatteryInfoAsync()
        {
            return ExecuteMessengerCommand(
                messenger => messenger.GetBatteryInfo,
                "Error al obtener información de batería"
            );
        }

        /// <summary>
        /// Obtiene información de hardware del dispositivo conectado.
        /// </summary>
        public Task<string?> GetHardwareInfoAsync()
        {
            return ExecuteMessengerCommand(
                messenger => messenger.GetHardwareInfo,
                "Error al obtener información de hardware"
            );
        }

        /// <summary>
        /// Obtiene configuración del dispositivo conectado.
        /// </summary>
        public Task<string?> GetSettingsAsync()
        {
            return ExecuteMessengerCommand(
                messenger => messenger.GetSettingInfo,
                "Error al obtener configuración"
            );
        }

        /// <summary>
        /// Configura el volumen del dispositivo.
        /// </summary>
        /// <param name="volumeLevel">Nivel de volumen (0-4).</param>
        public Task<bool> SetVolumeAsync(int volumeLevel)
        {
            if (volumeLevel < 0 || volumeLevel > 4)
                throw new ArgumentOutOfRangeException(nameof(volumeLevel), "El volumen debe estar entre 0 y 4.");

            var settings = $"[{{\"area\":\"3\",\"value\":\"{volumeLevel}\",\"name\":\"volume\"}}]";
            return SetSettingsAsync(settings);
        }

        /// <summary>
        /// Aplica configuración al dispositivo conectado.
        /// </summary>
        /// <param name="settingsJson">Configuración en formato JSON.</param>
        public Task<bool> SetSettingsAsync(string settingsJson)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            if (_currentDevice == null || !IsConnected)
                throw new InvalidOperationException("No hay dispositivo conectado.");

            var tcs = new TaskCompletionSource<bool>();

            try
            {
                _currentDevice.Messager.SetSettingInfo(settingsJson, result =>
                {
                    tcs.TrySetResult(result.IsSuccess);
                    if (!result.IsSuccess)
                    {
                        OnError(new ErrorEventArgs(
                            "Error al aplicar configuración",
                            new Exception(result.ExceptionOrNull()?.Message ?? "Error desconocido")
                        ));
                    }
                });
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Error al aplicar configuración: {ex.Message}", ex));
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Configura el escáner para leer SOLO códigos DataMatrix.
        /// Desactiva todos los demás tipos de códigos (1D, QR, PDF417, etc.).
        /// </summary>
        public async Task<bool> ConfigureForDataMatrixOnlyAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            if (_currentDevice == null || !IsConnected)
                throw new InvalidOperationException("No hay dispositivo conectado.");

            try
            {
                // Configuración JSON para deshabilitar TODOS los códigos excepto DataMatrix
                var disableAllButDataMatrix = @"[
                    {""value"":""1"",""area"":""27"",""name"":""datamatrix_on""},
                    {""value"":""0"",""area"":""11"",""name"":""codabar_on""},
                    {""value"":""0"",""area"":""11"",""name"":""iata25_on""},
                    {""value"":""0"",""area"":""11"",""name"":""interleaved25_on""},
                    {""value"":""0"",""area"":""11"",""name"":""matrix25_on""},
                    {""value"":""0"",""area"":""11"",""name"":""standard25_on""},
                    {""value"":""0"",""area"":""11"",""name"":""code39_on""},
                    {""value"":""0"",""area"":""11"",""name"":""code93_on""},
                    {""value"":""0"",""area"":""11"",""name"":""code128_on""},
                    {""value"":""0"",""area"":""12"",""name"":""ean_8_on""},
                    {""value"":""0"",""area"":""12"",""name"":""ean_13_on""},
                    {""value"":""0"",""area"":""12"",""name"":""upc_a_on""},
                    {""value"":""0"",""area"":""12"",""name"":""upc_e0_on""},
                    {""value"":""0"",""area"":""12"",""name"":""upc_e1_on""},
                    {""value"":""0"",""area"":""12"",""name"":""msi_on""},
                    {""value"":""0"",""area"":""12"",""name"":""code11_on""},
                    {""value"":""0"",""area"":""12"",""name"":""chinese_post_on""},
                    {""value"":""0"",""area"":""15"",""name"":""usps_fedex""},
                    {""value"":""0"",""area"":""25"",""name"":""aztec_on""},
                    {""value"":""0"",""area"":""25"",""name"":""maxicode_on""},
                    {""value"":""0"",""area"":""26"",""name"":""hanxin_on""},
                    {""value"":""0"",""area"":""28"",""name"":""qrcode_on""},
                    {""value"":""0"",""area"":""29"",""name"":""pdf417_on""},
                    {""value"":""0"",""area"":""32"",""name"":""gs1_128""}
                ]";

                var success = await SetSettingsAsync(disableAllButDataMatrix);

                if (success)
                {
                    OnStatusChanged("Escáner configurado para leer SOLO DataMatrix");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"Error al configurar DataMatrix: {ex.Message}", ex));
                return false;
            }
        }

        private void OnStatusChanged(string message)
        {
            // Este método auxiliar permite enviar mensajes de estado desde el wrapper
            System.Diagnostics.Debug.WriteLine($"[InateckWrapper] {message}");
        }


        // =====================================================
        // MÉTODOS PRIVADOS
        // =====================================================

        private Task<string?> ExecuteMessengerCommand(
            Func<Com.Inateck.Scanner.Ble.BleMessenger, Action<Java.Lang.Object>> commandSelector,
            string errorMessage)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InateckScannerWrapper));

            if (_currentDevice == null || !IsConnected)
                throw new InvalidOperationException("No hay dispositivo conectado.");

            var tcs = new TaskCompletionSource<string?>();

            try
            {
                var command = commandSelector(_currentDevice.Messager);
                command(result =>
                {
                    // Nota: El tipo Result de Kotlin necesita ser parseado
                    // Esta implementación puede requerir ajustes según el binding generado
                    var resultStr = result?.ToString();
                    tcs.TrySetResult(resultStr);
                });
            }
            catch (Exception ex)
            {
                OnError(new ErrorEventArgs($"{errorMessage}: {ex.Message}", ex));
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }


        // =====================================================
        // DISPARADORES DE EVENTOS
        // =====================================================

        protected virtual void OnScanStarted(ScanStartedEventArgs e) => ScanStarted?.Invoke(this, e);
        protected virtual void OnDeviceDiscovered(DeviceDiscoveredEventArgs e) => DeviceDiscovered?.Invoke(this, e);
        protected virtual void OnScanCompleted(ScanCompletedEventArgs e) => ScanCompleted?.Invoke(this, e);
        protected virtual void OnDeviceConnected(DeviceConnectedEventArgs e) => DeviceConnected?.Invoke(this, e);
        protected virtual void OnDeviceDisconnected(DeviceDisconnectedEventArgs e) => DeviceDisconnected?.Invoke(this, e);
        protected virtual void OnDataReceived(DataReceivedEventArgs e) => DataReceived?.Invoke(this, e);
        protected virtual void OnError(ErrorEventArgs e) => ErrorOccurred?.Invoke(this, e);
        protected virtual void OnBatteryInfoReceived(BatteryInfoEventArgs e) => BatteryInfoReceived?.Invoke(this, e);


        // =====================================================
        // DISPOSE PATTERN
        // =====================================================

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Desconectar si hay dispositivo conectado
                if (_currentDevice != null && IsConnected)
                {
                    try
                    {
                        DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
                    }
                    catch
                    {
                        // Ignorar errores durante dispose
                    }
                }

                _currentDevice = null;
                _application = null;
            }

            _disposed = true;
        }


        // =====================================================
        // CLASE INTERNA: CALLBACK DE ESCANEO
        // =====================================================

        private class ScanCallback : Java.Lang.Object, Com.Inateck.Scanner.Ble.Callback.IBleScanResultCallback
        {
            private readonly Action<IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>?>? _onStarted;
            private readonly Action<Com.Inateck.Scanner.Ble.InateckScannerDevice>? _onScanning;
            private readonly Action<IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>?>? _onFinished;

            public ScanCallback(
                Action<IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>?>? onStarted,
                Action<Com.Inateck.Scanner.Ble.InateckScannerDevice>? onScanning,
                Action<IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>?>? onFinished)
            {
                _onStarted = onStarted;
                _onScanning = onScanning;
                _onFinished = onFinished;
            }

            public void OnScanStarted(IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>? scanResultList)
            {
                _onStarted?.Invoke(scanResultList);
            }

            public void OnScanning(Com.Inateck.Scanner.Ble.InateckScannerDevice device)
            {
                _onScanning?.Invoke(device);
            }

            public void OnScanFinished(IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>? scanResultList)
            {
                _onFinished?.Invoke(scanResultList);
            }
        }
    }


    // =====================================================
    // EVENT ARGS
    // =====================================================

    public class ScanStartedEventArgs : EventArgs
    {
        public IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>? Devices { get; set; }
    }

    public class DeviceDiscoveredEventArgs : EventArgs
    {
        public Com.Inateck.Scanner.Ble.InateckScannerDevice? Device { get; set; }
    }

    public class ScanCompletedEventArgs : EventArgs
    {
        public IList<Com.Inateck.Scanner.Ble.InateckScannerDevice>? Devices { get; set; }
    }

    public class DeviceConnectedEventArgs : EventArgs
    {
        public Com.Inateck.Scanner.Ble.InateckScannerDevice? Device { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DeviceDisconnectedEventArgs : EventArgs
    {
        public Com.Inateck.Scanner.Ble.InateckScannerDevice? Device { get; set; }
        public bool IsUserInitiated { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        public string? Data { get; set; }
        public byte[]? RawData { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }

        public ErrorEventArgs() { }

        public ErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }

    public class BatteryInfoEventArgs : EventArgs
    {
        public int BatteryLevel { get; set; }
        public string? RawInfo { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
