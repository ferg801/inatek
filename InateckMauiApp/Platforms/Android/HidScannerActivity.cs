using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AW = Android.Widget;
using InateckMauiApp.Platforms.Android;
using InateckMauiApp.Services;
using Com.Inateck.Scanner.Ble;
using System.Linq;

namespace InateckMauiApp;

/// <summary>
/// HID Scanner Activity - Uses HID keyboard mode for barcode scanning.
/// This is the working implementation that captures scanner input as keyboard events.
/// </summary>
[Activity(
    Label = "HID Scanner",
    Theme = "@style/ScannerActivityTheme",
    MainLauncher = false,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                          ConfigChanges.Orientation |
                          ConfigChanges.UiMode |
                          ConfigChanges.ScreenLayout |
                          ConfigChanges.SmallestScreenSize |
                          ConfigChanges.Density)]
public class HidScannerActivity : Activity
{
    private const int BLUETOOTH_PERMISSION_REQUEST_CODE = 100;
    private IScannerService? _scannerService;
    private AW.TextView? _statusText;
    private AW.TextView? _connectionStatus;
    private AW.TextView? _deviceInfoText;
    private AW.Button? _scanButton;
    private AW.Button? _versionButton;
    private AW.Button? _batteryButton;
    private AW.Button? _disconnectButton;
    private AW.Button? _bluetoothSettingsButton;
    private AW.TextView? _hidStatusText;
    private AW.LinearLayout? _devicesLayout;
    private AW.LinearLayout? _codesListView;
    private List<string> _scannedBarcodes = new();
    private string? _connectedDeviceMac;
    private string? _lastConnectedMac; // Preserved for reconnection after returning from settings
    private bool _wasConnectedBeforePause = false;
    
    // HID Scanner input buffer
    private System.Text.StringBuilder _hidInputBuffer = new();
    private System.Timers.Timer? _hidInputTimer;
    private const int HID_INPUT_TIMEOUT_MS = 100; // Time to wait for complete barcode
    
    // Hidden EditText to capture HID keyboard input
    private AW.EditText? _hiddenInputField;
    
    // Visible EditText for manual keyboard input (not scanner)
    private AW.EditText? _manualInputField;
    
    // Flag to control HID capture - only when connected
    private bool _isConnected = false;
    private bool _isClearingHidField = false; // Prevent infinite loop when clearing
    
    // SharedPreferences for saving state across Activity recreations
    private const string PREFS_NAME = "InateckScannerPrefs";
    private const string KEY_CONNECTED_MAC = "connected_mac";
    private const string KEY_WAS_CONNECTED = "was_connected";
    private Android.Content.ISharedPreferences? _prefs;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Set window background to white to override MAUI splash theme
        Window?.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.White));
        
        // Initialize SharedPreferences for state persistence
        _prefs = GetSharedPreferences(PREFS_NAME, Android.Content.FileCreationMode.Private);
        
        // Restore saved state if Activity was recreated
        if (_prefs != null)
        {
            _lastConnectedMac = _prefs.GetString(KEY_CONNECTED_MAC, null);
            _wasConnectedBeforePause = _prefs.GetBoolean(KEY_WAS_CONNECTED, false);
            if (_wasConnectedBeforePause && !string.IsNullOrEmpty(_lastConnectedMac))
            {
                Log.Info("InateckScanner", $"[Lifecycle] OnCreate - Restored state: mac={_lastConnectedMac}");
            }
        }
        
        // Request Bluetooth permissions (Android 12+)
        RequestBluetoothPermissions();
        
        // Use real Bluetooth service instead of Mock
        _scannerService = new AndroidScannerService(this);
        
        // Subscribe to events
        _scannerService.StatusChanged += OnStatusChanged;
        _scannerService.DeviceDiscovered += OnDeviceDiscovered;
        _scannerService.ErrorOccurred += OnErrorOccurred;
        
        // Create main layout - use BeforeDescendants to allow manual input field to get focus
        // but prevent random focus changes from HID scanner
        var mainLayout = new AW.ScrollView(this);
        mainLayout.SetBackgroundColor(Android.Graphics.Color.White);
        mainLayout.Focusable = false;
        mainLayout.FocusableInTouchMode = false;
        mainLayout.DescendantFocusability = Android.Views.DescendantFocusability.BeforeDescendants;
        
        var scrollContent = new AW.LinearLayout(this)
        {
            Orientation = Android.Widget.Orientation.Vertical,
            Focusable = false,
            FocusableInTouchMode = false
        };
        scrollContent.SetPadding(20, 20, 20, 20);
        
        // Hidden EditText to capture HID keyboard input from scanner
        // Configure to NEVER show soft keyboard - we only want hardware HID input
        _hiddenInputField = new AW.EditText(this);
        _hiddenInputField.SetHeight(1);
        _hiddenInputField.SetWidth(1);
        _hiddenInputField.Alpha = 0.01f; // Almost invisible but still focusable
        _hiddenInputField.SetCursorVisible(false);
        _hiddenInputField.SetSingleLine(true);
        _hiddenInputField.ImeOptions = Android.Views.InputMethods.ImeAction.Done;
        // Use NULL input type to prevent soft keyboard from appearing
        _hiddenInputField.InputType = Android.Text.InputTypes.Null;
        _hiddenInputField.SetRawInputType(Android.Text.InputTypes.Null);
        _hiddenInputField.ShowSoftInputOnFocus = false;
        _hiddenInputField.Focusable = false; // Start disabled - only enable when connected
        _hiddenInputField.FocusableInTouchMode = false;
        _hiddenInputField.EditorAction += OnHiddenFieldEditorAction;
        _hiddenInputField.TextChanged += OnHiddenFieldTextChanged;
        scrollContent.AddView(_hiddenInputField);
        
        // Title
        var titleText = new AW.TextView(this)
        {
            Text = "Inatek Scanner",
            TextSize = 24
        };
        titleText.SetTextColor(Android.Graphics.Color.Black);
        titleText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        scrollContent.AddView(titleText);
        
        // Status display
        _statusText = new AW.TextView(this)
        {
            Text = "Ready",
            TextSize = 14
        };
        _statusText.SetTextColor(Android.Graphics.Color.DarkGray);
        var statusParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        statusParams.SetMargins(0, 20, 0, 20);
        scrollContent.AddView(_statusText, statusParams);
        
        // Scan button - not focusable to prevent HID input visual effects
        _scanButton = new AW.Button(this)
        {
            Text = "SCAN FOR DEVICES",
            Focusable = false,
            FocusableInTouchMode = false
        };
        _scanButton.Click += (s, e) => _ = OnScanClick();
        var scanParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        scanParams.SetMargins(0, 10, 0, 10);
        scrollContent.AddView(_scanButton, scanParams);
        
        // Connection status
        _connectionStatus = new AW.TextView(this)
        {
            Text = "Disconnected",
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
        var infoParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        infoParams.SetMargins(0, 0, 0, 10);
        scrollContent.AddView(_deviceInfoText, infoParams);
        
        // Version button - not focusable to prevent HID input visual effects
        _versionButton = new AW.Button(this)
        {
            Text = "GET VERSION",
            Enabled = false,
            Focusable = false,
            FocusableInTouchMode = false
        };
        _versionButton.Click += (s, e) => _ = OnGetVersionClick();
        scrollContent.AddView(_versionButton, scanParams);
        
        // Battery button - not focusable to prevent HID input visual effects
        _batteryButton = new AW.Button(this)
        {
            Text = "GET BATTERY",
            Enabled = false,
            Focusable = false,
            FocusableInTouchMode = false
        };
        _batteryButton.Click += (s, e) => _ = OnGetBatteryClick();
        scrollContent.AddView(_batteryButton, scanParams);
        
        // Disconnect button - not focusable to prevent HID input visual effects
        _disconnectButton = new AW.Button(this)
        {
            Text = "DISCONNECT",
            Enabled = false,
            Focusable = false,
            FocusableInTouchMode = false
        };
        _disconnectButton.Click += (s, e) => _ = OnDisconnectClick();
        var disconnectParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        disconnectParams.SetMargins(0, 10, 0, 10);
        scrollContent.AddView(_disconnectButton, disconnectParams);
        
        // HID Pairing status indicator
        _hidStatusText = new AW.TextView(this)
        {
            Text = "",
            TextSize = 13
        };
        _hidStatusText.SetTextColor(Android.Graphics.Color.DarkOrange);
        _hidStatusText.Visibility = Android.Views.ViewStates.Gone;
        var hidStatusParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        hidStatusParams.SetMargins(0, 0, 0, 5);
        scrollContent.AddView(_hidStatusText, hidStatusParams);
        
        // Bluetooth Settings button (hidden by default) - not focusable
        _bluetoothSettingsButton = new AW.Button(this)
        {
            Text = "📶 OPEN BLUETOOTH SETTINGS",
            Visibility = Android.Views.ViewStates.Gone,
            Focusable = false,
            FocusableInTouchMode = false
        };
        _bluetoothSettingsButton.Click += (s, e) => OpenBluetoothSettings();
        var btSettingsParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        btSettingsParams.SetMargins(0, 0, 0, 20);
        scrollContent.AddView(_bluetoothSettingsButton, btSettingsParams);
        
        // Manual input section - for keyboard entry only (not scanner)
        var manualInputLabel = new AW.TextView(this)
        {
            Text = "Manual Input (keyboard only):",
            TextSize = 14
        };
        manualInputLabel.SetTextColor(Android.Graphics.Color.Black);
        manualInputLabel.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        var manualInputLabelParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        manualInputLabelParams.SetMargins(0, 20, 0, 5);
        scrollContent.AddView(manualInputLabel, manualInputLabelParams);
        
        // Manual input field - accepts only keyboard input, not HID scanner
        _manualInputField = new AW.EditText(this);
        _manualInputField.Hint = "Type here with keyboard...";
        _manualInputField.SetSingleLine(true);
        _manualInputField.InputType = Android.Text.InputTypes.ClassText;
        _manualInputField.ImeOptions = Android.Views.InputMethods.ImeAction.Done;
        _manualInputField.SetBackgroundColor(Android.Graphics.Color.White);
        _manualInputField.SetTextColor(Android.Graphics.Color.Black);
        _manualInputField.SetHintTextColor(Android.Graphics.Color.Gray);
        _manualInputField.SetPadding(20, 15, 20, 15);
        // This field IS focusable - user can tap and type with soft keyboard
        _manualInputField.Focusable = true;
        _manualInputField.FocusableInTouchMode = true;
        // When this field gains focus, ensure HID goes to hidden field
        _manualInputField.FocusChange += OnManualInputFocusChange;
        _manualInputField.EditorAction += OnManualInputEditorAction;
        var manualInputParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        manualInputParams.SetMargins(0, 0, 0, 20);
        scrollContent.AddView(_manualInputField, manualInputParams);
        
        // Scanned codes header
        var codesHeaderText = new AW.TextView(this)
        {
            Text = "Scanned Barcodes:",
            TextSize = 14
        };
        codesHeaderText.SetTextColor(Android.Graphics.Color.Black);
        codesHeaderText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        var codesHeaderParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        codesHeaderParams.SetMargins(0, 10, 0, 10);
        scrollContent.AddView(codesHeaderText, codesHeaderParams);
        
        // Scanned codes list (empty initially) - non-focusable
        var codesListView = new AW.LinearLayout(this)
        {
            Orientation = Android.Widget.Orientation.Vertical,
            Focusable = false,
            FocusableInTouchMode = false
        };
        codesListView.SetBackgroundColor(Android.Graphics.Color.ParseColor("#FFFACD"));
        var codesListPadding = 10;
        codesListView.SetPadding(codesListPadding, codesListPadding, codesListPadding, codesListPadding);
        scrollContent.AddView(codesListView);
        
        // Store reference to update codes list
        this._codesListView = codesListView;
        
        // Devices header
        var devicesHeaderText = new AW.TextView(this)
        {
            Text = "Discovered Devices:",
            TextSize = 16,
            Focusable = false
        };
        devicesHeaderText.SetTextColor(Android.Graphics.Color.Black);
        devicesHeaderText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        var headerParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        headerParams.SetMargins(0, 20, 0, 10);
        scrollContent.AddView(devicesHeaderText, headerParams);
        
        // Devices container - non-focusable
        _devicesLayout = new AW.LinearLayout(this)
        {
            Orientation = Android.Widget.Orientation.Vertical,
            Focusable = false,
            FocusableInTouchMode = false
        };
        scrollContent.AddView(_devicesLayout);
        
        mainLayout.AddView(scrollContent);
        SetContentView(mainLayout);
    }
    
    protected override void OnPause()
    {
        base.OnPause();
        
        // Remember connection state before going to background using SharedPreferences
        _wasConnectedBeforePause = _isConnected;
        if (_isConnected && !string.IsNullOrEmpty(_connectedDeviceMac))
        {
            _lastConnectedMac = _connectedDeviceMac;
            
            // Save to SharedPreferences
            var editor = _prefs?.Edit();
            if (editor != null)
            {
                editor.PutString(KEY_CONNECTED_MAC, _connectedDeviceMac);
                editor.PutBoolean(KEY_WAS_CONNECTED, true);
                editor.Apply();
                Log.Info("InateckScanner", $"[Lifecycle] OnPause - Saved connection to {_connectedDeviceMac}");
            }
        }
    }
    
    protected override void OnResume()
    {
        base.OnResume();
        Log.Info("InateckScanner", $"[Lifecycle] OnResume - wasConnected={_wasConnectedBeforePause}, lastMac={_lastConnectedMac}");
        
        // Check if we were connected before going to background
        if (_wasConnectedBeforePause && !string.IsNullOrEmpty(_lastConnectedMac))
        {
            // Check if connection is still active
            var androidService = _scannerService as AndroidScannerService;
            if (androidService != null && !androidService.IsConnected)
            {
                Log.Info("InateckScanner", "[Lifecycle] Connection lost while in background, attempting reconnection...");
                _ = ReconnectToDeviceAsync(_lastConnectedMac);
            }
            else if (_isConnected)
            {
                // Connection still active, just update status and refocus
                UpdateHidPairingStatus(_connectedDeviceMac!);
                if (_hiddenInputField != null)
                {
                    _hiddenInputField.RequestFocus();
                }
            }
        }
        else if (_isConnected && !string.IsNullOrEmpty(_connectedDeviceMac))
        {
            // Normal resume with active connection
            UpdateHidPairingStatus(_connectedDeviceMac);
            if (_hiddenInputField != null)
            {
                _hiddenInputField.RequestFocus();
            }
        }
    }
    
    private async Task ReconnectToDeviceAsync(string mac)
    {
        try
        {
            RunOnUiThread(() =>
            {
                _connectionStatus!.Text = "Reconnecting...";
                _connectionStatus.SetTextColor(Android.Graphics.Color.ParseColor("#FFA500")); // Orange
            });
            
            Log.Info("InateckScanner", $"[Lifecycle] Reconnecting to {mac}...");
            bool success = await _scannerService!.ConnectAsync(mac);
            
            if (success)
            {
                Log.Info("InateckScanner", "[Lifecycle] ✓ Reconnection successful");
                
                var androidService = _scannerService as AndroidScannerService;
                if (androidService != null)
                {
                    androidService.SetCurrentConnectedMac(mac);
                }
                
                RunOnUiThread(() =>
                {
                    _connectionStatus!.Text = $"✓ Reconnected";
                    _connectionStatus.SetTextColor(Android.Graphics.Color.Green);
                    _deviceInfoText!.Text = "";
                    _versionButton!.Enabled = true;
                    _batteryButton!.Enabled = true;
                    _disconnectButton!.Enabled = true;
                    _isConnected = true;
                    _connectedDeviceMac = mac;
                    
                    if (_hiddenInputField != null)
                    {
                        _hiddenInputField.Focusable = true;
                        _hiddenInputField.FocusableInTouchMode = true;
                        _hiddenInputField.RequestFocus();
                    }
                });
                
                UpdateHidPairingStatus(mac);
            }
            else
            {
                Log.Warn("InateckScanner", "[Lifecycle] ✗ Reconnection failed");
                RunOnUiThread(() =>
                {
                    _connectionStatus!.Text = "Connection lost";
                    _connectionStatus.SetTextColor(Android.Graphics.Color.Red);
                    _isConnected = false;
                    _connectedDeviceMac = null;
                    _versionButton!.Enabled = false;
                    _batteryButton!.Enabled = false;
                    _disconnectButton!.Enabled = false;
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[Lifecycle] Reconnection error: {ex.Message}");
            RunOnUiThread(() =>
            {
                _connectionStatus!.Text = "Connection error";
                _connectionStatus.SetTextColor(Android.Graphics.Color.Red);
                _isConnected = false;
            });
        }
        finally
        {
            _wasConnectedBeforePause = false;
        }
    }

    private async Task OnScanClick()
    {
        _scanButton!.Enabled = false;
        _devicesLayout!.RemoveAllViews();
        RunOnUiThread(() => _statusText!.Text = "Scanning...");
        await _scannerService!.ScanForDevicesAsync(10);
        RunOnUiThread(() => _statusText!.Text = $"Found {_scannerService.DiscoveredDevices.Count} devices");
        _scanButton.Enabled = true;
    }

    private void OnStatusChanged(object? sender, string status)
    {
        RunOnUiThread(() =>
        {
            if (_statusText != null)
                _statusText.Text = status;
        });
    }

    private void OnDeviceDiscovered(object? sender, ScannerDeviceInfo device)
    {
        RunOnUiThread(() =>
        {
            if (_devicesLayout != null)
            {
                var deviceView = new AW.LinearLayout(this)
                {
                    Orientation = Android.Widget.Orientation.Vertical,
                    Focusable = false,
                    FocusableInTouchMode = false
                };
                deviceView.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F0F0F0"));
                var padding = 15;
                deviceView.SetPadding(padding, padding, padding, padding);
                
                var nameText = new AW.TextView(this)
                {
                    Text = $"📱 {device.Name}",
                    TextSize = 14,
                    Focusable = false
                };
                nameText.SetTextColor(Android.Graphics.Color.Black);
                nameText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
                deviceView.AddView(nameText);
                
                var macText = new AW.TextView(this)
                {
                    Text = $"MAC: {device.Mac}",
                    TextSize = 12,
                    Focusable = false
                };
                macText.SetTextColor(Android.Graphics.Color.DarkGray);
                deviceView.AddView(macText);
                
                var rssiText = new AW.TextView(this)
                {
                    Text = $"Signal: {device.Rssi} dBm",
                    TextSize = 12,
                    Focusable = false
                };
                rssiText.SetTextColor(Android.Graphics.Color.DarkGray);
                deviceView.AddView(rssiText);
                
                var clickText = new AW.TextView(this)
                {
                    Text = "👆 Tap to connect",
                    TextSize = 11,
                    Focusable = false
                };
                clickText.SetTextColor(Android.Graphics.Color.Blue);
                clickText.SetTypeface(null, Android.Graphics.TypefaceStyle.Italic);
                deviceView.AddView(clickText);
                
                var params2 = new AW.LinearLayout.LayoutParams(
                    AW.LinearLayout.LayoutParams.MatchParent,
                    AW.LinearLayout.LayoutParams.WrapContent);
                params2.SetMargins(0, 0, 0, 10);
                
                // Make device clickable to connect (touch only, not focusable)
                deviceView.Clickable = true;
                deviceView.Click += async (s, e) => await OnDeviceClick(device);
                
                _devicesLayout.AddView(deviceView, params2);
            }
        });
    }

    private async Task OnDeviceClick(ScannerDeviceInfo device)
    {
        try
        {
            Log.Info("InateckScanner", $"[MainActivity] OnDeviceClick: START - Device: {device.Name} ({device.Mac})");
            RunOnUiThread(() => _connectionStatus!.Text = $"Connecting to {device.Name}...");
            
            Log.Info("InateckScanner", $"[MainActivity] About to call ConnectAsync for MAC: {device.Mac}");
            bool success = await _scannerService!.ConnectAsync(device.Mac);
            Log.Info("InateckScanner", $"[MainActivity] ConnectAsync returned: {success}");
            
            if (success)
            {
                Log.Info("InateckScanner", $"[MainActivity] ✓ Connection successful, storing MAC");
                
                try
                {
                    var androidService = _scannerService as AndroidScannerService;
                    Log.Info("InateckScanner", $"[MainActivity] AndroidScannerService cast result: {(androidService != null ? "SUCCESS" : "NULL")}");
                    
                    if (androidService != null)
                    {
                        Log.Info("InateckScanner", $"[MainActivity] Calling SetCurrentConnectedMac with MAC: {device.Mac}");
                        androidService.SetCurrentConnectedMac(device.Mac);
                        Log.Info("InateckScanner", $"[MainActivity] ✓ SetCurrentConnectedMac completed");
                    }
                    else
                    {
                        Log.Error("InateckScanner", "[MainActivity] ✗ AndroidScannerService is NULL!");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("InateckScanner", $"[MainActivity] Exception calling SetCurrentConnectedMac: {ex.Message}");
                }

                RunOnUiThread(() =>
                {
                    _connectionStatus!.Text = $"✓ Connected: {device.Name}";
                    _connectionStatus.SetTextColor(Android.Graphics.Color.Green);
                    _deviceInfoText!.Text = "";
                    _versionButton!.Enabled = true;
                    _batteryButton!.Enabled = true;
                    _disconnectButton!.Enabled = true;
                    _isConnected = true;
                    _connectedDeviceMac = device.Mac;
                    
                    // Enable and focus hidden input field to capture HID keyboard input
                    if (_hiddenInputField != null)
                    {
                        _hiddenInputField.Focusable = true;
                        _hiddenInputField.FocusableInTouchMode = true;
                        _hiddenInputField.RequestFocus();
                    }
                    Log.Info("InateckScanner", "[HID] Hidden input field focused for HID capture");
                });
                
                // Check if device is paired in system for HID scanning
                UpdateHidPairingStatus(device.Mac);
            }
            else
            {
                Log.Error("InateckScanner", $"[MainActivity] ✗ ConnectAsync failed for {device.Mac}");
                RunOnUiThread(() =>
                {
                    _connectionStatus!.Text = "Failed to connect";
                    _connectionStatus.SetTextColor(Android.Graphics.Color.Red);
                    _versionButton!.Enabled = false;
                    _batteryButton!.Enabled = false;
                    _disconnectButton!.Enabled = false;
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[MainActivity] OnDeviceClick EXCEPTION: {ex.Message}");
        }
    }

    private async Task OnGetVersionClick()
    {
        if (_scannerService?.ConnectedDeviceName == null)
        {
            RunOnUiThread(() => _deviceInfoText!.Text = "No device connected");
            return;
        }
        
        RunOnUiThread(() => _deviceInfoText!.Text = "Getting version...");
        string? version = await _scannerService.GetDeviceVersionAsync();
        RunOnUiThread(() => _deviceInfoText!.Text = $"📦 Version: {version}");
    }

    private async Task OnGetBatteryClick()
    {
        if (_scannerService?.ConnectedDeviceName == null)
        {
            RunOnUiThread(() => _deviceInfoText!.Text = "No device connected");
            return;
        }
        
        RunOnUiThread(() => _deviceInfoText!.Text = "Getting battery...");
        string? battery = await _scannerService.GetBatteryInfoAsync();
        RunOnUiThread(() => _deviceInfoText!.Text = $"🔋 Battery: {battery}");
    }

    private async Task OnDisconnectClick()
    {
        _isConnected = false; // Stop HID capture immediately
        _hidInputBuffer.Clear(); // Clear any pending input
        _hidInputTimer?.Stop(); // Stop timer
        
        // Clear SharedPreferences - user intentionally disconnected
        var editor = _prefs?.Edit();
        if (editor != null)
        {
            editor.Remove(KEY_CONNECTED_MAC);
            editor.PutBoolean(KEY_WAS_CONNECTED, false);
            editor.Apply();
        }
        _lastConnectedMac = null;
        _wasConnectedBeforePause = false;
        
        // Disable and clear the hidden input field
        if (_hiddenInputField != null)
        {
            _isClearingHidField = true;
            _hiddenInputField.ClearFocus();
            _hiddenInputField.Focusable = false;
            _hiddenInputField.FocusableInTouchMode = false;
            _hiddenInputField.SetText("", AW.TextView.BufferType.Editable);
            _isClearingHidField = false;
        }
        
        RunOnUiThread(() => _connectionStatus!.Text = "Disconnecting...");
        await _scannerService!.DisconnectAsync();
        
        RunOnUiThread(() =>
        {
            _connectionStatus!.Text = "Disconnected";
            _connectionStatus.SetTextColor(Android.Graphics.Color.Red);
            _deviceInfoText!.Text = "";
            _versionButton!.Enabled = false;
            _batteryButton!.Enabled = false;
            _disconnectButton!.Enabled = false;
            _connectedDeviceMac = null;
            
            // Hide HID pairing status
            _hidStatusText!.Visibility = Android.Views.ViewStates.Gone;
            _bluetoothSettingsButton!.Visibility = Android.Views.ViewStates.Gone;
        });
    }
    
    private void AddBarcodeToList(string barcode)
    {
        if (_codesListView == null) return;
        
        var barcodeView = new AW.LinearLayout(this)
        {
            Orientation = Android.Widget.Orientation.Horizontal
        };
        barcodeView.SetBackgroundColor(Android.Graphics.Color.White);
        var barcodeViewParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        barcodeViewParams.SetMargins(0, 5, 0, 5);
        
        var barcodeText = new AW.TextView(this)
        {
            Text = barcode,
            TextSize = 12
        };
        barcodeText.SetTextColor(Android.Graphics.Color.Black);
        var textParams = new AW.LinearLayout.LayoutParams(
            0,
            AW.LinearLayout.LayoutParams.WrapContent,
            1);
        barcodeText.LayoutParameters = textParams;
        barcodeView.AddView(barcodeText);
        
        _codesListView.AddView(barcodeView, barcodeViewParams);
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        RunOnUiThread(() =>
        {
            if (_statusText != null)
                _statusText.Text = $"ERROR: {error}";
        });
    }

    private void RequestBluetoothPermissions()
    {
        // For Android 12+, we only need Bluetooth permissions (no location required)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            var permissions = new string[]
            {
                Android.Manifest.Permission.BluetoothScan,
                Android.Manifest.Permission.BluetoothConnect
                // Location NOT required for Android 12+ with neverForLocation flag
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
        // For Android 11 and below, location permissions are required for BLE scanning
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            var locationPermission = Android.Manifest.Permission.AccessFineLocation;
            if (ContextCompat.CheckSelfPermission(this, locationPermission) 
                != Android.Content.PM.Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, 
                    new[] { locationPermission }, 
                    BLUETOOTH_PERMISSION_REQUEST_CODE);
            }
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == BLUETOOTH_PERMISSION_REQUEST_CODE)
        {
            bool allGranted = grantResults.All(result => result == Android.Content.PM.Permission.Granted);
            
            if (allGranted)
            {
                RunOnUiThread(() => 
                {
                    if (_statusText != null)
                        _statusText.Text = "✓ Bluetooth permissions granted";
                });
            }
            else
            {
                RunOnUiThread(() => 
                {
                    if (_statusText != null)
                        _statusText.Text = "✗ Bluetooth permissions denied";
                });
            }
        }
    }

    /// <summary>
    /// Called when text changes in the hidden input field (HID scanner types here)
    /// </summary>
    private void OnHiddenFieldTextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        // Prevent infinite loop when clearing field
        if (_isClearingHidField) return;
        
        // Only process when connected
        if (!_isConnected) 
        {
            // Silently ignore - field should be disabled anyway
            return;
        }
        
        if (_hiddenInputField == null) return;
        
        string text = _hiddenInputField.Text ?? "";
        if (!string.IsNullOrEmpty(text))
        {
            Log.Debug("InateckScanner", $"[HID-FIELD] Text changed: '{text}'");
            ResetHidInputTimer();
        }
    }

    /// <summary>
    /// Called when Enter/Done is pressed in the hidden input field
    /// </summary>
    private void OnHiddenFieldEditorAction(object? sender, AW.TextView.EditorActionEventArgs e)
    {
        // Only process when scanner is connected
        if (!_isConnected)
            return;
            
        if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || 
            e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
            e.ActionId == Android.Views.InputMethods.ImeAction.Go)
        {
            string barcode = _hiddenInputField?.Text?.Trim() ?? "";
            Log.Info("InateckScanner", $"[HID-FIELD] EditorAction with barcode: '{barcode}'");
            
            if (!string.IsNullOrEmpty(barcode))
            {
                ProcessHidBarcodeFromField(barcode);
            }
            
            e.Handled = true;
        }
    }

    /// <summary>
    /// Process barcode captured from hidden EditText field
    /// </summary>
    private void ProcessHidBarcodeFromField(string barcode)
    {
        // Only process when connected
        if (!_isConnected)
            return;
        
        if (string.IsNullOrEmpty(barcode))
            return;
            
        Log.Info("InateckScanner", $"[HID-FIELD] ✓ Barcode received: {barcode}");
        
        // Clear the hidden field with flag to prevent loop
        _isClearingHidField = true;
        _hiddenInputField?.SetText("", AW.TextView.BufferType.Editable);
        _isClearingHidField = false;
        
        // Add to list
        _scannedBarcodes.Add(barcode);
        
        RunOnUiThread(() =>
        {
            _deviceInfoText!.Text = $"✓ Scanned (HID): {barcode}";
            AddBarcodeToList(barcode);
            AW.Toast.MakeText(this, $"Scanned: {barcode}", AW.ToastLength.Short)?.Show();
        });
    }

    /// <summary>
    /// Called when the manual input field gains or loses focus.
    /// When it has focus, we let the soft keyboard appear for manual typing.
    /// HID scanner input still goes to the hidden field via DispatchKeyEvent.
    /// </summary>
    private void OnManualInputFocusChange(object? sender, Android.Views.View.FocusChangeEventArgs e)
    {
        if (e.HasFocus)
        {
            Log.Debug("InateckScanner", "[ManualInput] Field gained focus - soft keyboard can appear");
            // Show soft keyboard for this field
            var imm = (Android.Views.InputMethods.InputMethodManager?)GetSystemService(Android.Content.Context.InputMethodService);
            imm?.ShowSoftInput(_manualInputField, Android.Views.InputMethods.ShowFlags.Implicit);
        }
        else
        {
            Log.Debug("InateckScanner", "[ManualInput] Field lost focus");
            // When focus is lost, if connected, refocus the hidden HID field
            if (_isConnected && _hiddenInputField != null)
            {
                _hiddenInputField.RequestFocus();
            }
        }
    }

    /// <summary>
    /// Called when user presses Done/Enter on soft keyboard in manual input field.
    /// </summary>
    private void OnManualInputEditorAction(object? sender, AW.TextView.EditorActionEventArgs e)
    {
        if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || 
            e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
            e.ActionId == Android.Views.InputMethods.ImeAction.Go)
        {
            string manualText = _manualInputField?.Text?.Trim() ?? "";
            Log.Info("InateckScanner", $"[ManualInput] User entered: '{manualText}'");
            
            if (!string.IsNullOrEmpty(manualText))
            {
                // Process manual input (you can customize this behavior)
                RunOnUiThread(() =>
                {
                    _deviceInfoText!.Text = $"✓ Manual input: {manualText}";
                    AW.Toast.MakeText(this, $"Manual: {manualText}", AW.ToastLength.Short)?.Show();
                });
                
                // Clear the field after processing
                _manualInputField?.SetText("", AW.TextView.BufferType.Editable);
            }
            
            // Hide keyboard
            var imm = (Android.Views.InputMethods.InputMethodManager?)GetSystemService(Android.Content.Context.InputMethodService);
            imm?.HideSoftInputFromWindow(_manualInputField?.WindowToken, 0);
            
            // Return focus to hidden field for HID scanning (if connected)
            if (_isConnected && _hiddenInputField != null)
            {
                _hiddenInputField.RequestFocus();
            }
            
            e.Handled = true;
        }
    }

    /// <summary>
    /// Override to capture HID keyboard input from scanner
    /// When scanner is in HID mode, it sends scanned data as keyboard key presses
    /// IMPORTANT: HID input from scanner (DeviceId > 0) is always captured here,
    /// even if the manual input field has focus. This ensures scanner data goes
    /// to the hidden field, while soft keyboard input goes to manual field.
    /// </summary>
    public override bool DispatchKeyEvent(Android.Views.KeyEvent? e)
    {
        if (e == null)
            return base.DispatchKeyEvent(e);

        // Only process key down events
        if (e.Action != Android.Views.KeyEventActions.Down)
            return base.DispatchKeyEvent(e);

        // Check if this is from an external device (like scanner)
        // DeviceId 0 is usually the built-in virtual keyboard, > 0 are external devices
        bool isExternalDevice = e.DeviceId > 0;
        
        // Check if manual input field has focus
        bool manualFieldHasFocus = _manualInputField?.HasFocus ?? false;
        
        // If soft keyboard input (DeviceId 0) and manual field has focus, let it through
        if (!isExternalDevice && manualFieldHasFocus)
        {
            // Let soft keyboard input go to manual field normally
            return base.DispatchKeyEvent(e);
        }
        
        // If it's an external device sending alphanumeric keys, consume them
        // to prevent them from triggering UI buttons when not connected
        if (isExternalDevice)
        {
            bool isAlphanumeric = IsAlphanumericKey(e.KeyCode);
            bool isEnter = e.KeyCode == Android.Views.Keycode.Enter;
            
            // If not connected to scanner in our app, consume external keyboard input
            // to prevent it from interacting with UI elements
            if (!_isConnected)
            {
                if (isAlphanumeric || isEnter)
                {
                    Log.Debug("InateckScanner", $"[HID] Ignoring external key (not connected): {e.KeyCode}");
                    return true; // Consume the event, don't let it interact with UI
                }
                return base.DispatchKeyEvent(e);
            }
            
            // Connected - process the barcode input
            int keyCode = (int)e.KeyCode;
            Log.Debug("InateckScanner", $"[HID] KeyEvent: KeyCode={e.KeyCode}, Char={e.UnicodeChar}, DeviceId={e.DeviceId}");

            // Handle Enter key - this marks end of barcode
            if (isEnter)
            {
                string fieldBarcode = _hiddenInputField?.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(fieldBarcode))
                {
                    ProcessHidBarcodeFromField(fieldBarcode);
                    return true;
                }
                
                ProcessHidBarcode();
                return true;
            }

            // Convert keycode to character
            char? c = KeyCodeToChar(e);
            if (c.HasValue)
            {
                _hidInputBuffer.Append(c.Value);
                Log.Debug("InateckScanner", $"[HID] Buffer: {_hidInputBuffer}");
                ResetHidInputTimer();
                return true;
            }
        }

        return base.DispatchKeyEvent(e);
    }
    
    /// <summary>
    /// Check if a keycode is alphanumeric (letters, numbers, common barcode characters)
    /// </summary>
    private bool IsAlphanumericKey(Android.Views.Keycode keyCode)
    {
        // Letters A-Z
        if (keyCode >= Android.Views.Keycode.A && keyCode <= Android.Views.Keycode.Z)
            return true;
        // Numbers 0-9
        if (keyCode >= Android.Views.Keycode.Num0 && keyCode <= Android.Views.Keycode.Num9)
            return true;
        // Common barcode characters
        if (keyCode == Android.Views.Keycode.Minus || 
            keyCode == Android.Views.Keycode.Period ||
            keyCode == Android.Views.Keycode.Slash ||
            keyCode == Android.Views.Keycode.Space)
            return true;
        return false;
    }

    private char? KeyCodeToChar(Android.Views.KeyEvent e)
    {
        // Get the unicode character from the key event
        int unicodeChar = e.UnicodeChar;
        if (unicodeChar > 0 && unicodeChar < 65536)
        {
            return (char)unicodeChar;
        }

        // Fallback for common keys
        var keyCode = e.KeyCode;
        
        // Numbers 0-9
        if (keyCode >= Android.Views.Keycode.Num0 && keyCode <= Android.Views.Keycode.Num9)
        {
            return (char)('0' + (keyCode - Android.Views.Keycode.Num0));
        }
        
        // Letters A-Z
        if (keyCode >= Android.Views.Keycode.A && keyCode <= Android.Views.Keycode.Z)
        {
            bool isShift = e.IsShiftPressed;
            char c = (char)('a' + (keyCode - Android.Views.Keycode.A));
            return isShift ? char.ToUpper(c) : c;
        }

        // Common symbols
        switch (keyCode)
        {
            case Android.Views.Keycode.Space: return ' ';
            case Android.Views.Keycode.Minus: return '-';
            case Android.Views.Keycode.Period: return '.';
            case Android.Views.Keycode.Comma: return ',';
            case Android.Views.Keycode.Slash: return '/';
            case Android.Views.Keycode.Backslash: return '\\';
            case Android.Views.Keycode.LeftBracket: return '[';
            case Android.Views.Keycode.RightBracket: return ']';
            case Android.Views.Keycode.Semicolon: return ';';
            case Android.Views.Keycode.Apostrophe: return '\'';
            case Android.Views.Keycode.Equals: return '=';
            case Android.Views.Keycode.Plus: return '+';
            case Android.Views.Keycode.Star: return '*';
            case Android.Views.Keycode.Pound: return '#';
            case Android.Views.Keycode.At: return '@';
        }

        return null;
    }

    private void ResetHidInputTimer()
    {
        _hidInputTimer?.Stop();
        _hidInputTimer?.Dispose();
        
        _hidInputTimer = new System.Timers.Timer(HID_INPUT_TIMEOUT_MS);
        _hidInputTimer.AutoReset = false;
        _hidInputTimer.Elapsed += (s, e) =>
        {
            RunOnUiThread(() => 
            {
                // Try hidden field first
                string fieldBarcode = _hiddenInputField?.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(fieldBarcode))
                {
                    ProcessHidBarcodeFromField(fieldBarcode);
                }
                else
                {
                    ProcessHidBarcode();
                }
            });
        };
        _hidInputTimer.Start();
    }

    private void ProcessHidBarcode()
    {
        _hidInputTimer?.Stop();
        
        string barcode = _hidInputBuffer.ToString().Trim();
        _hidInputBuffer.Clear();

        if (string.IsNullOrEmpty(barcode))
            return;

        Log.Info("InateckScanner", $"[HID] ✓ Barcode received: {barcode}");
        
        // Add to list
        _scannedBarcodes.Add(barcode);
        
        RunOnUiThread(() =>
        {
            _deviceInfoText!.Text = $"✓ Scanned (HID): {barcode}";
            AddBarcodeToList(barcode);
            AW.Toast.MakeText(this, $"Scanned: {barcode}", AW.ToastLength.Short)?.Show();
        });
    }
    
    /// <summary>
    /// Check if the device is paired/bonded in the system Bluetooth settings
    /// </summary>
    private bool IsDevicePairedInSystem(string macAddress)
    {
        try
        {
            var bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (bluetoothAdapter == null)
            {
                Log.Warn("InateckScanner", "[HID] No Bluetooth adapter found");
                return false;
            }
            
            var bondedDevices = bluetoothAdapter.BondedDevices;
            if (bondedDevices == null)
            {
                Log.Warn("InateckScanner", "[HID] No bonded devices list");
                return false;
            }
            
            // Normalize MAC address for comparison
            string normalizedMac = macAddress.ToUpperInvariant().Replace("-", ":");
            
            foreach (var device in bondedDevices)
            {
                string? deviceMac = device?.Address?.ToUpperInvariant();
                if (deviceMac == normalizedMac)
                {
                    Log.Info("InateckScanner", $"[HID] ✓ Device {macAddress} is PAIRED in system");
                    return true;
                }
            }
            
            Log.Info("InateckScanner", $"[HID] Device {macAddress} is NOT paired in system. Bonded count: {bondedDevices.Count}");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[HID] Error checking paired status: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Check if the device is connected as HID (Input Device) in the system
    /// </summary>
    private bool IsDeviceConnectedAsHid(string macAddress)
    {
        try
        {
            var bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            if (bluetoothAdapter == null)
                return false;
            
            // Normalize MAC address
            string normalizedMac = macAddress.ToUpperInvariant().Replace("-", ":");
            
            // Find the BluetoothDevice
            BluetoothDevice? targetDevice = null;
            var bondedDevices = bluetoothAdapter.BondedDevices;
            if (bondedDevices != null)
            {
                foreach (var device in bondedDevices)
                {
                    if (device?.Address?.ToUpperInvariant() == normalizedMac)
                    {
                        targetDevice = device;
                        break;
                    }
                }
            }
            
            if (targetDevice == null)
            {
                Log.Info("InateckScanner", $"[HID] Device {macAddress} not found in bonded devices");
                return false;
            }
            
            // Check if device is connected using reflection (for HID profile)
            // BluetoothDevice.IsConnected is hidden API, try using BondState as proxy
            var bondState = targetDevice.BondState;
            Log.Info("InateckScanner", $"[HID] Device {macAddress} bond state: {bondState}");
            
            // We can't directly check HID connection status without BluetoothHidHost profile
            // which requires system permissions. Best we can do is check if it's bonded.
            // The actual HID connection is managed by the Android system.
            
            return bondState == Bond.Bonded;
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[HID] Error checking HID connection: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Check if a Bluetooth device is connected as HID keyboard in the system
    /// Uses InputManager to check connected input devices
    /// </summary>
    private bool IsDeviceConnectedInSystem(string macAddress)
    {
        try
        {
            // Method 1: Check InputManager for connected input devices
            var inputManager = (Android.Hardware.Input.InputManager?)GetSystemService(InputService);
            if (inputManager != null)
            {
                var inputDeviceIds = inputManager.GetInputDeviceIds();
                if (inputDeviceIds != null)
                {
                    foreach (var deviceId in inputDeviceIds)
                    {
                        var inputDevice = inputManager.GetInputDevice(deviceId);
                        if (inputDevice != null)
                        {
                            // Check if it's an external Bluetooth device
                            string? descriptor = inputDevice.Descriptor;
                            string? deviceName = inputDevice.Name;
                            bool isExternal = inputDevice.IsExternal;
                            
                            Log.Debug("InateckScanner", $"[HID] Input device: {deviceName}, External: {isExternal}, Descriptor: {descriptor}");
                            
                            // Check if this is our scanner by name match
                            if (isExternal && !string.IsNullOrEmpty(deviceName))
                            {
                                // Scanner name contains "BCST" or matches our device
                                if (deviceName.Contains("BCST", StringComparison.OrdinalIgnoreCase) ||
                                    deviceName.Contains("Inateck", StringComparison.OrdinalIgnoreCase) ||
                                    deviceName.Contains("Scanner", StringComparison.OrdinalIgnoreCase))
                                {
                                    Log.Info("InateckScanner", $"[HID] ✓ Found connected scanner input device: {deviceName}");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            
            // Method 2: Fallback - Check BluetoothDevice connection state via profiles
            var bluetoothManager = (BluetoothManager?)GetSystemService(BluetoothService);
            if (bluetoothManager != null)
            {
                var bluetoothAdapter = bluetoothManager.Adapter;
                if (bluetoothAdapter != null)
                {
                    string normalizedMac = macAddress.ToUpperInvariant().Replace("-", ":");
                    
                    var bondedDevices = bluetoothAdapter.BondedDevices;
                    if (bondedDevices != null)
                    {
                        foreach (var device in bondedDevices)
                        {
                            if (device?.Address?.ToUpperInvariant() == normalizedMac)
                            {
                                // Try A2DP profile as proxy for "active connection"
                                // If device shows as connected on any profile, it might work
                                try
                                {
                                    var a2dpState = bluetoothManager.GetConnectionState(device, ProfileType.A2dp);
                                    var headsetState = bluetoothManager.GetConnectionState(device, ProfileType.Headset);
                                    
                                    Log.Debug("InateckScanner", $"[HID] Device {macAddress} A2DP: {a2dpState}, Headset: {headsetState}");
                                    
                                    // These won't apply to HID devices but worth checking
                                }
                                catch (Exception ex)
                                {
                                    Log.Debug("InateckScanner", $"[HID] Profile check: {ex.Message}");
                                }
                                break;
                            }
                        }
                    }
                }
            }
            
            Log.Info("InateckScanner", $"[HID] Device {macAddress} is NOT detected as connected HID keyboard");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[HID] Error checking connection status: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Update the HID pairing status UI
    /// </summary>
    private void UpdateHidPairingStatus(string macAddress)
    {
        bool isPaired = IsDevicePairedInSystem(macAddress);
        bool isConnected = IsDeviceConnectedInSystem(macAddress);
        
        Log.Info("InateckScanner", $"[HID] Status check - Paired: {isPaired}, Connected: {isConnected}");
        
        RunOnUiThread(() =>
        {
            if (isConnected)
            {
                // Device is connected - ready to scan
                _hidStatusText!.Text = "✓ Scanner connected - Ready to scan";
                _hidStatusText.SetTextColor(Android.Graphics.Color.DarkGreen);
                _hidStatusText.Visibility = Android.Views.ViewStates.Visible;
                _bluetoothSettingsButton!.Visibility = Android.Views.ViewStates.Gone;
            }
            else if (isPaired)
            {
                // Device is paired but not connected - needs connection
                _hidStatusText!.Text = "⚠️ Scanner paired but disconnected - Connect in Bluetooth settings to scan";
                _hidStatusText.SetTextColor(Android.Graphics.Color.DarkOrange);
                _hidStatusText.Visibility = Android.Views.ViewStates.Visible;
                _bluetoothSettingsButton!.Visibility = Android.Views.ViewStates.Visible;
                _bluetoothSettingsButton.Text = "📶 CONNECT IN BLUETOOTH SETTINGS";
            }
            else
            {
                // Device NOT paired - needs to be paired first
                _hidStatusText!.Text = "⚠️ Scanner not paired - Pair in Bluetooth settings to scan";
                _hidStatusText.SetTextColor(Android.Graphics.Color.Red);
                _hidStatusText.Visibility = Android.Views.ViewStates.Visible;
                _bluetoothSettingsButton!.Visibility = Android.Views.ViewStates.Visible;
                _bluetoothSettingsButton.Text = "📶 OPEN BLUETOOTH SETTINGS";
            }
        });
    }
    
    /// <summary>
    /// Open Android Bluetooth settings
    /// </summary>
    private void OpenBluetoothSettings()
    {
        try
        {
            var intent = new Intent(Settings.ActionBluetoothSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            StartActivity(intent);
        }
        catch (Exception ex)
        {
            Log.Error("InateckScanner", $"[MainActivity] Error opening Bluetooth settings: {ex.Message}");
            AW.Toast.MakeText(this, "Could not open Bluetooth settings", AW.ToastLength.Short)?.Show();
        }
    }
}
