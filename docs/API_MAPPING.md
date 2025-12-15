# API MAPPING - Inatek Scanner SDK

## Resumen Ejecutivo

**Archivo SDK:** `inateck-scanner-ble-2-0-0.jar`
**Package principal:** `com.inateck.scanner.ble`
**Versión analizada:** 2.0.0
**Fecha de análisis:** 2025-12-15

---

## 1. CLASES PRINCIPALES

### 1.1 BleListManager
**Ubicación:** `com.inateck.scanner.ble.BleListManager`

**Responsabilidad:** Gestión centralizada de dispositivos BLE y escaneo

**Métodos identificados (del código demo):**
```kotlin
// Inicialización
BleListManager.init(application: Application)

// Escaneo
BleListManager.scan(callback: BleScanResultCallBack)
BleListManager.stopScan()

// Propiedades
BleListManager.scannerDevices: List<BleScannerDevice>
BleListManager.disconnectHandler: (BleScannerDevice, Boolean) -> Unit
```

**Mapping a C#:**
```csharp
public static class BleListManager
{
    public static void Init(Android.App.Application application);
    public static void Scan(IBleScanResultCallBack callback);
    public static void StopScan();

    public static IList<BleScannerDevice> ScannerDevices { get; }
    public static Action<BleScannerDevice, bool> DisconnectHandler { get; set; }
}
```

---

### 1.2 BleScannerDevice
**Ubicación:** `com.inateck.scanner.ble.BleScannerDevice`

**Responsabilidad:** Representación de un dispositivo escáner individual

**Propiedades identificadas:**
```kotlin
val name: String
val mac: String
val connectState: BleScannerConnectState
val messager: BleMessager
```

**Métodos identificados:**
```kotlin
fun connect(callback: (Result<Unit>) -> Unit)
fun disconnect(callback: (Result<Unit>) -> Unit)
fun setMTU(mtu: Int, callback: (Result<Unit>) -> Unit)
```

**Mapping a C#:**
```csharp
public class BleScannerDevice
{
    public string Name { get; }
    public string Mac { get; }
    public BleScannerConnectState ConnectState { get; }
    public BleMessager Messager { get; }

    public void Connect(Action<Result<object>> callback);
    public void Disconnect(Action<Result<object>> callback);
    public void SetMTU(int mtu, Action<Result<object>> callback);
}
```

---

### 1.3 BleMessager
**Ubicación:** `com.inateck.scanner.ble.BleMessager`

**Responsabilidad:** Comunicación y comandos con el dispositivo

**Métodos identificados (del código demo):**
```kotlin
fun getHardwareInfo(callback: (Result<String>) -> Unit)
fun getBatteryInfo(callback: (Result<String>) -> Unit)
fun getVersion(callback: (Result<String>) -> Unit)
fun getSettingInfo(callback: (Result<String>) -> Unit)
fun setSettingInfo(settings: String, callback: (Result<Unit>) -> Unit)
fun sendData(data: ByteArray, callback: (Result<ByteArray>) -> Unit)
```

**Mapping a C#:**
```csharp
public class BleMessager
{
    public void GetHardwareInfo(Action<Result<string>> callback);
    public void GetBatteryInfo(Action<Result<string>> callback);
    public void GetVersion(Action<Result<string>> callback);
    public void GetSettingInfo(Action<Result<string>> callback);
    public void SetSettingInfo(string settings, Action<Result<object>> callback);
    public void SendData(byte[] data, Action<Result<byte[]>> callback);
}
```

---

### 1.4 BleScannerConnectState (Enum)
**Ubicación:** `com.inateck.scanner.ble.BleScannerConnectState`

**Valores identificados (del código demo):**
```kotlin
enum class BleScannerConnectState {
    CONNECTING,
    CONNECTED,
    DISCONNECTING,
    DISCONNECTED,
    UNKNOWN
}
```

**Mapping a C#:**
```csharp
public enum BleScannerConnectState
{
    Connecting,
    Connected,
    Disconnecting,
    Disconnected,
    Unknown
}
```

---

## 2. CALLBACKS / INTERFACES

### 2.1 BleScanResultCallBack
**Ubicación:** `com.inateck.scanner.ble.callback.BleScanResultCallBack`

**Métodos:**
```kotlin
interface BleScanResultCallBack {
    fun onScanStarted(scanResultList: List<BleScannerDevice>)
    fun onScanning(device: BleScannerDevice)
    fun onScanFinished(scanResultList: List<BleScannerDevice>)
}
```

**Mapping a C#:**
```csharp
public interface IBleScanResultCallBack
{
    void OnScanStarted(IList<BleScannerDevice> scanResultList);
    void OnScanning(BleScannerDevice device);
    void OnScanFinished(IList<BleScannerDevice> scanResultList);
}
```

---

### 2.2 BleConnectResultCallBack
**Ubicación:** `com.inateck.scanner.ble.callback.BleConnectResultCallBack`

**Nota:** No se observa uso directo en el código demo, pero está disponible en el JAR.

---

## 3. CLASES AUXILIARES

### 3.1 BleParseEvent
**Ubicación:** `com.inateck.scanner.ble.BleParseEvent`

**Subclases identificadas:**
- `BleParseEvent.Loading` - Evento de carga

### 3.2 BleTaskManager
**Ubicación:** `com.inateck.scanner.ble.BleTaskManager`

**Responsabilidad:** Gestión interna de tareas BLE (probablemente uso interno del SDK)

### 3.3 ScanRecordUtil
**Ubicación:** `com.inateck.scanner.ble.ScanRecordUtil`

**Responsabilidad:** Utilidades para parsing de registros de escaneo

### 3.4 BleScannerException
**Ubicación:** `com.inateck.scanner.ble.BleScannerException`

**Tipo:** Excepción personalizada del SDK

### 3.5 CmdJNA
**Ubicación:** `com.inateck.scanner.ble.CmdJNA`

**Responsabilidad:** Interfaz JNA para procesamiento de comandos (parsing library)

---

## 4. DEPENDENCIAS EXTERNAS

**Identificadas en build.gradle.kts:**

```kotlin
// Biblioteca BLE de terceros
implementation("com.github.Jasonchenlijian:FastBle:2.4.0")

// JSON parsing
implementation("com.google.code.gson:gson:2.8.9")

// JNA (Java Native Access) - Incluidas en libs/
- jna-min.jar
- jna-platform.jar
```

---

## 5. FLUJO DE USO TÍPICO

### 5.1 Inicialización
```kotlin
// 1. Inicializar el manager
BleListManager.init(application)
```

### 5.2 Escaneo de dispositivos
```kotlin
// 2. Iniciar escaneo
BleListManager.scan(object : BleScanResultCallBack {
    override fun onScanStarted(scanResultList: List<BleScannerDevice>) {
        // Escaneo iniciado
    }

    override fun onScanning(device: BleScannerDevice) {
        // Dispositivo encontrado
    }

    override fun onScanFinished(scanResultList: List<BleScannerDevice>) {
        // Escaneo completado
    }
})

// 3. Detener escaneo
BleListManager.stopScan()
```

### 5.3 Conexión a dispositivo
```kotlin
// 4. Conectar a un dispositivo
val device = BleListManager.scannerDevices[0]
device.connect { result ->
    if (result.isSuccess) {
        // Conectado exitosamente
    } else {
        // Error de conexión
    }
}
```

### 5.4 Obtener información del dispositivo
```kotlin
// 5. Leer información
device.messager.getVersion { result ->
    if (result.isSuccess) {
        val version = result.getOrNull()
        // Procesar versión
    }
}

device.messager.getBatteryInfo { result ->
    if (result.isSuccess) {
        val battery = result.getOrNull()
        // Procesar batería
    }
}
```

### 5.5 Configurar dispositivo
```kotlin
// 6. Cambiar configuración (ejemplo: volumen)
val volumeSettings = "[{\"area\":\"3\",\"value\":\"4\",\"name\":\"volume\"}]"
device.messager.setSettingInfo(volumeSettings) { result ->
    if (result.isSuccess) {
        // Configuración aplicada
    }
}
```

### 5.6 Desconexión
```kotlin
// 7. Desconectar
device.disconnect { result ->
    if (result.isSuccess) {
        // Desconectado exitosamente
    }
}

// 8. Manejar desconexiones no solicitadas
BleListManager.disconnectHandler = { device, isUser ->
    // device: dispositivo desconectado
    // isUser: true si fue iniciado por el usuario
}
```

---

## 6. FORMATO DE DATOS OBSERVADO

### 6.1 Configuraciones (JSON)
Las configuraciones se pasan como strings JSON:

```json
[
  {
    "area": "3",
    "value": "4",
    "name": "volume"
  }
]
```

**Áreas identificadas:**
- Area "3" = Volumen (valores: 0-4)

---

## 7. PERMISOS ANDROID REQUERIDOS

**Del AndroidManifest.xml:**

```xml
<!-- Localización (requerido para BLE en Android) -->
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />

<!-- Bluetooth -->
<uses-permission android:name="android.permission.BLUETOOTH" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />

<!-- Bluetooth (Android 12+) -->
<uses-permission
    android:name="android.permission.BLUETOOTH_SCAN"
    android:usesPermissionFlags="neverForLocation" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<uses-permission android:name="android.permission.BLUETOOTH_ADVERTISE" />
```

---

## 8. CONSIDERACIONES PARA EL BINDING

### 8.1 Transformaciones necesarias en Metadata.xml

```xml
<metadata>
  <!-- Renombrar para evitar conflictos -->
  <attr path="/api/package[@name='com.inateck.scanner.ble']/class[@name='BleListManager']"
        name="managedName">InateckBleListManager</attr>

  <!-- Convertir callbacks a interfaces -->
  <attr path="/api/package[@name='com.inateck.scanner.ble.callback']/interface[@name='BleScanResultCallBack']"
        name="managedName">IBleScanResultCallBack</attr>

  <attr path="/api/package[@name='com.inateck.scanner.ble.callback']/interface[@name='BleConnectResultCallBack']"
        name="managedName">IBleConnectResultCallBack</attr>

  <!-- Ocultar clases internas si es necesario -->
  <!-- <remove-node path="/api/package[@name='com.inateck.scanner.ble']/class[@name='BleTaskManager']" /> -->
</metadata>
```

### 8.2 Wrapper C# recomendado

**Convertir callbacks de Kotlin a eventos C#:**

```csharp
public class InateckScannerWrapper
{
    // Eventos C# en lugar de callbacks
    public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;
    public event EventHandler<ScanCompletedEventArgs> ScanCompleted;
    public event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
    public event EventHandler<DataReceivedEventArgs> DataReceived;
    public event EventHandler<ErrorEventArgs> ErrorOccurred;

    // Métodos async en lugar de callbacks
    public Task<bool> InitializeAsync(Application app);
    public Task<List<BleScannerDevice>> ScanAsync();
    public Task<bool> ConnectAsync(BleScannerDevice device);
    public Task<string> GetVersionAsync(BleScannerDevice device);
    public Task DisconnectAsync(BleScannerDevice device);
}
```

---

## 9. LIMITACIONES IDENTIFICADAS

1. **No hay evento directo de lectura de códigos de barras**:
   - No se observa un callback específico para datos escaneados en el código demo
   - Posiblemente se usa `BleMessager.sendData()` o hay características BLE específicas
   - Requiere investigación adicional del protocolo BLE del dispositivo

2. **Dependencia de FastBle**:
   - El SDK depende de una biblioteca externa de BLE
   - Esta dependencia también debe incluirse en el binding

3. **JNA requerido**:
   - Usa JNA para procesamiento nativo
   - Los JARs de JNA deben incluirse en el binding

---

## 10. ARCHIVOS A INCLUIR EN EL BINDING

```
InateckBinding/
├── Jars/
│   ├── inateck-scanner-ble-2-0-0.jar  ← SDK principal
│   ├── jna-min.jar                     ← Dependencia JNA
│   └── jna-platform.jar                ← Dependencia JNA
└── packages.config / .csproj
    └── FastBle (via NuGet o JAR binding)
```

---

## 11. PRÓXIMOS PASOS RECOMENDADOS

1. ✅ **Completado**: Análisis de estructura del SDK
2. ⏳ **Siguiente**: Crear proyecto Android Binding Library
3. ⏳ Probar binding básico (compilación)
4. ⏳ Implementar wrapper C# con eventos
5. ⏳ Investigar protocolo de lectura de códigos de barras
6. ⏳ Integrar en MAUI

---

## 12. REFERENCIAS

- **Repositorio SDK**: https://github.com/Inateck-Technology-Inc/android_sdk
- **Documentación oficial**: https://docs.inateck.com/scanner-sdk-en/
- **FastBle**: https://github.com/Jasonchenlijian/FastBle
- **Modelo escáner**: Inatek BCST-75S

---

**Documento generado automáticamente por el análisis del SDK**
**Última actualización:** 2025-12-15
