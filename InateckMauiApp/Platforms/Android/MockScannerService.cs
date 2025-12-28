using InateckMauiApp.Services;
using System.Collections.Generic;
using System.Linq;

namespace InateckMauiApp.Platforms.Android
{
    /// <summary>
    /// Implementación de Mock del IScannerService para testing.
    /// Esta versión simula el comportamiento del escáner sin dependencias Kotlin.
    /// </summary>
    public class MockScannerService : IScannerService
    {
        public event EventHandler<ScannerDeviceInfo>? DeviceDiscovered;
        public event EventHandler<string>? DataReceived;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<int>? BatteryLevelChanged;

        public bool IsInitialized { get; private set; } = true;
        public bool IsConnected => _connectedDevices.Count > 0;
        public string? ConnectedDeviceName => _currentDevice?.Name;
        public string? ConnectedDeviceMac => _currentDevice?.Mac;
        public IReadOnlyList<ScannerDeviceInfo> DiscoveredDevices => _discoveredDevices.AsReadOnly();

        private List<ScannerDeviceInfo> _discoveredDevices = new();
        private List<string> _connectedDevices = new();
        private ScannerDeviceInfo? _currentDevice;

        public MockScannerService()
        {
            StatusChanged?.Invoke(this, "Mock Scanner Initialized");
        }

        public Task<bool> InitializeAsync()
        {
            return Task.FromResult(true);
        }

        public Task<bool> VerifyAndSyncStateAsync()
        {
            // En Mock, simplemente retornar estado actual
            return Task.FromResult(IsConnected);
        }

        public async Task<List<ScannerDeviceInfo>> ScanForDevicesAsync(int durationSeconds = 10)
        {
            _discoveredDevices.Clear();
            StatusChanged?.Invoke(this, $"Mock: Scanning for {durationSeconds} seconds...");

            // Simular descoberta de dispositivos
            await Task.Delay(1000);
            
            var device1 = new ScannerDeviceInfo 
            { 
                Name = "Inatek-BCST75S-1", 
                Mac = "AA:BB:CC:DD:EE:01",
                ConnectionState = "Available",
                Rssi = -65
            };
            
            _discoveredDevices.Add(device1);
            DeviceDiscovered?.Invoke(this, device1);

            await Task.Delay(1000);
            
            var device2 = new ScannerDeviceInfo 
            { 
                Name = "Inatek-BCST75S-2", 
                Mac = "AA:BB:CC:DD:EE:02",
                ConnectionState = "Available",
                Rssi = -72
            };
            
            _discoveredDevices.Add(device2);
            DeviceDiscovered?.Invoke(this, device2);

            await Task.Delay(durationSeconds * 1000 - 2000);
            StatusChanged?.Invoke(this, "Mock: Scan completed");

            return _discoveredDevices;
        }

        public void StopScan()
        {
            StatusChanged?.Invoke(this, "Mock: Scan stopped");
        }

        public async Task<bool> ConnectAsync(string deviceMac)
        {
            StatusChanged?.Invoke(this, $"Mock: Connecting to {deviceMac}...");
            await Task.Delay(1000);

            _currentDevice = _discoveredDevices.FirstOrDefault(d => d.Mac == deviceMac);
            if (_currentDevice != null)
            {
                _connectedDevices.Add(deviceMac);
                _currentDevice.ConnectionState = "Connected";
                StatusChanged?.Invoke(this, $"Mock: Connected to {_currentDevice.Name}");
                return true;
            }

            ErrorOccurred?.Invoke(this, "Device not found");
            return false;
        }

        public async Task<bool> DisconnectAsync()
        {
            if (_currentDevice != null)
            {
                _connectedDevices.Remove(_currentDevice.Mac);
                _currentDevice.ConnectionState = "Disconnected";
                StatusChanged?.Invoke(this, "Mock: Disconnected");
                _currentDevice = null;
                return true;
            }

            return false;
        }

        public async Task<string?> GetDeviceVersionAsync()
        {
            return "Mock v1.0.0";
        }

        public async Task<string?> GetBatteryInfoAsync()
        {
            return "85%";
        }

        public async Task<bool> SetVolumeAsync(int level)
        {
            return true;
        }

        public async Task<bool> ConfigureForDataMatrixOnlyAsync()
        {
            return true;
        }

        public async Task<string> GetDeviceInfo(ScannerDeviceInfo device)
        {
            return $"Mock device info for {device.Name}";
        }

        public async Task<string?> ScanBarcodeAsync()
        {
            StatusChanged?.Invoke(this, "Mock: Scanning barcode...");
            await Task.Delay(500);
            
            // Simular diferentes códigos DataMatrix
            string[] mockCodes = new[]
            {
                "DM-BCST75S-2025-001-ABC123",
                "DM-PROD-SKU-78945-XYZ789",
                "DM-SERIAL-4H8K2J9L-REV2",
                "DM-BATCH-20251217-PALLET5",
                "DM-INATEK-DEVICE-00156-QR"
            };
            
            var random = new Random();
            string scannedCode = mockCodes[random.Next(mockCodes.Length)];
            
            DataReceived?.Invoke(this, scannedCode);
            StatusChanged?.Invoke(this, "Mock: Barcode scanned");
            
            return scannedCode;
        }    }
}