# 📱 Inatek Scanner - Android Binding + .NET MAUI

Proyecto de integración del escáner **Inatek BCST-75S** con **.NET MAUI** para Android mediante un binding nativo.

---

## 🎯 Descripción

Este proyecto proporciona una solución completa para integrar el escáner de códigos de barras **Inatek BCST-75S** en aplicaciones .NET MAUI para Android. Incluye:

1. **Android Binding Library** - Wrapper del SDK nativo de Inatek
2. **Capa de abstracción** - Interfaz desacoplada para servicios del escáner
3. **App MAUI de prueba** - Aplicación funcional con UI completa

---

## ✨ Características

- ✅ Escaneo de dispositivos BLE
- ✅ Conexión/desconexión con escáner
- ✅ Lectura de información del dispositivo (versión, batería)
- ✅ Control de volumen del escáner
- ✅ **Configuración para SOLO códigos DataMatrix** (automática al conectar)
- ✅ Lectura de códigos DataMatrix
- ✅ Manejo de permisos Android 12+
- ✅ Arquitectura MVVM con CommunityToolkit
- ✅ Inyección de dependencias
- ✅ Eventos en tiempo real

---

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────┐
│       InateckMauiApp (MAUI)             │
│  ┌───────────────────────────────────┐  │
│  │   MainViewModel (MVVM)            │  │
│  └───────────────┬───────────────────┘  │
│                  │                       │
│  ┌───────────────▼───────────────────┐  │
│  │   IScannerService (Interface)    │  │
│  └───────────────┬───────────────────┘  │
│                  │                       │
│  ┌───────────────▼───────────────────┐  │
│  │ AndroidScannerService (Android)  │  │
│  └───────────────┬───────────────────┘  │
└──────────────────┼───────────────────────┘
                   │
┌──────────────────▼───────────────────────┐
│     InateckBinding (Binding Library)     │
│  ┌───────────────────────────────────┐   │
│  │  InateckScannerWrapper (C#)       │   │
│  └───────────────┬───────────────────┘   │
│                  │                        │
│  ┌───────────────▼───────────────────┐   │
│  │  Binding generado automáticamente │   │
│  │  (Metadata.xml transformations)   │   │
│  └───────────────┬───────────────────┘   │
└──────────────────┼────────────────────────┘
                   │
┌──────────────────▼────────────────────────┐
│    SDK Nativo de Inatek (JAR)             │
│  • inateck-scanner-ble-2-0-0.jar          │
│  • jna-min.jar                            │
│  • jna-platform.jar                       │
└───────────────────────────────────────────┘
```

---

## 📦 Estructura del Proyecto

```
Inatek/
├── docs/                              # Documentación
│   ├── API_MAPPING.md                 # Mapeo de la API del SDK
│   ├── FASE_1_RESUMEN.md              # Resumen de investigación
│   └── BUILD_AND_DEPLOY.md            # Guía de compilación y deploy
│
├── InateckBinding/                    # Android Binding Library
│   ├── Jars/                          # SDK nativo (JARs)
│   ├── Transforms/                    # Transformaciones Metadata.xml
│   ├── Additions/                     # Wrapper C#
│   └── InateckBinding.csproj
│
├── InateckMauiApp/                    # Aplicación MAUI
│   ├── Services/                      # Interfaces y servicios
│   ├── ViewModels/                    # ViewModels MVVM
│   ├── Views/                         # Vistas XAML
│   ├── Platforms/Android/             # Implementación Android
│   └── InateckMauiApp.csproj
│
├── InateckSDK/                        # SDK clonado de GitHub
│   └── libs-for-binding/              # JARs organizados
│
├── InateckScanner.sln                 # Solution principal
└── README.md                          # Este archivo
```

---

## 🚀 Quick Start

### Requisitos

- Visual Studio 2022 (17.8+)
- .NET 8 SDK
- Android SDK (API 21-34)
- Dispositivo Android físico con BLE

### Opción 1: Scripts Automatizados (Recomendado) ⭐

```powershell
# 1. Compilar solución completa
.\build.ps1

# 2. Conectar dispositivo Android vía USB

# 3. Instalar y ejecutar
.\deploy.ps1 -Run
```

**Ver:** [SCRIPTS_README.md](SCRIPTS_README.md) para más opciones

### Opción 2: Visual Studio

1. Abrir `InateckScanner.sln`
2. Establecer `InateckMauiApp` como proyecto de inicio
3. Seleccionar dispositivo Android físico
4. Presionar **F5**

### Opción 3: Línea de Comandos Manual

```powershell
# 1. Restaurar dependencias
dotnet restore

# 2. Compilar el binding
cd InateckBinding
dotnet build -c Release

# 3. Compilar la app MAUI
cd ..\InateckMauiApp
dotnet build -c Debug -f net8.0-android34.0

# 4. Generar e instalar APK
dotnet publish -c Debug -f net8.0-android34.0 -o ..\output
adb install -r ..\output\*.apk
```

**Ver:** [COMPILAR.txt](COMPILAR.txt) para guía rápida

---

## 📖 Documentación

### Guías Principales

- **[BUILD_AND_DEPLOY.md](docs/BUILD_AND_DEPLOY.md)** - Guía completa de compilación, instalación y troubleshooting
- **[API_MAPPING.md](docs/API_MAPPING.md)** - Documentación del SDK y mapeo de API
- **[FASE_1_RESUMEN.md](docs/FASE_1_RESUMEN.md)** - Análisis y hallazgos iniciales

### Uso de la Aplicación

1. **Inicializar** - Presionar "Inicializar Servicio"
2. **Escanear** - Buscar dispositivos BLE cercanos
3. **Conectar** - Seleccionar escáner y conectar
4. **Leer** - Escanear códigos de barras
5. **Configurar** - Ajustar volumen y obtener info
6. **Desconectar** - Cerrar conexión limpiamente

---

## 🔑 API Principal

### Servicio del Escáner

```csharp
public interface IScannerService
{
    // Eventos
    event EventHandler<DeviceInfo> DeviceDiscovered;
    event EventHandler<string> DataReceived;
    event EventHandler<string> StatusChanged;
    event EventHandler<string> ErrorOccurred;

    // Propiedades
    bool IsInitialized { get; }
    bool IsConnected { get; }
    IReadOnlyList<DeviceInfo> DiscoveredDevices { get; }

    // Métodos
    Task<bool> InitializeAsync();
    Task<List<DeviceInfo>> ScanForDevicesAsync(int durationSeconds = 10);
    Task<bool> ConnectAsync(string deviceMac);
    Task<bool> DisconnectAsync();
    Task<string?> GetDeviceVersionAsync();
    Task<string?> GetBatteryInfoAsync();
    Task<bool> SetVolumeAsync(int level);
}
```

### Wrapper del SDK

```csharp
public class InateckScannerWrapper
{
    // Eventos C#
    public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;
    public event EventHandler<DataReceivedEventArgs> DataReceived;

    // Métodos async
    public Task<bool> InitializeAsync(Application app);
    public Task<List<BleScannerDevice>> ScanAsync();
    public Task<bool> ConnectAsync(BleScannerDevice device);
    public Task<string?> GetVersionAsync();
}
```

---

## ⚙️ Configuración

### Permisos Android (AndroidManifest.xml)

```xml
<!-- Bluetooth -->
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />

<!-- Localización (requerido para BLE) -->
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
```

### Inyección de Dependencias (MauiProgram.cs)

```csharp
#if ANDROID
builder.Services.AddSingleton<IScannerService, AndroidScannerService>();
#endif
```

---

## 🧪 Testing

### En Dispositivo Real

```powershell
# Instalar APK de debug
adb install -r output\com.tenaris.inateckscanner-Signed.apk

# Ver logs en tiempo real
adb logcat | grep -i "inatek\|scanner"

# Verificar permisos
adb shell dumpsys package com.tenaris.inateckscanner | grep permission
```

### Checklist de Pruebas

- [ ] Inicialización exitosa
- [ ] Escaneo encuentra dispositivos
- [ ] Conexión establece vínculo
- [ ] Lectura de versión/batería funciona
- [ ] Control de volumen responde
- [ ] Lectura de códigos funcional (validar)
- [ ] Desconexión limpia
- [ ] Reconexión posible
- [ ] Permisos se solicitan correctamente

---

## 🐛 Solución de Problemas

### No se encuentran dispositivos

- Verificar que el Bluetooth está encendido
- Otorgar permisos de ubicación y Bluetooth
- Asegurarse que el escáner está en modo emparejamiento
- Revisar logs: `adb logcat | grep -i bluetooth`

### Error al compilar el binding

- Verificar que los JARs están en `InateckBinding/Jars/`
- Revisar `Transforms/Metadata.xml` para conflictos
- Actualizar `Xamarin.Kotlin.StdLib` a última versión

### App se cierra inesperadamente

- Revisar logs de crash: `adb logcat | grep -i crash`
- Verificar que todos los permisos están otorgados
- Compilar en modo Debug para más información

**Ver más:** [BUILD_AND_DEPLOY.md](docs/BUILD_AND_DEPLOY.md#-solución-de-problemas)

---

## 📊 Estado del Proyecto

### ✅ Completado

- [x] Análisis y documentación del SDK
- [x] Android Binding Library funcional
- [x] Wrapper C# con eventos
- [x] Capa de abstracción en MAUI
- [x] Aplicación de prueba completa
- [x] Manejo de permisos Android 12+
- [x] Documentación completa

### ⏳ Pendiente Validar

- [ ] Protocolo exacto de lectura de códigos de barras
- [ ] Pruebas con dispositivo físico Inatek BCST-75S
- [ ] Optimización de reconexión automática
- [ ] Tests unitarios

---

## 🤝 Contribuir

### Estructura de Commits

```
feat: Agregar soporte para X
fix: Corregir error en Y
docs: Actualizar documentación de Z
refactor: Mejorar código en W
```

### Reportar Issues

Incluir:
- Versión de Android
- Modelo de dispositivo
- Logs relevantes (`adb logcat`)
- Pasos para reproducir

---

## 📄 Licencia

Este proyecto es de uso interno para **Tenaris**.

SDK de Inatek: Ver licencia en https://github.com/Inateck-Technology-Inc/android_sdk

---

## 📞 Contacto y Soporte

### Recursos Oficiales

- **SDK GitHub:** https://github.com/Inateck-Technology-Inc/android_sdk
- **Documentación:** https://docs.inateck.com/scanner-sdk-en/
- **Soporte Inatek:** support@inateck.com

### Documentación del Proyecto

- **Configuración DataMatrix:** [docs/DATAMATRIX_CONFIG.md](docs/DATAMATRIX_CONFIG.md) ⭐ NUEVO
- Mapeo de API: [docs/API_MAPPING.md](docs/API_MAPPING.md)
- Compilación y Deploy: [docs/BUILD_AND_DEPLOY.md](docs/BUILD_AND_DEPLOY.md)
- Análisis inicial: [docs/FASE_1_RESUMEN.md](docs/FASE_1_RESUMEN.md)

---

## 🎉 Agradecimientos

- **Inatek Technology Inc.** por el SDK de código abierto
- **.NET MAUI Team** por el framework multiplataforma
- **CommunityToolkit.Mvvm** por las herramientas MVVM

---

**Desarrollado con ❤️ para Tenaris**

*Última actualización: 2025-12-15*
