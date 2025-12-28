using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AW = Android.Widget;
using System.Linq;
using System.Text.Json;
using InateckMauiApp.Platforms.Android;

namespace InateckMauiApp;

/// <summary>
/// SDK Scanner Activity - Implementation using Inatek SDK CMD library.
/// Uses GATT mode for direct communication with the scanner.
/// </summary>
[Activity(
    Label = "SDK Scanner",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = false,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                          ConfigChanges.Orientation |
                          ConfigChanges.UiMode |
                          ConfigChanges.ScreenLayout |
                          ConfigChanges.SmallestScreenSize |
                          ConfigChanges.Density)]
public class SdkScannerActivity : Activity
{
    private const int BLUETOOTH_PERMISSION_REQUEST_CODE = 100;
    
    // BCST-75S Scanner GATT Service and Characteristic UUIDs
    // Primary service: FFE0 with FFE1 (Notify) and FFE2 (Write)
    private static readonly Java.Util.UUID SERVICE_UUID = 
        Java.Util.UUID.FromString("0000ffe0-0000-1000-8000-00805f9b34fb");
    private static readonly Java.Util.UUID NOTIFY_CHAR_UUID = 
        Java.Util.UUID.FromString("0000ffe1-0000-1000-8000-00805f9b34fb");
    private static readonly Java.Util.UUID WRITE_CHAR_UUID = 
        Java.Util.UUID.FromString("0000ffe2-0000-1000-8000-00805f9b34fb");
    private static readonly Java.Util.UUID CCCD_UUID = 
        Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
    
    // Alternative service: AE00 with AE01 (Write) and AE02 (Notify)
    private static readonly Java.Util.UUID ALT_SERVICE_UUID = 
        Java.Util.UUID.FromString("0000ae00-0000-1000-8000-00805f9b34fb");
    private static readonly Java.Util.UUID ALT_WRITE_CHAR_UUID = 
        Java.Util.UUID.FromString("0000ae01-0000-1000-8000-00805f9b34fb");
    private static readonly Java.Util.UUID ALT_NOTIFY_CHAR_UUID = 
        Java.Util.UUID.FromString("0000ae02-0000-1000-8000-00805f9b34fb");
    
    // UI Elements
    private AW.TextView? _statusText;
    private AW.TextView? _connectionStatus;
    private AW.TextView? _deviceInfoText;
    private AW.Button? _scanButton;
    private AW.Button? _disconnectButton;
    private AW.Button? _backButton;
    private AW.LinearLayout? _devicesLayout;
    private AW.LinearLayout? _codesListView;
    private AW.ScrollView? _mainScrollView;
    
    // State
    private List<string> _scannedBarcodes = new();
    private string? _connectedDeviceMac;
    private bool _isConnected = false;
    private List<BluetoothDevice> _discoveredDevices = new();
    
    // BLE Components
    private BluetoothAdapter? _bluetoothAdapter;
    private BluetoothGatt? _bluetoothGatt;
    private BluetoothGattCharacteristic? _writeCharacteristic;
    private BluetoothGattCharacteristic? _notifyCharacteristic;
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        Log.Info("InateckScanner", "[SDK] SdkScannerActivity OnCreate");
        
        // Initialize Bluetooth
        var bluetoothManager = (BluetoothManager?)GetSystemService(BluetoothService);
        _bluetoothAdapter = bluetoothManager?.Adapter;
        
        // Request Bluetooth permissions (Android 12+)
        RequestBluetoothPermissions();
        
        CreateUI();
    }
    
    private void CreateUI()
    {
        // Create main layout
        _mainScrollView = new AW.ScrollView(this);
        _mainScrollView.SetBackgroundColor(Android.Graphics.Color.White);
        
        var scrollContent = new AW.LinearLayout(this)
        {
            Orientation = AW.Orientation.Vertical
        };
        scrollContent.SetPadding(20, 20, 20, 20);
        
        // Back button
        _backButton = new AW.Button(this)
        {
            Text = "← Back to Menu"
        };
        _backButton.SetTextColor(Android.Graphics.Color.DarkGray);
        _backButton.SetBackgroundColor(Android.Graphics.Color.LightGray);
        _backButton.Click += (s, e) => Finish();
        var backParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.WrapContent,
            AW.LinearLayout.LayoutParams.WrapContent);
        backParams.SetMargins(0, 0, 0, 20);
        scrollContent.AddView(_backButton, backParams);
        
        // Title with mode indicator
        var titleText = new AW.TextView(this)
        {
            Text = "📡 SDK Mode (GATT + CMD Library)",
            TextSize = 22
        };
        titleText.SetTextColor(Android.Graphics.Color.ParseColor("#2196F3"));
        titleText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        scrollContent.AddView(titleText);
        
        // Description
        var descText = new AW.TextView(this)
        {
            Text = "Uses native Inatek CMD library for parsing scanner data.\nScanner must be in GATT mode.",
            TextSize = 12
        };
        descText.SetTextColor(Android.Graphics.Color.Gray);
        scrollContent.AddView(descText);
        
        // Status display
        _statusText = new AW.TextView(this)
        {
            Text = "Ready - SDK CMD Library",
            TextSize = 14
        };
        _statusText.SetTextColor(Android.Graphics.Color.DarkGray);
        var statusParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        statusParams.SetMargins(0, 20, 0, 20);
        scrollContent.AddView(_statusText, statusParams);
        
        // Button layout params
        var buttonParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        buttonParams.SetMargins(0, 10, 0, 10);
        
        // Scan button
        _scanButton = new AW.Button(this)
        {
            Text = "🔍 SCAN FOR DEVICES"
        };
        _scanButton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#2196F3"));
        _scanButton.SetTextColor(Android.Graphics.Color.White);
        _scanButton.Click += (s, e) => _ = OnScanClick();
        scrollContent.AddView(_scanButton, buttonParams);
        
        // Connection status
        _connectionStatus = new AW.TextView(this)
        {
            Text = "⬤ Disconnected",
            TextSize = 14
        };
        _connectionStatus.SetTextColor(Android.Graphics.Color.Red);
        var connParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        connParams.SetMargins(0, 10, 0, 20);
        scrollContent.AddView(_connectionStatus, connParams);
        
        // Device info display
        _deviceInfoText = new AW.TextView(this)
        {
            Text = "",
            TextSize = 12
        };
        _deviceInfoText.SetTextColor(Android.Graphics.Color.DarkGray);
        scrollContent.AddView(_deviceInfoText);
        
        // Disconnect button
        _disconnectButton = new AW.Button(this)
        {
            Text = "⏏ DISCONNECT",
            Enabled = false
        };
        _disconnectButton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#f44336"));
        _disconnectButton.SetTextColor(Android.Graphics.Color.White);
        _disconnectButton.Click += (s, e) => _ = OnDisconnectClick();
        scrollContent.AddView(_disconnectButton, buttonParams);
        
        // Scanned codes header
        var codesHeaderText = new AW.TextView(this)
        {
            Text = "📦 Scanned Barcodes:",
            TextSize = 16
        };
        codesHeaderText.SetTextColor(Android.Graphics.Color.Black);
        codesHeaderText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        var codesHeaderParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        codesHeaderParams.SetMargins(0, 30, 0, 10);
        scrollContent.AddView(codesHeaderText, codesHeaderParams);
        
        // Scanned codes list
        _codesListView = new AW.LinearLayout(this)
        {
            Orientation = AW.Orientation.Vertical
        };
        _codesListView.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E3F2FD"));
        _codesListView.SetPadding(15, 15, 15, 15);
        scrollContent.AddView(_codesListView);
        
        // Add placeholder for codes
        var noCodesText = new AW.TextView(this)
        {
            Text = "No barcodes scanned yet\n(Scanner sends data automatically when scanned)",
            TextSize = 12
        };
        noCodesText.SetTextColor(Android.Graphics.Color.Gray);
        _codesListView.AddView(noCodesText);
        
        // Devices header
        var devicesHeaderText = new AW.TextView(this)
        {
            Text = "📱 Discovered Devices:",
            TextSize = 16
        };
        devicesHeaderText.SetTextColor(Android.Graphics.Color.Black);
        devicesHeaderText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        var headerParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        headerParams.SetMargins(0, 20, 0, 10);
        scrollContent.AddView(devicesHeaderText, headerParams);
        
        // Devices container
        _devicesLayout = new AW.LinearLayout(this)
        {
            Orientation = AW.Orientation.Vertical
        };
        scrollContent.AddView(_devicesLayout);
        
        _mainScrollView.AddView(scrollContent);
        SetContentView(_mainScrollView);
    }
    
    private void RequestBluetoothPermissions()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            var permissions = new string[]
            {
                Android.Manifest.Permission.BluetoothScan,
                Android.Manifest.Permission.BluetoothConnect
            };

            var permissionsToRequest = new List<string>();
            foreach (var permission in permissions)
            {
                if (ContextCompat.CheckSelfPermission(this, permission) 
                    != Android.Content.PM.Permission.Granted)
                {
                    permissionsToRequest.Add(permission);
                }
            }

            if (permissionsToRequest.Count > 0)
            {
                ActivityCompat.RequestPermissions(this, 
                    permissionsToRequest.ToArray(), 
                    BLUETOOTH_PERMISSION_REQUEST_CODE);
            }
        }
    }
    
    private async Task OnScanClick()
    {
        Log.Info("InateckScanner", "[SDK] ========== STARTING DEVICE SCAN ==========");
        Android.Widget.Toast.MakeText(this, "Buscando dispositivos...", Android.Widget.ToastLength.Short)?.Show();
        UpdateStatus("Buscando dispositivos...");
        _devicesLayout?.RemoveAllViews();
        _discoveredDevices.Clear();
        
        try
        {
            Log.Info("InateckScanner", "[SDK] Checking Bluetooth adapter...");
            if (_bluetoothAdapter == null)
            {
                Log.Error("InateckScanner", "[SDK] Bluetooth adapter is NULL");
                UpdateStatus("Error: Bluetooth adapter es null");
                return;
            }
            
            if (!_bluetoothAdapter.IsEnabled)
            {
                Log.Error("InateckScanner", "[SDK] Bluetooth is NOT enabled");
                UpdateStatus("Error: Bluetooth no está habilitado");
                return;
            }
            
            // FIRST: Show already bonded/paired devices
            Log.Info("InateckScanner", "[SDK] Getting bonded devices...");
            var bondedDevices = _bluetoothAdapter.BondedDevices;
            if (bondedDevices != null)
            {
                foreach (var device in bondedDevices)
                {
                    var name = device.Name ?? "Desconocido";
                    Log.Info("InateckScanner", $"[SDK] Bonded device: {name} ({device.Address})");
                    
                    // Check if it's an Inatek device
                    if (name.Contains("BCST", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Inatek", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Scanner", StringComparison.OrdinalIgnoreCase))
                    {
                        AddDeviceToUI(device, true); // true = bonded
                    }
                }
            }
            
            // THEN: Also scan for new devices
            Log.Info("InateckScanner", "[SDK] Bluetooth adapter OK, getting scanner...");
            var scanner = _bluetoothAdapter.BluetoothLeScanner;
            if (scanner == null)
            {
                Log.Error("InateckScanner", "[SDK] BLE Scanner is NULL");
                UpdateStatus("Error: BLE Scanner no disponible");
                return;
            }
            
            Log.Info("InateckScanner", "[SDK] Starting BLE scan for 10 seconds...");
            var callback = new SdkScanCallback(this);
            scanner.StartScan(callback);
            
            // Scan for 10 seconds
            await Task.Delay(10000);
            
            scanner.StopScan(callback);
            Log.Info("InateckScanner", $"[SDK] Scan complete. Found {_discoveredDevices.Count} devices");
            UpdateStatus($"Escaneo completo. {_discoveredDevices.Count} dispositivos encontrados");
            Android.Widget.Toast.MakeText(this, $"Encontrados: {_discoveredDevices.Count} dispositivos", Android.Widget.ToastLength.Short)?.Show();
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[SDK] Scan error: {ex.Message}");
            Log.Error("InateckScanner", $"[SDK] Stack trace: {ex.StackTrace}");
            UpdateStatus($"Error de escaneo: {ex.Message}");
        }
    }
    
    public void OnDeviceFound(BluetoothDevice device)
    {
        if (device == null || string.IsNullOrEmpty(device.Address))
            return;
            
        // Check if already discovered
        if (_discoveredDevices.Any(d => d.Address == device.Address))
            return;
            
        var deviceName = device.Name ?? "";
        var deviceMac = device.Address;
        
        // Only show Inatek-related scanners (filter out other BLE devices)
        var isInatekDevice = deviceName.Contains("BCST", StringComparison.OrdinalIgnoreCase) ||
                            deviceName.Contains("Inatek", StringComparison.OrdinalIgnoreCase) ||
                            deviceName.Contains("HPRT", StringComparison.OrdinalIgnoreCase) ||
                            deviceName.Contains("Scanner", StringComparison.OrdinalIgnoreCase);
        
        if (!isInatekDevice)
        {
            // Skip non-Inatek devices
            return;
        }
        
        Log.Info("InateckScanner", $"[SDK] Device found via scan: {deviceName} ({deviceMac})");
        
        _discoveredDevices.Add(device);
        
        // Show only Inatek devices
        RunOnUiThread(() => AddDeviceToUI(device, false));
    }
    
    private void AddDeviceToUI(BluetoothDevice device, bool isBonded)
    {
        var deviceName = device.Name ?? "Desconocido";
        var deviceMac = device.Address;
        
        // Check if it's an Inatek device
        var isInatek = deviceName.Contains("BCST", StringComparison.OrdinalIgnoreCase) ||
                      deviceName.Contains("Inatek", StringComparison.OrdinalIgnoreCase) ||
                      deviceName.Contains("Scanner", StringComparison.OrdinalIgnoreCase);
        
        Log.Info("InateckScanner", $"[SDK] Adding to UI: {deviceName} (Inatek={isInatek}, Bonded={isBonded})");
        
        var deviceLayout = new AW.LinearLayout(this)
        {
            Orientation = AW.Orientation.Horizontal
        };
        
        // Different colors: Green for bonded Inatek, light green for scanned Inatek, gray for others
        if (isBonded && isInatek)
            deviceLayout.SetBackgroundColor(Android.Graphics.Color.ParseColor("#C8E6C9")); // Dark green for bonded
        else if (isInatek)
            deviceLayout.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E8F5E9")); // Light green for scanned
        else
            deviceLayout.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F5F5")); // Gray for others
            
        deviceLayout.SetPadding(15, 15, 15, 15);
        
        var infoLayout = new AW.LinearLayout(this)
        {
            Orientation = AW.Orientation.Vertical
        };
        infoLayout.LayoutParameters = new AW.LinearLayout.LayoutParams(
            0, AW.LinearLayout.LayoutParams.WrapContent, 1);
        
        var statusIcon = isBonded ? "🔗" : "📡";
        var nameText = new AW.TextView(this)
        {
            Text = $"{statusIcon} {deviceName}",
            TextSize = 14
        };
        nameText.SetTextColor(Android.Graphics.Color.Black);
        nameText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        infoLayout.AddView(nameText);
        
        var statusText = isBonded ? $"{deviceMac} (Vinculado)" : deviceMac;
        var macText = new AW.TextView(this)
        {
            Text = statusText,
            TextSize = 11
        };
        macText.SetTextColor(Android.Graphics.Color.Gray);
        infoLayout.AddView(macText);
        
        deviceLayout.AddView(infoLayout);
        
        var connectBtn = new AW.Button(this)
        {
            Text = "Conectar"
        };
        connectBtn.SetBackgroundColor(Android.Graphics.Color.ParseColor("#4CAF50"));
        connectBtn.SetTextColor(Android.Graphics.Color.White);
        connectBtn.Click += (s, e) => _ = ConnectToDevice(device);
        deviceLayout.AddView(connectBtn);
        
        var layoutParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        layoutParams.SetMargins(0, 5, 0, 5);
        
        // Add bonded devices at top, scanned at bottom
        if (isBonded)
            _devicesLayout?.AddView(deviceLayout, 0, layoutParams);
        else
            _devicesLayout?.AddView(deviceLayout, layoutParams);
    }
    
    private async Task ConnectToDevice(BluetoothDevice device)
    {
        Log.Info("InateckScanner", $"[SDK] Connecting to {device.Name} ({device.Address})...");
        UpdateStatus($"Connecting to {device.Name}...");
        
        try
        {
            var callback = new SdkGattCallback(this);
            _bluetoothGatt = device.ConnectGatt(this, false, callback, BluetoothTransports.Le);
            _connectedDeviceMac = device.Address;
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[SDK] Connection error: {ex.Message}");
            UpdateStatus($"Connection error: {ex.Message}");
        }
    }
    
    public void OnConnected()
    {
        _isConnected = true;
        RunOnUiThread(() =>
        {
            _connectionStatus!.Text = "⬤ Connected (GATT)";
            _connectionStatus.SetTextColor(Android.Graphics.Color.ParseColor("#4CAF50"));
            _disconnectButton!.Enabled = true;
            UpdateStatus("Connected - Ready to scan");
        });
    }
    
    public void OnServicesDiscovered(BluetoothGatt gatt)
    {
        Log.Info("InateckScanner", "[SDK] Services discovered, looking for Inatek service...");
        
        // List all available services for debugging
        var services = gatt.Services;
        Log.Info("InateckScanner", $"[SDK] Found {services?.Count ?? 0} services:");
        if (services != null)
        {
            foreach (var svc in services)
            {
                Log.Info("InateckScanner", $"[SDK]   Service: {svc.Uuid}");
                var chars = svc.Characteristics;
                if (chars != null)
                {
                    foreach (var chr in chars)
                    {
                        Log.Info("InateckScanner", $"[SDK]     Characteristic: {chr.Uuid} (props: {chr.Properties})");
                    }
                }
            }
        }
        
        // Try primary service FFE0 first
        var service = gatt.GetService(SERVICE_UUID);
        var writeUuid = WRITE_CHAR_UUID;
        var notifyUuid = NOTIFY_CHAR_UUID;
        
        if (service == null)
        {
            Log.Info("InateckScanner", $"[SDK] Primary service {SERVICE_UUID} not found, trying alternative AE00...");
            service = gatt.GetService(ALT_SERVICE_UUID);
            writeUuid = ALT_WRITE_CHAR_UUID;
            notifyUuid = ALT_NOTIFY_CHAR_UUID;
        }
        
        if (service == null)
        {
            Log.Error("InateckScanner", "[SDK] No compatible service found!");
            RunOnUiThread(() => UpdateStatus("Error: No compatible service found"));
            return;
        }
        
        Log.Info("InateckScanner", $"[SDK] Using service: {service.Uuid}");
        
        _writeCharacteristic = service.GetCharacteristic(writeUuid);
        _notifyCharacteristic = service.GetCharacteristic(notifyUuid);
        
        if (_writeCharacteristic == null || _notifyCharacteristic == null)
        {
            Log.Error("InateckScanner", $"[SDK] Required characteristics not found! Write={_writeCharacteristic != null}, Notify={_notifyCharacteristic != null}");
            RunOnUiThread(() => UpdateStatus("Error: Characteristics not found"));
            return;
        }
        
        Log.Info("InateckScanner", $"[SDK] Found characteristics: Write={writeUuid}, Notify={notifyUuid}");
        Log.Info("InateckScanner", "[SDK] Enabling notifications...");
        
        // Enable notifications on primary notify characteristic
        gatt.SetCharacteristicNotification(_notifyCharacteristic, true);
        
        var descriptor = _notifyCharacteristic.GetDescriptor(CCCD_UUID);
        if (descriptor != null)
        {
            descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue?.ToArray());
            gatt.WriteDescriptor(descriptor);
            Log.Info("InateckScanner", "[SDK] Notification descriptor written for primary");
        }
        
        // Also enable notifications on FF01 (barcode data service)
        var ff00Service = gatt.GetService(Java.Util.UUID.FromString("0000ff00-0000-1000-8000-00805f9b34fb"));
        if (ff00Service != null)
        {
            var ff01Char = ff00Service.GetCharacteristic(Java.Util.UUID.FromString("0000ff01-0000-1000-8000-00805f9b34fb"));
            if (ff01Char != null)
            {
                gatt.SetCharacteristicNotification(ff01Char, true);
                var ff01Desc = ff01Char.GetDescriptor(CCCD_UUID);
                if (ff01Desc != null)
                {
                    ff01Desc.SetValue(BluetoothGattDescriptor.EnableNotificationValue?.ToArray());
                    // Note: We can only write one descriptor at a time, so we'll use a handler
                    new Android.OS.Handler(Android.OS.Looper.MainLooper!).PostDelayed(() => {
                        gatt.WriteDescriptor(ff01Desc);
                        Log.Info("InateckScanner", "[SDK] Notification descriptor written for FF01");
                    }, 500);
                }
            }
        }
        
        RunOnUiThread(() =>
        {
            _connectionStatus!.Text = "⬤ Connected (Ready)";
            _disconnectButton!.Enabled = true;
            UpdateStatus("Connected! Ready to scan");
        });
    }
    
    public void OnDisconnected()
    {
        _isConnected = false;
        RunOnUiThread(() =>
        {
            _connectionStatus!.Text = "⬤ Disconnected";
            _connectionStatus.SetTextColor(Android.Graphics.Color.Red);
            _disconnectButton!.Enabled = false;
            UpdateStatus("Disconnected");
        });
    }
    
    public void OnDataReceived(byte[] data)
    {
        if (data == null || data.Length == 0) return;
        
        var hexData = BitConverter.ToString(data).Replace("-", " ");
        Log.Info("InateckScanner", $"[SDK] Data received ({data.Length} bytes): {hexData}");
        
        try
        {
            // First, try to extract barcode directly from raw data
            // Format: C1 <length> <barcode bytes> <terminator> <checksum>
            // Byte 0 (C1/193): Scan data message type
            // Byte 1: Length of data
            // Bytes 2 to length+1: Barcode ASCII bytes
            if (data.Length >= 4 && data[0] == 0xC1)
            {
                int dataLength = data[1];
                if (data.Length >= dataLength + 2)
                {
                    // Extract barcode bytes (skip header, exclude terminator 0x0A and checksum)
                    var barcodeBytes = new List<byte>();
                    for (int i = 2; i < 2 + dataLength - 1; i++)  // -1 to exclude 0x0A terminator
                    {
                        if (i < data.Length && data[i] != 0x0A)  // Stop at newline
                        {
                            barcodeBytes.Add(data[i]);
                        }
                        else break;
                    }
                    
                    if (barcodeBytes.Count > 0)
                    {
                        var barcode = System.Text.Encoding.ASCII.GetString(barcodeBytes.ToArray());
                        Log.Info("InateckScanner", $"[SDK] Extracted barcode from raw data: {barcode}");
                        OnBarcodeReceived(barcode);
                        return;
                    }
                }
            }
            
            // Use SDK CMD library to parse the data
            var result = InateckScannerCmd.ParseNotifyData(data);
            
            if (!string.IsNullOrEmpty(result))
            {
                Log.Info("InateckScanner", $"[SDK] Parsed result: {result}");
                
                // Try to parse as JSON and extract the barcode
                try
                {
                    using var doc = JsonDocument.Parse(result);
                    
                    // Check for notify_data array (contains barcode bytes)
                    if (doc.RootElement.TryGetProperty("notify_data", out var notifyDataElement))
                    {
                        if (notifyDataElement.ValueKind == JsonValueKind.Array)
                        {
                            var bytes = new List<byte>();
                            foreach (var item in notifyDataElement.EnumerateArray())
                            {
                                bytes.Add((byte)item.GetInt32());
                            }
                            
                            // Same parsing: skip header (2 bytes), extract until newline (0x0A)
                            if (bytes.Count >= 4 && bytes[0] == 0xC1)
                            {
                                var barcodeBytes = new List<byte>();
                                for (int i = 2; i < bytes.Count - 1; i++)
                                {
                                    if (bytes[i] != 0x0A)
                                        barcodeBytes.Add(bytes[i]);
                                    else break;
                                }
                                
                                if (barcodeBytes.Count > 0)
                                {
                                    var barcode = System.Text.Encoding.ASCII.GetString(barcodeBytes.ToArray());
                                    Log.Info("InateckScanner", $"[SDK] Extracted barcode from notify_data: {barcode}");
                                    OnBarcodeReceived(barcode);
                                    return;
                                }
                            }
                        }
                    }
                    
                    // Legacy: check for "code" property
                    if (doc.RootElement.TryGetProperty("code", out var codeElement))
                    {
                        var barcode = codeElement.GetString();
                        if (!string.IsNullOrEmpty(barcode))
                        {
                            OnBarcodeReceived(barcode);
                            return;
                        }
                    }
                }
                catch
                {
                    // Not JSON, treat the result as the barcode itself
                    if (!result.Contains("status") && !result.Contains("error"))
                    {
                        OnBarcodeReceived(result);
                        return;
                    }
                }
                
                // Log non-barcode responses
                RunOnUiThread(() => _deviceInfoText!.Text = $"SDK Response: {result}");
            }
            else
            {
                // Check generic result
                var checkResult = InateckScannerCmd.CheckResult(data);
                Log.Info("InateckScanner", $"[SDK] Check result: {checkResult}");
            }
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[SDK] Error parsing data: {ex.Message}");
        }
    }
    
    public void OnBarcodeReceived(string barcode)
    {
        Log.Info("InateckScanner", $"[SDK] Barcode received: {barcode}");
        _scannedBarcodes.Add(barcode);
        
        RunOnUiThread(() =>
        {
            // Clear placeholder if present
            if (_codesListView?.ChildCount == 1)
            {
                var firstChild = _codesListView.GetChildAt(0) as AW.TextView;
                if (firstChild?.Text?.Contains("No barcodes") == true)
                {
                    _codesListView.RemoveAllViews();
                }
            }
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var codeLayout = new AW.LinearLayout(this)
            {
                Orientation = AW.Orientation.Horizontal
            };
            codeLayout.SetBackgroundColor(Android.Graphics.Color.White);
            codeLayout.SetPadding(10, 10, 10, 10);
            
            var codeText = new AW.TextView(this)
            {
                Text = $"[{timestamp}] {barcode}",
                TextSize = 14
            };
            codeText.SetTextColor(Android.Graphics.Color.Black);
            codeLayout.AddView(codeText);
            
            var layoutParams = new AW.LinearLayout.LayoutParams(
                AW.LinearLayout.LayoutParams.MatchParent,
                AW.LinearLayout.LayoutParams.WrapContent);
            layoutParams.SetMargins(0, 5, 0, 5);
            
            _codesListView?.AddView(codeLayout, 0, layoutParams);
            
            _deviceInfoText!.Text = $"✓ Last scan: {barcode}";
            AW.Toast.MakeText(this, $"Scanned: {barcode}", AW.ToastLength.Short)?.Show();
        });
    }
    
    private async Task WriteCommand(byte[] data)
    {
        if (_bluetoothGatt == null || _writeCharacteristic == null)
        {
            Log.Error("InateckScanner", "[SDK] Cannot write: not connected");
            return;
        }
        
        var hexData = BitConverter.ToString(data).Replace("-", " ");
        Log.Info("InateckScanner", $"[SDK] Writing {data.Length} bytes: {hexData}");
        
        _writeCharacteristic.SetValue(data);
        _writeCharacteristic.WriteType = GattWriteType.Default;
        
        var success = _bluetoothGatt.WriteCharacteristic(_writeCharacteristic);
        Log.Info("InateckScanner", $"[SDK] Write initiated: {success}");
        
        // Small delay to allow the write to complete
        await Task.Delay(100);
    }
    
    private async Task OnDisconnectClick()
    {
        Log.Info("InateckScanner", "[SDK] Disconnecting...");
        
        try
        {
            _bluetoothGatt?.Disconnect();
            _bluetoothGatt?.Close();
            _bluetoothGatt = null;
            _writeCharacteristic = null;
            _notifyCharacteristic = null;
            OnDisconnected();
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[SDK] Disconnect error: {ex.Message}");
        }
    }
    
    private void UpdateStatus(string status)
    {
        RunOnUiThread(() =>
        {
            if (_statusText != null)
                _statusText.Text = status;
        });
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _bluetoothGatt?.Disconnect();
        _bluetoothGatt?.Close();
    }
    
    // ========================================
    // BLE Scan Callback
    // ========================================
    private class SdkScanCallback : Android.Bluetooth.LE.ScanCallback
    {
        private readonly SdkScannerActivity _activity;
        
        public SdkScanCallback(SdkScannerActivity activity)
        {
            _activity = activity;
        }
        
        public override void OnScanResult(Android.Bluetooth.LE.ScanCallbackType callbackType, 
            Android.Bluetooth.LE.ScanResult? result)
        {
            if (result?.Device != null)
            {
                var name = result.Device.Name ?? "(sin nombre)";
                var mac = result.Device.Address ?? "??";
                Log.Info("InateckScanner", $"[SDK-SCAN] BLE device: {name} - {mac}");
                _activity.OnDeviceFound(result.Device);
            }
        }
    }
    
    // ========================================
    // GATT Callback
    // ========================================
    private class SdkGattCallback : BluetoothGattCallback
    {
        private readonly SdkScannerActivity _activity;
        
        public SdkGattCallback(SdkScannerActivity activity)
        {
            _activity = activity;
        }
        
        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            Log.Info("InateckScanner", $"[SDK-GATT] Connection state: {newState}, status: {status}");
            
            if (newState == ProfileState.Connected)
            {
                _activity.OnConnected();
                gatt?.DiscoverServices();
            }
            else if (newState == ProfileState.Disconnected)
            {
                _activity.OnDisconnected();
            }
        }
        
        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
            Log.Info("InateckScanner", $"[SDK-GATT] Services discovered, status: {status}");
            
            if (status == GattStatus.Success && gatt != null)
            {
                _activity.OnServicesDiscovered(gatt);
            }
        }
        
        public override void OnCharacteristicChanged(BluetoothGatt? gatt, 
            BluetoothGattCharacteristic? characteristic)
        {
            Log.Info("InateckScanner", $"[SDK-GATT] OnCharacteristicChanged called!");
            var data = characteristic?.GetValue();
            if (data != null && data.Length > 0)
            {
                var hex = BitConverter.ToString(data).Replace("-", " ");
                Log.Info("InateckScanner", $"[SDK-GATT] Received {data.Length} bytes: {hex}");
                _activity.OnDataReceived(data);
            }
            else
            {
                Log.Warn("InateckScanner", "[SDK-GATT] OnCharacteristicChanged but data is null/empty");
            }
        }
        
        public override void OnCharacteristicWrite(BluetoothGatt? gatt,
            BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            Log.Info("InateckScanner", $"[SDK-GATT] Write completed, status: {status}");
        }
        
        public override void OnDescriptorWrite(BluetoothGatt? gatt,
            BluetoothGattDescriptor? descriptor, GattStatus status)
        {
            Log.Info("InateckScanner", $"[SDK-GATT] Descriptor write completed, status: {status}");
        }
    }
}
