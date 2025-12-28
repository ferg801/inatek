using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;
using InateckMauiApp.Services;
using Java.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.Inateck.Scanner.Ble;
using Com.Inateck.Scanner.Ble.Callback;
using Kotlin.Jvm.Functions;

namespace InateckMauiApp.Platforms.Android
{
    /// <summary>
    /// Implementación real de IScannerService usando Bluetooth API de Android.
    /// Conecta a dispositivos Inatek via Bluetooth clásico.
    /// </summary>
    public class AndroidScannerService : IScannerService
    {
        public event EventHandler<ScannerDeviceInfo>? DeviceDiscovered;
        public event EventHandler<string>? DataReceived;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<int>? BatteryLevelChanged;

        public bool IsInitialized { get; private set; } = false;
        public bool IsConnected => (_bluetoothSocket?.IsConnected ?? false) || (_bluetoothGatt != null);
        public string? ConnectedDeviceName => _connectedDevice?.Name;
        public string? ConnectedDeviceMac => _connectedDevice?.Address;
        public IReadOnlyList<ScannerDeviceInfo> DiscoveredDevices => _discoveredDevices.AsReadOnly();

        private BluetoothAdapter? _bluetoothAdapter;
        private BluetoothSocket? _bluetoothSocket;
        private BluetoothGatt? _bluetoothGatt;
        private BluetoothGattCallback? _gattCallback;
        private BluetoothDevice? _connectedDevice;
        private List<ScannerDeviceInfo> _discoveredDevices = new();
        private BroadcastReceiver? _discoveryReceiver;
        private Context? _context;
        private bool _isScanning = false;
        private TaskCompletionSource<bool>? _gattConnectedTcs;
        private TaskCompletionSource<bool>? _servicesDiscoveredTcs;
        private string _lastUILog = "";
        private string? _currentConnectedMac = null;  // NEW: Store MAC for Inateck SDK lookup
        private BleScannerDevice? _currentInateckDevice = null;

        public AndroidScannerService(Context context)
        {
            _context = context;
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            
            if (_bluetoothAdapter != null)
            {
                IsInitialized = true;
                LogToUI("[Bluetooth] Adapter available");
                StatusChanged?.Invoke(this, "✓ Bluetooth adapter available");
            }
            else
            {
                LogToUI("[Bluetooth] ERROR: No adapter");
                StatusChanged?.Invoke(this, "✗ Bluetooth not available");
                ErrorOccurred?.Invoke(this, "Device doesn't support Bluetooth");
            }
        }

        /// <summary>
        /// Log técnico para debugging - solo va a logcat, no muestra Toast
        /// </summary>
        internal void LogDebug(string message)
        {
            Log.Debug("InateckScanner", message);
        }

        /// <summary>
        /// Mensaje importante para el usuario - muestra Toast
        /// </summary>
        internal void ShowUserMessage(string message)
        {
            _lastUILog = message;
            Log.Info("InateckScanner", $"[USER] {message}");
            
            try
            {
                if (_context != null && MainThread.IsMainThread)
                {
                    Toast.MakeText(_context, message, ToastLength.Short)?.Show();
                }
                else if (_context != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            Toast.MakeText(_context, message, ToastLength.Short)?.Show();
                        }
                        catch { }
                    });
                }
            }
            catch { }
            
            StatusChanged?.Invoke(this, message);
        }

        /// <summary>
        /// LogToUI ahora solo hace log, sin Toast (para compatibilidad)
        /// </summary>
        internal void LogToUI(string message)
        {
            _lastUILog = message;
            Log.Debug("InateckScanner", $"[LogToUI] {message}");
            // Ya no muestra Toast - usar ShowUserMessage para mensajes importantes
        }

        public Task<bool> InitializeAsync()
        {
            if (_bluetoothAdapter == null)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Adapter is null");
                ErrorOccurred?.Invoke(this, "Bluetooth adapter not found");
                return Task.FromResult(false);
            }

            if (!_bluetoothAdapter.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Bluetooth is disabled");
                StatusChanged?.Invoke(this, "⚠ Bluetooth is disabled. Enable in Settings.");
                return Task.FromResult(false);
            }

            IsInitialized = true;
            System.Diagnostics.Debug.WriteLine("[Bluetooth] Initialized successfully");
            StatusChanged?.Invoke(this, "✓ Bluetooth initialized");
            return Task.FromResult(true);
        }

        /// <summary>
        /// Verifica y sincroniza el estado de Bluetooth y conexión BLE cuando la app vuelve al frente.
        /// Se usa al regresar de Stand by o pausa.
        /// </summary>
        public Task<bool> VerifyAndSyncStateAsync()
        {
            System.Diagnostics.Debug.WriteLine("[Bluetooth] VerifyAndSyncStateAsync - Checking state on resume");

            // Verificar que Bluetooth está disponible
            if (_bluetoothAdapter == null)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Adapter is null on resume");
                IsInitialized = false;
                StatusChanged?.Invoke(this, "⚠ Bluetooth adapter not available");
                return Task.FromResult(false);
            }

            // Verificar que Bluetooth está habilitado
            if (!_bluetoothAdapter.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Bluetooth is disabled on resume");
                IsInitialized = false;
                StatusChanged?.Invoke(this, "⚠ Bluetooth is disabled. Enable in Settings.");
                return Task.FromResult(false);
            }

            // Verificar si la conexión BLE se perdió
            if (IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] Connection still active after resume");
                StatusChanged?.Invoke(this, "✓ Connection restored");
                return Task.FromResult(true);
            }
            else if (_connectedDevice != null)
            {
                // La conexión se perdió pero el dispositivo estaba conectado
                System.Diagnostics.Debug.WriteLine("[Bluetooth] Connection lost during pause - clearing state");
                _connectedDevice = null;
                _bluetoothGatt = null;
                StatusChanged?.Invoke(this, "⚠ Connection was lost during pause");
                return Task.FromResult(false);
            }
            else
            {
                // No había conexión, simplemente re-inicializar
                System.Diagnostics.Debug.WriteLine("[Bluetooth] No connection on resume - state is clean");
                IsInitialized = true;
                StatusChanged?.Invoke(this, "✓ Bluetooth ready");
                return Task.FromResult(true);
            }
        }

        public async Task<List<ScannerDeviceInfo>> ScanForDevicesAsync(int durationSeconds = 10)
        {
            if (_bluetoothAdapter == null)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Adapter is null during scan");
                ErrorOccurred?.Invoke(this, "Bluetooth adapter not available");
                return new List<ScannerDeviceInfo>();
            }

            if (!_bluetoothAdapter.IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Bluetooth disabled during scan");
                ErrorOccurred?.Invoke(this, "Bluetooth is disabled");
                return new List<ScannerDeviceInfo>();
            }

            _discoveredDevices.Clear();
            _isScanning = true;
            System.Diagnostics.Debug.WriteLine($"[Bluetooth] Starting discovery for {durationSeconds}s");
            StatusChanged?.Invoke(this, $"🔍 Scanning for {durationSeconds} seconds...");

            try
            {
                // First, add already bonded (paired) devices - FILTER ONLY INATEK (BCST)
                var bondedDevices = _bluetoothAdapter.BondedDevices;
                if (bondedDevices != null && bondedDevices.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Bluetooth] Found {bondedDevices.Count} bonded devices");
                    foreach (var device in bondedDevices)
                    {
                        if (device != null && !string.IsNullOrEmpty(device.Name) && !string.IsNullOrEmpty(device.Address))
                        {
                            // FILTER: Only show Inatek devices (name contains BCST)
                            if (IsInatekDevice(device.Name))
                            {
                                var deviceInfo = new ScannerDeviceInfo 
                                { 
                                    Name = $"{device.Name} (bonded)", 
                                    Mac = device.Address, 
                                    Rssi = -1 // Not available for bonded devices
                                };
                                _discoveredDevices.Add(deviceInfo);
                                System.Diagnostics.Debug.WriteLine($"[Bluetooth] Bonded Inatek device: {device.Name} ({device.Address})");
                                DeviceDiscovered?.Invoke(this, deviceInfo);
                            }
                        }
                    }
                }

                // Register receiver for discovery
                _discoveryReceiver = new BluetoothDiscoveryReceiver(this);
                var intentFilter = new IntentFilter(BluetoothDevice.ActionFound);
                intentFilter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
                _context?.RegisterReceiver(_discoveryReceiver, intentFilter);

                // Start discovery
                _bluetoothAdapter.StartDiscovery();
                System.Diagnostics.Debug.WriteLine("[Bluetooth] Discovery started");

                // Wait for scan duration
                await Task.Delay(durationSeconds * 1000);

                // Stop discovery
                _bluetoothAdapter.CancelDiscovery();
                _isScanning = false;

                if (_discoveryReceiver != null)
                {
                    try
                    {
                        _context?.UnregisterReceiver(_discoveryReceiver);
                    }
                    catch { }
                }

                System.Diagnostics.Debug.WriteLine($"[Bluetooth] Discovery completed. Found {_discoveredDevices.Count} devices");
                StatusChanged?.Invoke(this, $"✓ Scan completed. Found {_discoveredDevices.Count} devices");
                return _discoveredDevices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bluetooth] ERROR during scan: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"Scan error: {ex.Message}");
                return new List<ScannerDeviceInfo>();
            }
        }

        public void StopScan()
        {
            if (_bluetoothAdapter != null && _isScanning)
            {
                _bluetoothAdapter.CancelDiscovery();
                _isScanning = false;
            }

            if (_discoveryReceiver != null)
            {
                try
                {
                    _context?.UnregisterReceiver(_discoveryReceiver);
                }
                catch { }
            }

            StatusChanged?.Invoke(this, "Scan stopped");
        }

        public async Task<bool> ConnectAsync(string deviceMac)
        {
            if (_bluetoothAdapter == null)
            {
                System.Diagnostics.Debug.WriteLine("[BT] ERROR: Adapter is null");
                ErrorOccurred?.Invoke(this, "Bluetooth adapter not available");
                return false;
            }

            try
            {
                LogToUI($"[CONNECT] Starting connection attempt to {deviceMac}");
                System.Diagnostics.Debug.WriteLine($"[BT] Attempting connection to {deviceMac}");
                StatusChanged?.Invoke(this, $"🔗 Connecting to {deviceMac}...");

                if (_bluetoothAdapter.IsDiscovering)
                {
                    _bluetoothAdapter.CancelDiscovery();
                    await Task.Delay(500);
                }

                _connectedDevice = _bluetoothAdapter.GetRemoteDevice(deviceMac);
                if (_connectedDevice == null)
                {
                    LogToUI($"[ERROR] Device not found: {deviceMac}");
                    System.Diagnostics.Debug.WriteLine($"[BT] Device not found: {deviceMac}");
                    ErrorOccurred?.Invoke(this, "Device not found");
                    return false;
                }

                LogToUI($"[CONNECT] Device found: {_connectedDevice.Name}");
                System.Diagnostics.Debug.WriteLine($"[BT] Device found: {_connectedDevice.Name}");

                // Try BLE (Gatt) connection first - Inatek scanners use BLE
                LogToUI("[BLE] Attempting Gatt connection (BLE)...");
                System.Diagnostics.Debug.WriteLine("[BLE] Attempting Gatt connection (BLE)...");
                if (await TryConnectBleAsync(deviceMac))
                {
                    return true;
                }

                // Fall back to classic Bluetooth (RFCOMM)
                LogToUI("[BT] BLE failed, trying Classic Bluetooth (RFCOMM)...");
                System.Diagnostics.Debug.WriteLine("[BT] BLE failed, trying Classic Bluetooth (RFCOMM)...");
                return await TryConnectClassicAsync(deviceMac);
            }
            catch (Exception ex)
            {
                LogToUI($"[ERROR] {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BT] Connection error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BT] StackTrace: {ex.StackTrace}");
                ShowUserMessage("Connection failed");
                ErrorOccurred?.Invoke(this, $"Connection failed: {ex.Message}");
                _bluetoothSocket = null;
                return false;
            }
        }

        private async Task<bool> TryConnectBleAsync(string deviceMac)
        {
            try
            {
                LogToUI("[BLE-1] TryConnectBleAsync START");
                
                if (_connectedDevice == null)
                {
                    LogToUI("[BLE-1] Device is null - returning false");
                    return false;
                }

                LogToUI("[BLE] Creating BleGattCallback...");
                _gattCallback = new BleGattCallback(this);
                LogToUI("[BLE-2] Callback created successfully");
                
                // Create TaskCompletionSource BEFORE calling connectGatt to avoid race condition
                _gattConnectedTcs = new TaskCompletionSource<bool>();
                LogToUI("[BLE-5] TaskCompletionSource created BEFORE connectGatt");
                
                LogToUI("[BLE] Calling connectGatt...");
                try
                {
                    _bluetoothGatt = _connectedDevice.ConnectGatt(_context, false, _gattCallback);
                    LogToUI("[BLE-3] ConnectGatt returned (not null check)");
                }
                catch (Exception exGatt)
                {
                    LogToUI($"[BLE-ERROR-GATT] ConnectGatt threw: {exGatt.Message}");
                    return false;
                }

                if (_bluetoothGatt == null)
                {
                    LogToUI("[BLE] ConnectGatt returned null - FAILED");
                    return false;
                }

                LogToUI("[BLE-4] BluetoothGatt object is NOT null");
                LogToUI("[BLE] Gatt connection initiated, waiting for callback...");

                var connectTask = _gattConnectedTcs.Task;
                var timeoutTask = Task.Delay(10000);
                
                LogToUI("[BLE] ⏱️ Waiting 10 seconds for callback...");
                var completed = await Task.WhenAny(connectTask, timeoutTask);
                
                LogToUI("[BLE-7] Task.WhenAny completed");

                if (completed == connectTask && connectTask.IsCompletedSuccessfully)
                {
                    LogToUI("[BLE] ✓ GATT connected successfully!");
                    ShowUserMessage($"✓ Connected: {_connectedDevice.Name}");
                    StatusChanged?.Invoke(this, $"✓ Connected: {_connectedDevice.Name}");
                    return true;
                }
                else if (completed == connectTask)
                {
                    LogToUI("[BLE] GATT connection task failed with exception");
                    try
                    {
                        var result = await connectTask;
                    }
                    catch (Exception ex)
                    {
                        LogToUI($"[BLE] Task exception: {ex.Message}");
                    }
                    if (_bluetoothGatt != null)
                    {
                        _bluetoothGatt.Disconnect();
                        _bluetoothGatt.Close();
                        _bluetoothGatt = null;
                    }
                    return false;
                }
                else
                {
                    LogToUI("[BLE] ❌ GATT TIMEOUT - Callback never invoked");
                    ShowUserMessage("Connection timeout");
                    if (_bluetoothGatt != null)
                    {
                        try
                        {
                            _bluetoothGatt.Disconnect();
                        }
                        catch { }
                        try
                        {
                            _bluetoothGatt.Close();
                        }
                        catch { }
                        _bluetoothGatt = null;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogToUI($"[BLE-CATCH] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BLE] Exception details: {ex}");
                if (_bluetoothGatt != null)
                {
                    try
                    {
                        _bluetoothGatt.Close();
                    }
                    catch { }
                    _bluetoothGatt = null;
                }
                return false;
            }
        }

        private async Task<bool> TryConnectClassicAsync(string deviceMac)
        {
            try
            {
                if (_connectedDevice == null)
                    return false;

                // Close previous socket if exists
                if (_bluetoothSocket != null)
                {
                    try 
                    { 
                        _bluetoothSocket.Close();
                        _bluetoothSocket.Dispose();
                    }
                    catch { }
                    _bluetoothSocket = null;
                }

                // Create RFCOMM socket for Serial Port Profile (SPP)
                // UUID for SPP - standard for serial communication over Bluetooth
                const string spUuid = "00001101-0000-1000-8000-00805f9b34fb";
                
                System.Diagnostics.Debug.WriteLine("[BT] Creating RFCOMM socket...");
                _bluetoothSocket = _connectedDevice.CreateRfcommSocketToServiceRecord(
                    UUID.FromString(spUuid));

                if (_bluetoothSocket == null)
                {
                    System.Diagnostics.Debug.WriteLine("[BT] Failed to create RFCOMM socket");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("[BT] Attempting socket connection...");
                _bluetoothSocket.Connect();
                
                System.Diagnostics.Debug.WriteLine($"[BT] ✓ Connected: {_connectedDevice.Name}");
                StatusChanged?.Invoke(this, $"✓ Connected: {_connectedDevice.Name}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BT] Classic connection error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BT] StackTrace: {ex.StackTrace}");
                _bluetoothSocket = null;
                return false;
            }
        }

        internal void OnGattConnected()
        {
            System.Diagnostics.Debug.WriteLine("[BLE] GATT Connection state changed to CONNECTED");
            Log.Info("InateckScanner", "[BLE] OnGattConnected() - Starting service discovery");
            
            // Automatically discover services after connection
            if (_bluetoothGatt != null)
            {
                _bluetoothGatt.DiscoverServices();
                Log.Info("InateckScanner", "[BLE] DiscoverServices() called");
            }
            
            _gattConnectedTcs?.TrySetResult(true);
        }

        internal void OnGattDisconnected()
        {
            Log.Info("InateckScanner", "[BLE] GATT Connection state changed to DISCONNECTED");
            _bluetoothGatt = null;
            StatusChanged?.Invoke(this, "✗ Disconnected");
        }

        internal void OnServicesDiscovered()
        {
            Log.Info("InateckScanner", "[BLE] Services discovered callback received");
            
            // Enable notifications on RX characteristic
            try
            {
                EnableGattNotifications();
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[BLE] Error enabling notifications: {ex.Message}");
            }
            
            _servicesDiscoveredTcs?.TrySetResult(true);
        }

        private void EnableGattNotifications()
        {
            Log.Info("InateckScanner", "[BLE] *** ENABLE GATT NOTIFICATIONS CALLED ***");
            LogToUI("[BLE] *** ENABLE GATT NOTIFICATIONS CALLED ***");
            
            if (_bluetoothGatt == null)
            {
                Log.Error("InateckScanner", "[BLE] Cannot enable notifications - GATT is null");
                LogToUI("[BLE] ERROR: _bluetoothGatt is NULL!");
                return;
            }

            Log.Info("InateckScanner", "[BLE] Getting service 0000ffe0...");
            LogToUI("[BLE] Getting service 0000ffe0...");
            
            // Get the service (0000ffe0-0000-1000-8000-00805f9b34fb)
            var service = _bluetoothGatt.GetService(UUID.FromString("0000ffe0-0000-1000-8000-00805f9b34fb"));
            if (service == null)
            {
                Log.Error("InateckScanner", "[BLE] Service 0000ffe0 not found for notifications");
                LogToUI("[BLE] ERROR: Service 0000ffe0 NOT found!");
                return;
            }

            Log.Info("InateckScanner", "[BLE] Service found!");
            LogToUI("[BLE] Service found!");

            // Try to enable notifications on all characteristics
            Log.Info("InateckScanner", "[BLE] Attempting to enable notifications on all characteristics...");
            
            // List of characteristics to try
            string[] charUuids = new string[]
            {
                "0000ffe1-0000-1000-8000-00805f9b34fb",  // RX
                "0000ffe0-0000-1000-8000-00805f9b34fb",  // Also try ffe0
                "0000ffe2-0000-1000-8000-00805f9b34fb",  // Try ffe2
            };

            int notifyCount = 0;
            foreach (var charUuid in charUuids)
            {
                var candidate = service.GetCharacteristic(UUID.FromString(charUuid));
                if (candidate == null)
                {
                    Log.Info("InateckScanner", $"[BLE] Characteristic {charUuid.Substring(4, 4)} not found");
                    continue;
                }

                int props = (int)candidate.Properties;
                bool hasNotify = (props & 0x10) != 0;  // PROPERTY_NOTIFY
                bool hasIndicate = (props & 0x20) != 0;  // PROPERTY_INDICATE

                Log.Info("InateckScanner", $"[BLE] Char {charUuid.Substring(4, 4)} - Notify: {hasNotify}, Indicate: {hasIndicate}");

                if (hasNotify || hasIndicate)
                {
                    bool notifEnabled = _bluetoothGatt.SetCharacteristicNotification(candidate, true);
                    Log.Info("InateckScanner", $"[BLE] SetCharacteristicNotification({charUuid.Substring(4, 4)}): {notifEnabled}");
                    
                    if (notifEnabled)
                    {
                        // Get CCCD descriptor
                        var descriptor = candidate.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb"));
                        if (descriptor != null)
                        {
                            descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue!.ToArray());
                            bool descriptorWritten = _bluetoothGatt.WriteDescriptor(descriptor);
                            Log.Info("InateckScanner", $"[BLE] WriteDescriptor({charUuid.Substring(4, 4)}): {descriptorWritten}");
                            
                            if (descriptorWritten)
                                notifyCount++;
                        }
                    }
                }
            }

            Log.Info("InateckScanner", $"[BLE] *** NOTIFICATIONS ENABLED ON {notifyCount} CHARACTERISTICS ***");
            LogToUI($"[BLE] Notifications enabled on {notifyCount} characteristic(s)");
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                Log.Info("InateckScanner", "[BLE] Disconnecting");
                LogToUI("[BLE] Disconnecting...");
                
                // Save reference BEFORE Disconnect because callback may set it to null
                var gatt = _bluetoothGatt;
                Log.Info("InateckScanner", $"[BLE] gatt reference saved, is null: {gatt == null}");
                
                if (gatt != null)
                {
                    Log.Info("InateckScanner", "[BLE] gatt is not null, attempting Disconnect");
                    
                    try
                    {
                        Log.Info("InateckScanner", "[BLE] Calling gatt.Disconnect()");
                        gatt.Disconnect();
                        Log.Info("InateckScanner", "[BLE] gatt.Disconnect() succeeded");
                        
                        Log.Info("InateckScanner", "[BLE] Waiting 500ms for callback");
                        await Task.Delay(500);
                        Log.Info("InateckScanner", "[BLE] Wait complete");
                    }
                    catch (Exception exDisconnect)
                    {
                        Log.Warn("InateckScanner", $"[BLE] Disconnect() exception: {exDisconnect.Message}");
                    }
                    
                    try
                    {
                        Log.Info("InateckScanner", "[BLE] Calling gatt.Close()");
                        gatt.Close();
                        Log.Info("InateckScanner", "[BLE] gatt.Close() succeeded");
                    }
                    catch (Exception exClose)
                    {
                        Log.Warn("InateckScanner", $"[BLE] Close() exception: {exClose.Message}");
                    }
                    
                    try
                    {
                        Log.Info("InateckScanner", "[BLE] Calling gatt.Dispose()");
                        gatt.Dispose();
                        Log.Info("InateckScanner", "[BLE] gatt.Dispose() succeeded");
                    }
                    catch (Exception exDispose)
                    {
                        Log.Warn("InateckScanner", $"[BLE] Dispose() exception: {exDispose.Message}");
                    }
                }
                else
                {
                    Log.Info("InateckScanner", "[BLE] gatt is null, skipping cleanup");
                }
                
                Log.Info("InateckScanner", "[BLE] Clearing _bluetoothGatt and _connectedDevice");
                _bluetoothGatt = null;
                _connectedDevice = null;
                
                Log.Info("InateckScanner", "[BLE] ✓ Disconnected successfully");
                LogToUI("✓ Disconnected");
                ShowUserMessage("Disconnected");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[BLE] ERROR during disconnect: {ex.Message}\nStackTrace: {ex.StackTrace}");
                LogToUI($"✗ Disconnect error: {ex.Message}");
                ShowUserMessage("Disconnect failed");
                return false;
            }
        }

        /// <summary>
        /// Set scanner to GATT mode (instead of HID keyboard mode)
        /// This allows the app to receive scanned data via BLE notifications
        /// </summary>
        public async Task<string?> SetGattModeAsync()
        {
            try
            {
                Log.Info("InateckScanner", "[CONFIG] SetGattModeAsync: START");
                LogToUI("[CONFIG] Setting GATT mode...");

                if (_bluetoothGatt == null || !IsConnected)
                {
                    return "Device not connected";
                }

                // According to Inateck SDK documentation:
                // GATT Mode requires: bt_mode_low=0, bt_mode_high=1
                // JSON commands: { "value":"0","area":"1","name":"bt_mode_low" } and { "value":"1","area":"31","name":"bt_mode_high" }
                
                string[] commands = new string[]
                {
                    "{\"value\":\"0\",\"area\":\"1\",\"name\":\"bt_mode_low\"}\r",
                    "{\"value\":\"1\",\"area\":\"31\",\"name\":\"bt_mode_high\"}\r"
                };

                int successCount = 0;
                int timeoutCount = 0;
                
                foreach (var cmd in commands)
                {
                    Log.Info("InateckScanner", $"[CONFIG] Sending: {cmd.Trim()}");
                    var response = await SendGattCommandAsync(cmd, 2000);
                    Log.Info("InateckScanner", $"[CONFIG] Response: {response ?? "NULL"}");
                    
                    if (response == "Timeout" || response == null)
                    {
                        timeoutCount++;
                    }
                    else
                    {
                        successCount++;
                    }
                    await Task.Delay(500);
                }

                // Be honest about the result
                if (timeoutCount == commands.Length)
                {
                    Log.Info("InateckScanner", "[CONFIG] All commands timed out - scanner may not support GATT mode");
                    LogToUI("[CONFIG] ⚠ Scanner did not respond. May only support HID mode.");
                    return "Scanner did not respond to GATT mode commands. This scanner model may only support HID (keyboard) mode.";
                }
                else if (timeoutCount > 0)
                {
                    Log.Info("InateckScanner", "[CONFIG] Some commands timed out");
                    LogToUI("[CONFIG] ⚠ Partial response. Try reconnecting.");
                    return "Partial response. Please disconnect and reconnect the scanner.";
                }
                else
                {
                    Log.Info("InateckScanner", "[CONFIG] GATT mode commands sent successfully.");
                    LogToUI("[CONFIG] ✓ GATT mode set. Reconnect the scanner!");
                    return "GATT mode set. Please disconnect and reconnect the scanner.";
                }
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[CONFIG] Exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string?> GetDeviceVersionAsync()
        {
            try
            {
                Log.Info("InateckScanner", "[VERSION] GetDeviceVersionAsync: START");
                StatusChanged?.Invoke(this, "Requesting version...");
                
                // Check if BLE connected
                if (_bluetoothGatt != null && IsConnected)
                {
                    Log.Info("InateckScanner", "[VERSION] Using BLE to get version");
                    return await GetVersionViaBleAsync();
                }

                // Check if classic Bluetooth connected
                if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
                {
                    Log.Info("InateckScanner", "[VERSION] Using Classic Bluetooth");
                    var response = await SendCommandWithResponseAsync("VERSION\r", 5000);
                    return !string.IsNullOrEmpty(response) ? response : "Unknown";
                }

                Log.Error("InateckScanner", "[VERSION] ERROR: Not connected");
                return "Device not connected";
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[VERSION] ERROR: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Get device version via BLE Device Information Service (0x180A)
        /// </summary>
        private async Task<string?> GetVersionViaBleAsync()
        {
            try
            {
                Log.Info("InateckScanner", "[VERSION-BLE] Starting version request...");
                LogToUI("[VERSION] Requesting via BLE...");

                // Device Information Service UUID
                var deviceInfoService = _bluetoothGatt?.GetService(UUID.FromString("0000180a-0000-1000-8000-00805f9b34fb"));
                
                if (deviceInfoService != null)
                {
                    Log.Info("InateckScanner", "[VERSION-BLE] ✓ Device Information Service found!");
                    LogToUI("[VERSION] Device Info Service found");

                    var result = new System.Text.StringBuilder();

                    // Firmware Revision (0x2A26)
                    var firmwareChar = deviceInfoService.GetCharacteristic(UUID.FromString("00002a26-0000-1000-8000-00805f9b34fb"));
                    if (firmwareChar != null)
                    {
                        bool readResult = _bluetoothGatt!.ReadCharacteristic(firmwareChar);
                        if (readResult)
                        {
                            await Task.Delay(500);
                            var value = firmwareChar.GetValue();
                            if (value != null && value.Length > 0)
                            {
                                string firmware = Encoding.ASCII.GetString(value).Trim('\0');
                                Log.Info("InateckScanner", $"[VERSION-BLE] Firmware: {firmware}");
                                result.Append($"FW: {firmware}");
                            }
                        }
                    }

                    // Software Revision (0x2A28)
                    var softwareChar = deviceInfoService.GetCharacteristic(UUID.FromString("00002a28-0000-1000-8000-00805f9b34fb"));
                    if (softwareChar != null)
                    {
                        bool readResult = _bluetoothGatt!.ReadCharacteristic(softwareChar);
                        if (readResult)
                        {
                            await Task.Delay(500);
                            var value = softwareChar.GetValue();
                            if (value != null && value.Length > 0)
                            {
                                string software = Encoding.ASCII.GetString(value).Trim('\0');
                                Log.Info("InateckScanner", $"[VERSION-BLE] Software: {software}");
                                if (result.Length > 0) result.Append(" | ");
                                result.Append($"SW: {software}");
                            }
                        }
                    }

                    // Hardware Revision (0x2A27)
                    var hardwareChar = deviceInfoService.GetCharacteristic(UUID.FromString("00002a27-0000-1000-8000-00805f9b34fb"));
                    if (hardwareChar != null)
                    {
                        bool readResult = _bluetoothGatt!.ReadCharacteristic(hardwareChar);
                        if (readResult)
                        {
                            await Task.Delay(500);
                            var value = hardwareChar.GetValue();
                            if (value != null && value.Length > 0)
                            {
                                string hardware = Encoding.ASCII.GetString(value).Trim('\0');
                                Log.Info("InateckScanner", $"[VERSION-BLE] Hardware: {hardware}");
                                if (result.Length > 0) result.Append(" | ");
                                result.Append($"HW: {hardware}");
                            }
                        }
                    }

                    // Model Number (0x2A24)
                    var modelChar = deviceInfoService.GetCharacteristic(UUID.FromString("00002a24-0000-1000-8000-00805f9b34fb"));
                    if (modelChar != null)
                    {
                        bool readResult = _bluetoothGatt!.ReadCharacteristic(modelChar);
                        if (readResult)
                        {
                            await Task.Delay(500);
                            var value = modelChar.GetValue();
                            if (value != null && value.Length > 0)
                            {
                                string model = Encoding.ASCII.GetString(value).Trim('\0');
                                Log.Info("InateckScanner", $"[VERSION-BLE] Model: {model}");
                                if (result.Length > 0) result.Append(" | ");
                                result.Append($"Model: {model}");
                            }
                        }
                    }

                    if (result.Length > 0)
                    {
                        string versionInfo = result.ToString();
                        Log.Info("InateckScanner", $"[VERSION-BLE] ✓ Version info: {versionInfo}");
                        LogToUI($"📱 {versionInfo}");
                        StatusChanged?.Invoke(this, $"📱 {versionInfo}");
                        return versionInfo;
                    }
                }
                else
                {
                    Log.Info("InateckScanner", "[VERSION-BLE] Device Information Service (0x180A) not found");
                }

                // If no Device Info Service, try to use device name
                string deviceName = ConnectedDeviceName ?? "Unknown";
                Log.Info("InateckScanner", $"[VERSION-BLE] Using device name as version: {deviceName}");
                return $"Device: {deviceName}";
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[VERSION-BLE] Exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string?> GetBatteryInfoAsync()
        {
            try
            {
                Log.Info("InateckScanner", "[BATTERY] GetBatteryInfoAsync: START");
                StatusChanged?.Invoke(this, "Requesting battery...");
                
                // Check if BLE connected
                if (_bluetoothGatt != null && IsConnected)
                {
                    Log.Info("InateckScanner", "[BATTERY] Using BLE to get battery");
                    return await GetBatteryViaBleAsync();
                }

                // Check if classic Bluetooth connected
                if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
                {
                    Log.Info("InateckScanner", "[BATTERY] Using Classic Bluetooth to get battery");
                    return await GetBatteryViaClassicAsync();
                }

                Log.Error("InateckScanner", "[BATTERY] ERROR: Not connected");
                ErrorOccurred?.Invoke(this, "Not connected to device");
                return "Device not connected";
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[BATTERY] ERROR: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Get battery via BLE - First try standard Battery Service, then fall back to GATT commands
        /// </summary>
        private async Task<string?> GetBatteryViaBleAsync()
        {
            try
            {
                Log.Info("InateckScanner", "[BATTERY-BLE] Starting battery request...");
                LogToUI("[BATTERY] Requesting via BLE...");

                // Discover services if not already done
                if (_bluetoothGatt?.Services?.Count == 0)
                {
                    Log.Info("InateckScanner", "[BATTERY-BLE] Discovering services...");
                    _servicesDiscoveredTcs = new TaskCompletionSource<bool>();
                    _bluetoothGatt.DiscoverServices();
                    
                    var discoverTask = _servicesDiscoveredTcs.Task;
                    var timeoutTask = Task.Delay(5000);
                    await Task.WhenAny(discoverTask, timeoutTask);
                }

                // List all available services for debugging
                Log.Info("InateckScanner", "[BATTERY-BLE] Listing all services...");
                if (_bluetoothGatt?.Services != null)
                {
                    foreach (var svc in _bluetoothGatt.Services)
                    {
                        Log.Info("InateckScanner", $"[BATTERY-BLE] Service: {svc.Uuid}");
                        foreach (var chr in svc.Characteristics ?? new List<BluetoothGattCharacteristic>())
                        {
                            Log.Info("InateckScanner", $"[BATTERY-BLE]   Char: {chr.Uuid} Props={chr.Properties}");
                        }
                    }
                }

                // METHOD 1: Try standard BLE Battery Service (0x180F)
                Log.Info("InateckScanner", "[BATTERY-BLE] Trying standard Battery Service (0x180F)...");
                var batteryService = _bluetoothGatt?.GetService(UUID.FromString("0000180f-0000-1000-8000-00805f9b34fb"));
                
                if (batteryService != null)
                {
                    Log.Info("InateckScanner", "[BATTERY-BLE] ✓ Battery Service found!");
                    LogToUI("[BATTERY] Standard Battery Service found");
                    
                    // Battery Level characteristic (0x2A19)
                    var batteryLevelChar = batteryService.GetCharacteristic(UUID.FromString("00002a19-0000-1000-8000-00805f9b34fb"));
                    
                    if (batteryLevelChar != null)
                    {
                        Log.Info("InateckScanner", "[BATTERY-BLE] ✓ Battery Level characteristic found!");
                        
                        // Read the battery level
                        bool readResult = _bluetoothGatt!.ReadCharacteristic(batteryLevelChar);
                        Log.Info("InateckScanner", $"[BATTERY-BLE] ReadCharacteristic returned: {readResult}");
                        
                        if (readResult)
                        {
                            // Wait for read callback
                            await Task.Delay(1000);
                            
                            var value = batteryLevelChar.GetValue();
                            if (value != null && value.Length > 0)
                            {
                                int batteryLevel = value[0];
                                Log.Info("InateckScanner", $"[BATTERY-BLE] ✓ Battery level: {batteryLevel}%");
                                LogToUI($"🔋 Battery: {batteryLevel}%");
                                StatusChanged?.Invoke(this, $"🔋 Battery: {batteryLevel}%");
                                return $"{batteryLevel}%";
                            }
                        }
                    }
                }
                else
                {
                    Log.Info("InateckScanner", "[BATTERY-BLE] Battery Service (0x180F) not found");
                }

                // METHOD 2: Try Device Information Service for battery info
                Log.Info("InateckScanner", "[BATTERY-BLE] Trying Device Information Service...");
                var deviceInfoService = _bluetoothGatt?.GetService(UUID.FromString("0000180a-0000-1000-8000-00805f9b34fb"));
                
                if (deviceInfoService != null)
                {
                    Log.Info("InateckScanner", "[BATTERY-BLE] Device Info Service found, checking characteristics...");
                    foreach (var chr in deviceInfoService.Characteristics ?? new List<BluetoothGattCharacteristic>())
                    {
                        Log.Info("InateckScanner", $"[BATTERY-BLE] DevInfo Char: {chr.Uuid}");
                    }
                }

                // METHOD 3: Check if there's a battery characteristic in the main service (ffe0)
                Log.Info("InateckScanner", "[BATTERY-BLE] Checking service ffe0 for battery characteristic...");
                var mainService = _bluetoothGatt?.GetService(UUID.FromString("0000ffe0-0000-1000-8000-00805f9b34fb"));
                
                if (mainService != null)
                {
                    foreach (var chr in mainService.Characteristics ?? new List<BluetoothGattCharacteristic>())
                    {
                        int props = (int)chr.Properties;
                        bool canRead = (props & 0x02) != 0;  // PROPERTY_READ
                        
                        if (canRead)
                        {
                            Log.Info("InateckScanner", $"[BATTERY-BLE] Trying to read characteristic {chr.Uuid}...");
                            bool readResult = _bluetoothGatt!.ReadCharacteristic(chr);
                            
                            if (readResult)
                            {
                                await Task.Delay(500);
                                var value = chr.GetValue();
                                if (value != null && value.Length > 0)
                                {
                                    string hexValue = BitConverter.ToString(value);
                                    Log.Info("InateckScanner", $"[BATTERY-BLE] Char {chr.Uuid} value: {hexValue}");
                                }
                            }
                        }
                    }
                }

                Log.Info("InateckScanner", "[BATTERY-BLE] No battery information found via standard methods");
                LogToUI("[BATTERY] Battery service not available on this device");
                return "Battery info not available";
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[BATTERY-BLE] Exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        // Keep the old command-based approach as reference (not used now)
        private async Task<string?> GetBatteryViaCommandsAsync()
        {
            try
            {
                // Try different battery commands that Inateck scanners understand
                string[] batteryCommands = new string[]
                {
                    "BATTERY\r",     // Common command
                    "battery\r",     // Lowercase variant
                    "BAT\r",         // Short form
                    "B\r",           // Single char
                };

                foreach (var command in batteryCommands)
                {
                    Log.Info("InateckScanner", $"[BATTERY-BLE] Trying command: {command.Trim()}");
                    var response = await SendGattCommandAsync(command, 3000);
                    
                    Log.Info("InateckScanner", $"[BATTERY-BLE] Response: {response ?? "NULL"}");
                    
                    if (!string.IsNullOrEmpty(response) && 
                        !response.Contains("failed") && 
                        !response.Contains("timeout") &&
                        !response.Contains("not found"))
                    {
                        // Try to parse battery percentage from response
                        var batteryInfo = ParseBatteryResponse(response);
                        if (batteryInfo != null)
                        {
                            Log.Info("InateckScanner", $"[BATTERY-BLE] Parsed battery: {batteryInfo}");
                            LogToUI($"🔋 Battery: {batteryInfo}");
                            StatusChanged?.Invoke(this, $"🔋 Battery: {batteryInfo}");
                            return batteryInfo;
                        }
                    }
                }

                Log.Info("InateckScanner", "[BATTERY-BLE] No valid battery response from any command");
                LogToUI("[BATTERY] No response to battery commands");
                return "Battery info unavailable";
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[BATTERY-BLE] Exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Get battery via Classic Bluetooth
        /// </summary>
        private async Task<string?> GetBatteryViaClassicAsync()
        {
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
            {
                return "Device not connected";
            }

            try
            {
                Log.Info("InateckScanner", "[BATTERY-BT] Sending battery command...");
                
                string command = "BATTERY\r";
                var stream = _bluetoothSocket.OutputStream;
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
                stream.Flush();
                
                // Read response
                var response = await ReadBatteryResponseAsync(3000);
                
                if (!string.IsNullOrEmpty(response))
                {
                    var batteryInfo = ParseBatteryResponse(response);
                    Log.Info("InateckScanner", $"[BATTERY-BT] Battery: {batteryInfo}");
                    return batteryInfo ?? response;
                }
                
                return "Battery info unavailable";
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[BATTERY-BT] Exception: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Parse battery response from scanner
        /// </summary>
        private string? ParseBatteryResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return null;

            // Clean response
            response = response.Trim();
            Log.Info("InateckScanner", $"[BATTERY] Parsing response: '{response}'");

            // If response contains a number, extract it as percentage
            var match = System.Text.RegularExpressions.Regex.Match(response, @"(\d+)");
            if (match.Success)
            {
                return $"{match.Groups[1].Value}%";
            }

            // If response is already in percentage format
            if (response.Contains("%"))
            {
                return response;
            }

            // Return raw response if we can't parse it
            return response;
        }

        /// <summary>
        /// Read battery response from classic Bluetooth
        /// </summary>
        private async Task<string?> ReadBatteryResponseAsync(int timeoutMs = 3000)
        {
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
                return null;

            try
            {
                var stream = _bluetoothSocket.InputStream;
                byte[] buffer = new byte[256];
                var readTask = Task.Run(() =>
                {
                    return stream.Read(buffer, 0, buffer.Length);
                });

                var completedTask = await Task.WhenAny(readTask, Task.Delay(timeoutMs));
                
                if (completedTask == readTask && readTask.IsCompletedSuccessfully)
                {
                    int bytesRead = readTask.Result;
                    if (bytesRead > 0)
                    {
                        return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get battery using Inateck SDK messager API (correct approach)
        /// </summary>
        private async Task<string?> GetBatteryViaSdkAsync()
        {
            try
            {
                Log.Info("InateckScanner", "[SDK] GetBatteryViaSdkAsync: START");
                
                // Get the BleScannerDevice from Inateck SDK
                var device = GetInateckDevice();
                Log.Info("InateckScanner", $"[SDK] GetBatteryViaSdkAsync: GetInateckDevice returned {(device != null ? device.Name : "NULL")}");
                
                if (device == null)
                {
                    Log.Error("InateckScanner", "[SDK] GetBatteryViaSdkAsync: No Inateck device found");
                    LogToUI("[SDK] No Inateck device found");
                    return "Device not connected";
                }

                Log.Info("InateckScanner", $"[SDK] GetBatteryViaSdkAsync: Found device {device.Name}, calling Messager.GetBatteryInfo");

                // Use the SDK's messager API with callback
                var tcs = new TaskCompletionSource<string?>();

                try
                {
                    var callback = new BatteryCallback(
                        onSuccess: (battery) =>
                        {
                            Log.Info("InateckScanner", $"[SDK] Battery callback SUCCESS: {battery}");
                            LogToUI($"🔋 Battery: {battery}");
                            tcs.TrySetResult(battery);
                        },
                        onFailure: (error) =>
                        {
                            Log.Error("InateckScanner", $"[SDK] Battery callback FAILURE: {error}");
                            tcs.TrySetResult($"Failed: {error}");
                        }
                    );

                    Log.Info("InateckScanner", "[SDK] Created BatteryCallback, calling device.Messager.GetBatteryInfo()");
                    device.Messager?.GetBatteryInfo(callback);
                    Log.Info("InateckScanner", "[SDK] GetBatteryInfo() call completed");
                }
                catch (Exception ex)
                {
                    Log.Error("InateckScanner", $"[SDK] Exception calling GetBatteryInfo: {ex.Message}");
                    return $"SDK call failed: {ex.Message}";
                }

                // Wait with timeout
                var result = await Task.WhenAny(
                    tcs.Task,
                    Task.Delay(5000)
                );

                if (result == tcs.Task && tcs.Task.IsCompleted)
                {
                    var battery = await tcs.Task;
                    Log.Info("InateckScanner", $"[SDK] GetBatteryViaSdkAsync returning: {battery}");
                    return battery;
                }
                else
                {
                    Log.Error("InateckScanner", "[SDK] Battery request timeout (no response after 5 seconds)");
                    return "Timeout (no response from device)";
                }
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[SDK] Exception in GetBatteryViaSdkAsync: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Callback class for battery info requests
        /// </summary>
        private class BatteryCallback : Java.Lang.Object, Kotlin.Jvm.Functions.IFunction1
        {
            private readonly Action<string> _onSuccess;
            private readonly Action<string> _onFailure;

            public BatteryCallback(Action<string> onSuccess, Action<string> onFailure)
            {
                _onSuccess = onSuccess;
                _onFailure = onFailure;
            }

            // IFunction1.Invoke(T)
            public Java.Lang.Object? Invoke(Java.Lang.Object? result)
            {
                try
                {
                    if (result != null)
                    {
                        var batteryString = result.ToString() ?? "Unknown";
                        _onSuccess(batteryString);
                    }
                    else
                    {
                        _onFailure("Battery info is null");
                    }
                }
                catch (Exception ex)
                {
                    _onFailure($"Parse error: {ex.Message}");
                }
                return null;
            }
        }

        public async Task<string?> ScanBarcodeAsync()
        {
            // Check if BLE connected
            if (_bluetoothGatt != null && IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("[BLE] Using BLE to scan barcode");
                return await ScanBarcodeBleAsync();
            }

            // Check if classic Bluetooth connected
            if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("[BT] Using Classic Bluetooth to scan barcode");
                return await ScanBarcodeClassicAsync();
            }

            System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Not connected for barcode scan");
            ErrorOccurred?.Invoke(this, "Not connected to device");
            return null;
        }

        private async Task<string?> ScanBarcodeClassicAsync()
        {
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
            {
                ErrorOccurred?.Invoke(this, "Not connected to device");
                return null;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[BT] Initiating barcode scan (Classic)...");
                StatusChanged?.Invoke(this, "📷 Waiting for barcode...");

                // Send SCAN command to device
                string command = "SCAN\r";
                var stream = _bluetoothSocket.OutputStream;
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
                stream.Flush();
                System.Diagnostics.Debug.WriteLine("[BT] SCAN command sent");

                // Read response with extended timeout (15 seconds for actual scanning)
                var response = await ReadBarcodeResponseAsync(15000);
                
                if (!string.IsNullOrEmpty(response))
                {
                    System.Diagnostics.Debug.WriteLine($"[BT] Barcode received: {response}");
                    DataReceived?.Invoke(this, response);
                    StatusChanged?.Invoke(this, "✓ Barcode scanned");
                    return response;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[BT] ERROR: No response from device");
                    ErrorOccurred?.Invoke(this, "No response from device (timeout)");
                    StatusChanged?.Invoke(this, "✗ Scan timeout");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BT] ERROR during scan: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"Scan error: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> ScanBarcodeBleAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[BLE] Initiating barcode scan (BLE)...");
                StatusChanged?.Invoke(this, "📷 Waiting for barcode...");

                // Discover services if not already done
                if (_bluetoothGatt?.Services?.Count == 0)
                {
                    Log.Info("InateckScanner", "[BLE] Discovering services...");
                    _servicesDiscoveredTcs = new TaskCompletionSource<bool>();
                    _bluetoothGatt.DiscoverServices();
                    
                    var discoverTask = _servicesDiscoveredTcs.Task;
                    var timeoutTask = Task.Delay(5000);
                    await Task.WhenAny(discoverTask, timeoutTask);
                }

                string command = "SCAN\r";
                var response = await SendGattCommandAsync(command, 15000);
                
                if (!string.IsNullOrEmpty(response) && !response.Contains("failed") && !response.Contains("timeout"))
                {
                    System.Diagnostics.Debug.WriteLine($"[BLE] Barcode received: {response}");
                    DataReceived?.Invoke(this, response);
                    StatusChanged?.Invoke(this, "✓ Barcode scanned");
                    return response;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[BLE] ERROR: No valid response from device");
                    ErrorOccurred?.Invoke(this, "No response from device (timeout)");
                    StatusChanged?.Invoke(this, "✗ Scan timeout");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BLE] ERROR during scan: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"Scan error: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> ReadBarcodeResponseAsync(int timeoutMs = 15000)
        {
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
                return null;

            try
            {
                var stream = _bluetoothSocket.InputStream;
                byte[] buffer = new byte[2048];
                var readTask = Task.Run(() =>
                {
                    return stream.Read(buffer, 0, buffer.Length);
                });

                // Wait with timeout
                var completedTask = await Task.WhenAny(readTask, Task.Delay(timeoutMs));
                
                if (completedTask == readTask && readTask.IsCompletedSuccessfully)
                {
                    int bytesRead = readTask.Result;
                    if (bytesRead > 0)
                    {
                        string data = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                        System.Diagnostics.Debug.WriteLine($"[Bluetooth] Raw data: {data}");
                        
                        // Parse barcode (remove control characters and newlines)
                        string barcode = data.Replace("\r", "").Replace("\n", "").Trim();
                        
                        // Filter empty responses
                        if (!string.IsNullOrEmpty(barcode) && barcode.Length > 3)
                        {
                            return barcode;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Bluetooth] Read timeout");
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bluetooth] ERROR reading barcode: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> SetVolumeAsync(int level)
        {
            string command = $"VOLUME:{level}\r";
            var result = await SendCommandAsync(command);
            return !string.IsNullOrEmpty(result);
        }

        public async Task<bool> ConfigureForDataMatrixOnlyAsync()
        {
            var result = await SendCommandAsync("CONFIG:DATAMATRIX_ONLY\r");
            return !string.IsNullOrEmpty(result);
        }

        public async Task<string> GetDeviceInfo(ScannerDeviceInfo device)
        {
            return $"{device.Name} ({device.Mac}) - Signal: {device.Rssi} dBm";
        }

        private async Task<string?> SendCommandAsync(string command)
        {
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
            {
                ErrorOccurred?.Invoke(this, "Not connected");
                return null;
            }

            try
            {
                var stream = _bluetoothSocket.OutputStream;
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
                stream.Flush();

                // Read response with timeout
                var response = await ReadResponseWithTimeoutAsync(5000);
                return response;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Command failed: {ex.Message}");
                return null;
            }
        }

        protected TaskCompletionSource<string?>? _gattResponseTcs;

        public void SetGattResponse(string? response)
        {
            if (_gattResponseTcs != null && !_gattResponseTcs.Task.IsCompleted)
            {
                _gattResponseTcs.SetResult(response);
            }
        }

        private async Task<string?> SendCommandWithResponseAsync(string command, int timeoutMs = 5000)
        {
            // Try GATT first (BLE)
            if (_bluetoothGatt != null && IsConnected)
            {
                Log.Info("InateckScanner", "[CMD] Trying BLE (GATT)...");
                var gattResponse = await SendGattCommandAsync(command, timeoutMs);
                
                // If GATT succeeded, return it
                if (gattResponse != null && !gattResponse.Contains("Timeout"))
                {
                    Log.Info("InateckScanner", $"[CMD] ✓ Got response via BLE: {gattResponse}");
                    return gattResponse;
                }
                
                Log.Info("InateckScanner", "[CMD] BLE failed or timed out, trying Bluetooth Classic...");
            }

            // Fallback to classic Bluetooth socket
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("[Bluetooth] ERROR: Not connected (GATT or Socket)");
                Log.Error("InateckScanner", "[CMD] ERROR: Not connected to GATT or Socket");
                return null;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Bluetooth] Sending command via Socket: {command.Trim()}");
                Log.Info("InateckScanner", $"[CMD] Sending via Socket: {command.Trim()}");
                var stream = _bluetoothSocket.OutputStream;
                byte[] data = Encoding.ASCII.GetBytes(command);
                stream.Write(data, 0, data.Length);
                stream.Flush();

                // Read response with specified timeout
                var response = await ReadResponseWithTimeoutAsync(timeoutMs);
                Log.Info("InateckScanner", $"[CMD] ✓ Socket response: {response}");
                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bluetooth] ERROR sending command: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> SendGattCommandAsync(string command, int timeoutMs = 5000)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GATT] Sending command: {command.Trim()}");
                
                // Get service 0000ffe0
                var service = _bluetoothGatt?.GetService(UUID.FromString("0000ffe0-0000-1000-8000-00805f9b34fb"));
                if (service == null)
                {
                    System.Diagnostics.Debug.WriteLine("[GATT] ERROR: Service 0000ffe0 not found");
                    LogToUI("[GATT] ERROR: Service not found");
                    return "Service not found";
                }

                // Try different characteristic UUIDs to find writable one
                string[] charUuids = new string[]
                {
                    "0000ffe0-0000-1000-8000-00805f9b34fb",  // Try this first (usually TX)
                    "0000ffe2-0000-1000-8000-00805f9b34fb",  // Try ffe2
                    "0000fff1-0000-1000-8000-00805f9b34fb",  // Try fff1
                    "0000fff2-0000-1000-8000-00805f9b34fb",  // Try fff2
                    "0000fff3-0000-1000-8000-00805f9b34fb",  // Try fff3
                };

                BluetoothGattCharacteristic? txCharacteristic = null;
                
                foreach (var charUuid in charUuids)
                {
                    var candidate = service.GetCharacteristic(UUID.FromString(charUuid));
                    if (candidate == null) continue;
                    
                    int props = (int)candidate.Properties;
                    bool canWrite = (props & 0x08) != 0;  // PROPERTY_WRITE
                    bool canWriteNoResp = (props & 0x04) != 0;  // PROPERTY_WRITE_NO_RESPONSE
                    
                    System.Diagnostics.Debug.WriteLine($"[GATT] Char {charUuid} - Write: {canWrite}, WriteNoResp: {canWriteNoResp}");
                    
                    if (canWrite || canWriteNoResp)
                    {
                        txCharacteristic = candidate;
                        System.Diagnostics.Debug.WriteLine($"[GATT] Found writable characteristic: {charUuid}");
                        LogToUI($"[GATT] Found writable: {charUuid.Substring(4, 4)}");
                        break;
                    }
                }
                
                if (txCharacteristic == null)
                {
                    System.Diagnostics.Debug.WriteLine("[GATT] ERROR: No writable characteristic found");
                    LogToUI("[GATT] ERROR: No writable characteristic");
                    return "No writable characteristic found";
                }

                // Check if characteristic has write permission
                int txProps = (int)txCharacteristic.Properties;
                bool canWrite2 = (txProps & 0x08) != 0;  // PROPERTY_WRITE
                bool canWriteNoResp2 = (txProps & 0x04) != 0;  // PROPERTY_WRITE_NO_RESPONSE
                
                System.Diagnostics.Debug.WriteLine($"[GATT] Using characteristic - Properties: {txProps}, CanWrite: {canWrite2}, CanWriteNoResp: {canWriteNoResp2}");
                LogToUI($"[GATT] TX Properties: Write={canWrite2}, WriteNoResp={canWriteNoResp2}");

                if (!canWrite2 && !canWriteNoResp2)
                {
                    System.Diagnostics.Debug.WriteLine("[GATT] ERROR: Characteristic is read-only");
                    LogToUI("[GATT] ERROR: TX is read-only (not writable)");
                    return "Characteristic is read-only";
                }

                // Write command
                byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                txCharacteristic.SetValue(commandBytes);
                
                System.Diagnostics.Debug.WriteLine($"[GATT] Writing {commandBytes.Length} bytes: {command.Trim()}");
                Log.Info("InateckScanner", $"[GATT] About to write command: {command.Trim()} ({commandBytes.Length} bytes)");
                LogToUI($"[GATT] Writing command: {command.Trim()}");
                
                bool writeSuccess = _bluetoothGatt.WriteCharacteristic(txCharacteristic);
                
                System.Diagnostics.Debug.WriteLine($"[GATT] WriteCharacteristic returned: {writeSuccess}");
                Log.Info("InateckScanner", $"[GATT] WriteCharacteristic returned: {writeSuccess}");
                
                if (!writeSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("[GATT] ERROR: WriteCharacteristic returned false");
                    LogToUI("[GATT] ERROR: WriteCharacteristic failed");
                    return "Write failed";
                }

                // Give device time to prepare response
                // Give device time to prepare response
                await Task.Delay(500);
                
                // Prepare to receive response via notification
                _gattResponseTcs = new TaskCompletionSource<string?>();
                
                Log.Info("InateckScanner", "[GATT] Waiting for response via notification (10 seconds timeout)...");
                LogToUI("[GATT] Waiting for device response...");

                // Wait for response with timeout (10 seconds to give device time to respond)
                var responseTask = _gattResponseTcs.Task;
                var completedTask = await Task.WhenAny(responseTask, Task.Delay(10000));
                
                if (completedTask == responseTask && responseTask.IsCompletedSuccessfully)
                {
                    var result = responseTask.Result;
                    Log.Info("InateckScanner", $"[GATT] ✓ Got response: {result}");
                    return result;
                }
                else
                {
                    Log.Info("InateckScanner", "[GATT] Timeout waiting for response (no notification received)");
                    LogToUI("[GATT] ⏱ No response from device");
                    return "Timeout";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GATT] ERROR: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private async Task<string?> ReadResponseWithTimeoutAsync(int timeoutMs = 5000)
        {
            if (_bluetoothSocket == null || !_bluetoothSocket.IsConnected)
                return null;

            try
            {
                var stream = _bluetoothSocket.InputStream;
                byte[] buffer = new byte[1024];
                
                var readTask = Task.Run(() =>
                {
                    return stream.Read(buffer, 0, buffer.Length);
                });

                var completedTask = await Task.WhenAny(readTask, Task.Delay(timeoutMs));
                
                if (completedTask == readTask && readTask.IsCompletedSuccessfully)
                {
                    int bytesRead = readTask.Result;
                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                        System.Diagnostics.Debug.WriteLine($"[Bluetooth] Response: {response}");
                        return response;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Bluetooth] Timeout waiting for response ({timeoutMs}ms)");
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bluetooth] ERROR reading response: {ex.Message}");
                return null;
            }
        }

        public void OnDeviceDiscovered(BluetoothDevice device, int rssi)
        {
            // FILTER: Only show Inatek devices (name contains BCST)
            if (!IsInatekDevice(device.Name))
            {
                System.Diagnostics.Debug.WriteLine($"[Bluetooth] Filtered non-Inatek device: {device.Name}");
                return;
            }

            // Avoid duplicates
            if (_discoveredDevices.Any(d => d.Mac == device.Address))
                return;

            var deviceInfo = new ScannerDeviceInfo
            {
                Name = device.Name ?? "Unknown",
                Mac = device.Address,
                ConnectionState = "Available",
                Rssi = rssi
            };

            _discoveredDevices.Add(deviceInfo);
            System.Diagnostics.Debug.WriteLine($"[Bluetooth] Inatek device discovered: {deviceInfo.Name} ({device.Address}) RSSI: {rssi}");
            DeviceDiscovered?.Invoke(this, deviceInfo);
        }

        /// <summary>
        /// Check if device is an Inatek scanner (name contains BCST)
        /// </summary>
        private bool IsInatekDevice(string? deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return false;
            return deviceName.Contains("BCST", StringComparison.OrdinalIgnoreCase);
        }

        public void OnDiscoveryFinished()
        {
            StatusChanged?.Invoke(this, "Discovery finished");
        }

        /// <summary>
        /// Store the current connected MAC for Inateck SDK lookup
        /// </summary>
        public void SetCurrentConnectedMac(string mac)
        {
            _currentConnectedMac = mac;
            Log.Info("InateckScanner", $"[SDK] SetCurrentConnectedMac: Stored MAC {mac}");
        }

        /// <summary>
        /// Set the current Inateck device (called by the app when connecting)
        /// </summary>
        public void SetInateckDevice(BleScannerDevice? device)
        {
            _currentInateckDevice = device;
            if (device != null)
            {
                Log.Info("InateckScanner", $"[SDK] SetInateckDevice: Device set to: {device.Name} ({device.Mac})");
            }
            else
            {
                Log.Info("InateckScanner", "[SDK] SetInateckDevice: Called with null device");
            }
        }

        /// <summary>
        /// Helper method to get the currently connected Inateck BleScannerDevice from the SDK
        /// </summary>
        private BleScannerDevice? GetInateckDevice()
        {
            Log.Info("InateckScanner", "[SDK] GetInateckDevice: START");
            
            // Return the cached device if we have it
            if (_currentInateckDevice != null)
            {
                Log.Info("InateckScanner", $"[SDK] ✓ Returning cached device: {_currentInateckDevice.Name}");
                return _currentInateckDevice;
            }

            Log.Info("InateckScanner", "[SDK] Cached device is NULL, trying BleListManager.Instance");

            try
            {
                // Try to get device from BleListManager
                // This is the official SDK way to access connected devices
                Log.Info("InateckScanner", "[SDK] Accessing BleListManager.Instance.ScannerDevices");
                
                var listManager = BleListManager.Instance;
                Log.Info("InateckScanner", $"[SDK] BleListManager.Instance: {(listManager != null ? "OK" : "NULL")}");
                
                if (listManager == null)
                {
                    Log.Error("InateckScanner", "[SDK] BleListManager.Instance is NULL");
                    return null;
                }

                var devices = listManager.ScannerDevices;
                Log.Info("InateckScanner", $"[SDK] ScannerDevices collection: {(devices != null ? $"OK ({devices.Count} items)" : "NULL")}");
                
                if (devices == null || devices.Count == 0)
                {
                    Log.Error("InateckScanner", "[SDK] No devices in BleListManager.ScannerDevices");
                    Log.Info("InateckScanner", "[SDK] NOTE: You may need to scan using the Inateck SDK first");
                    return null;
                }

                // Look for connected device by MAC
                var macToFind = _currentConnectedMac ?? _connectedDevice?.Address;
                Log.Info("InateckScanner", $"[SDK] Looking for device with MAC: {macToFind ?? "NULL"}");
                
                if (string.IsNullOrEmpty(macToFind))
                {
                    Log.Error("InateckScanner", "[SDK] No MAC address to search");
                    return null;
                }

                foreach (var device in devices)
                {
                    if (device != null && !string.IsNullOrEmpty(device.Mac))
                    {
                        Log.Info("InateckScanner", $"[SDK] Checking: {device.Name ?? "?"} ({device.Mac})");
                        
                        if (device.Mac.Equals(macToFind, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.Info("InateckScanner", $"[SDK] ✓ FOUND: {device.Name}");
                            _currentInateckDevice = device;
                            return device;
                        }
                    }
                }

                Log.Error("InateckScanner", $"[SDK] Device with MAC {macToFind} not found in BleListManager");
                return null;
            }
            catch (Java.Lang.NullPointerException npe)
            {
                Log.Error("InateckScanner", $"[SDK] NullPointerException: {npe.Message}");
                return null;
            }
            catch (Java.Lang.Exception je)
            {
                Log.Error("InateckScanner", $"[SDK] Java Exception: {je.GetType().Name}: {je.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error("InateckScanner", $"[SDK] Exception: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// BluetoothGatt callback handler for BLE connection state changes and service discovery
    /// </summary>
    public class BleGattCallback : BluetoothGattCallback
    {
        private readonly AndroidScannerService _scannerService;

        public BleGattCallback(AndroidScannerService scannerService)
        {
            _scannerService = scannerService;
        }

        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);
            
            _scannerService.LogToUI($"[CALLBACK] OnConnectionStateChange invoked! Status: {status}, State: {newState}");
            Log.Info("InateckScanner", $"[GATT] OnConnectionStateChange - Status: {status}, State: {newState}");
            
            // Log detailed state information
            System.Diagnostics.Debug.WriteLine($"[GATT] Connection State - Status enum: {(int)status}, State: {(int)newState}");

            if (newState == ProfileState.Connected || (int)newState == 2)
            {
                _scannerService.LogToUI("[BLE] ✓ GATT connected successfully!");
                Log.Info("InateckScanner", "[GATT] ✓ Connection state: CONNECTED");
                System.Diagnostics.Debug.WriteLine("[GATT] Calling OnGattConnected()...");
                _scannerService.OnGattConnected();
            }
            else if (newState == ProfileState.Disconnected || (int)newState == 0)
            {
                _scannerService.LogToUI("[BLE] ✗ GATT disconnected");
                Log.Info("InateckScanner", "[GATT] ✗ Connection state: DISCONNECTED");
                System.Diagnostics.Debug.WriteLine("[GATT] Calling OnGattDisconnected()...");
                _scannerService.OnGattDisconnected();
            }
            else
            {
                _scannerService.LogToUI($"[BLE] Unknown state: {(int)newState}");
                Log.Warn("InateckScanner", $"[GATT] Unknown state: {newState} (int: {(int)newState})");
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);

            if (status == GattStatus.Success)
            {
                Log.Info("InateckScanner", "[GATT] Services discovered successfully");
                _scannerService.OnServicesDiscovered();
            }
            else
            {
                Log.Error("InateckScanner", $"[GATT] Service discovery failed with status: {status}");
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            base.OnCharacteristicRead(gatt, characteristic, status);

            if (status == GattStatus.Success && characteristic?.GetValue() != null)
            {
                var data = characteristic.GetValue();
                string dataStr = System.Text.Encoding.ASCII.GetString(data);
                Log.Info("InateckScanner", $"[GATT] ✓ Characteristic read: {characteristic.Uuid} = {dataStr}");
                
                // Complete the response task if waiting
                _scannerService.SetGattResponse(dataStr);
            }
            else
            {
                Log.Error("InateckScanner", $"[GATT] ✗ Characteristic read failed: {characteristic?.Uuid} - {status}");
            }
        }

        public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            base.OnCharacteristicWrite(gatt, characteristic, status);

            if (status == GattStatus.Success)
            {
                Log.Info("InateckScanner", $"[GATT] ✓ Characteristic write successful: {characteristic?.Uuid}");
                
                // Now read the response
                if (characteristic != null && gatt != null)
                {
                    gatt.ReadCharacteristic(characteristic);
                }
            }
            else
            {
                Log.Error("InateckScanner", $"[GATT] ✗ Characteristic write failed: {characteristic?.Uuid} - {status}");
                _scannerService.SetGattResponse($"Write failed: {status}");
            }
        }

        public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);

            Log.Info("InateckScanner", "[GATT] *** OnCharacteristicChanged CALLED ***");
            
            if (characteristic != null && characteristic.GetValue() != null)
            {
                var data = characteristic.GetValue();
                string dataStr = System.Text.Encoding.ASCII.GetString(data);
                Log.Info("InateckScanner", $"[GATT] ✓ Characteristic changed: {characteristic.Uuid} = {dataStr}");
                
                // Complete the response task if waiting
                _scannerService.SetGattResponse(dataStr);
            }
            else
            {
                Log.Info("InateckScanner", $"[GATT] OnCharacteristicChanged but data is null/empty. Char: {characteristic?.Uuid}");
            }
        }
    }

    /// <summary>
    /// BroadcastReceiver para manejar descubrimiento de dispositivos Bluetooth
    /// </summary>
    public class BluetoothDiscoveryReceiver : BroadcastReceiver
    {
        private readonly AndroidScannerService _scannerService;

        public BluetoothDiscoveryReceiver(AndroidScannerService scannerService)
        {
            _scannerService = scannerService;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null)
                return;

            string action = intent.Action ?? "";

            if (action == BluetoothDevice.ActionFound)
            {
                var device = (BluetoothDevice?)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                int rssi = intent.GetShortExtra(BluetoothDevice.ExtraRssi, -100);

                if (device != null)
                {
                    _scannerService.OnDeviceDiscovered(device, rssi);
                }
            }
            else if (action == BluetoothAdapter.ActionDiscoveryFinished)
            {
                _scannerService.OnDiscoveryFinished();
            }
        }
    }
}