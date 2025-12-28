using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InateckMauiApp.Services
{
    /// <summary>
    /// Interfaz de abstracción para el servicio del escáner Inatek.
    /// Permite desacoplar la lógica de negocio de la implementación específica de Android.
    /// </summary>
    public interface IScannerService
    {
        // =====================================================
        // EVENTOS
        // =====================================================

        /// <summary>
        /// Se dispara cuando se encuentra un dispositivo durante el escaneo.
        /// </summary>
        event EventHandler<ScannerDeviceInfo> DeviceDiscovered;

        /// <summary>
        /// Se dispara cuando se reciben datos del escáner (código de barras leído).
        /// </summary>
        event EventHandler<string> DataReceived;

        /// <summary>
        /// Se dispara cuando cambia el estado de conexión.
        /// </summary>
        event EventHandler<string> StatusChanged;

        /// <summary>
        /// Se dispara cuando ocurre un error.
        /// </summary>
        event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// Se dispara cuando cambia el nivel de batería.
        /// </summary>
        event EventHandler<int> BatteryLevelChanged;


        // =====================================================
        // PROPIEDADES
        // =====================================================

        /// <summary>
        /// Indica si el servicio está inicializado.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Indica si hay un dispositivo conectado.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Nombre del dispositivo conectado (null si no hay conexión).
        /// </summary>
        string? ConnectedDeviceName { get; }

        /// <summary>
        /// Dirección MAC del dispositivo conectado (null si no hay conexión).
        /// </summary>
        string? ConnectedDeviceMac { get; }

        /// <summary>
        /// Lista de dispositivos descubiertos en el último escaneo.
        /// </summary>
        IReadOnlyList<ScannerDeviceInfo> DiscoveredDevices { get; }


        // =====================================================
        // MÉTODOS
        // =====================================================

        /// <summary>
        /// Inicializa el servicio del escáner.
        /// Debe llamarse antes de cualquier otra operación.
        /// </summary>
        /// <returns>Task que retorna true si la inicialización fue exitosa.</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Verifica y sincroniza el estado de Bluetooth y conexión BLE cuando la app vuelve del Stand by.
        /// Se usa en OnAppearing para validar que la conexión sigue activa o detectar si se perdió.
        /// </summary>
        /// <returns>Task que retorna true si el estado está sincronizado correctamente.</returns>
        Task<bool> VerifyAndSyncStateAsync();

        /// <summary>
        /// Inicia el escaneo de dispositivos BLE.
        /// </summary>
        /// <param name="durationSeconds">Duración del escaneo en segundos.</param>
        /// <returns>Task que retorna la lista de dispositivos encontrados.</returns>
        Task<List<ScannerDeviceInfo>> ScanForDevicesAsync(int durationSeconds = 10);

        /// <summary>
        /// Detiene el escaneo en curso.
        /// </summary>
        void StopScan();

        /// <summary>
        /// Conecta al dispositivo especificado por su dirección MAC.
        /// </summary>
        /// <param name="deviceMac">Dirección MAC del dispositivo.</param>
        /// <returns>Task que retorna true si la conexión fue exitosa.</returns>
        Task<bool> ConnectAsync(string deviceMac);

        /// <summary>
        /// Desconecta el dispositivo actual.
        /// </summary>
        /// <returns>Task que retorna true si la desconexión fue exitosa.</returns>
        Task<bool> DisconnectAsync();

        /// <summary>
        /// Obtiene la versión del firmware del dispositivo conectado.
        /// </summary>
        /// <returns>Versión del firmware o null si falla.</returns>
        Task<string?> GetDeviceVersionAsync();

        /// <summary>
        /// Obtiene el nivel de batería del dispositivo conectado.
        /// </summary>
        /// <returns>Información de batería como string o null si falla.</returns>
        Task<string?> GetBatteryInfoAsync();

        /// <summary>
        /// Configura el volumen del dispositivo.
        /// </summary>
        /// <param name="level">Nivel de volumen (0-4).</param>
        /// <returns>Task que retorna true si la configuración fue exitosa.</returns>
        Task<bool> SetVolumeAsync(int level);

        /// <summary>
        /// Configura el escáner para leer SOLO códigos DataMatrix.
        /// Desactiva todos los demás tipos de códigos (QR, PDF417, códigos 1D, etc.).
        /// </summary>
        /// <returns>Task que retorna true si la configuración fue exitosa.</returns>
        Task<bool> ConfigureForDataMatrixOnlyAsync();

        /// <summary>
        /// Escanea un código de barras/DataMatrix.
        /// </summary>
        /// <returns>El código escaneado o null si falla.</returns>
        Task<string?> ScanBarcodeAsync();
    }


    /// <summary>
    /// Información de un dispositivo descubierto.
    /// </summary>
    public class ScannerDeviceInfo
    {
        /// <summary>
        /// Nombre del dispositivo.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dirección MAC del dispositivo.
        /// </summary>
        public string Mac { get; set; } = string.Empty;

        /// <summary>
        /// Estado de conexión del dispositivo.
        /// </summary>
        public string ConnectionState { get; set; } = "Disconnected";

        /// <summary>
        /// Nivel de señal RSSI (opcional).
        /// </summary>
        public int? Rssi { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Mac}) - {ConnectionState}";
        }
    }}