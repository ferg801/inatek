# 📱 Inatek Scanner - .NET MAUI para Android

Aplicación .NET MAUI para integrar el escáner de códigos de barras **Inatek BCST-75S** en Android.

---

## 🎯 Descripción

Esta aplicación permite conectar y usar el escáner Inatek BCST-75S en **dos modos**:

### 📟 Modo HID (Recomendado)
El escáner actúa como un teclado Bluetooth y envía los códigos escaneados como texto.
- ✅ Fácil de usar - solo emparejar y escanear
- ✅ No requiere configuración especial
- ✅ Funciona con cualquier app que acepte texto

### 📡 Modo SDK (Avanzado)
Conexión directa BLE/GATT con el escáner usando la librería nativa CMD.
- ✅ Comunicación directa sin emparejamiento Bluetooth del sistema
- ✅ Menor latencia
- ⚠️ Requiere configurar el escáner en modo GATT (vía app Inatek Office)

---

## ✨ Características

- ✅ Dos modos de conexión: HID y SDK
- ✅ Escaneo de dispositivos BLE
- ✅ Conexión/desconexión con escáner
- ✅ Lectura de batería y versión del dispositivo (modo HID)
- ✅ Captura de códigos escaneados
- ✅ Auto-reconexión al volver a la app
- ✅ UI nativa Android optimizada
- ✅ Historial de escaneos con timestamps

---

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────┐
│                   MainActivity.cs                    │
│              (Selector de Modo)                      │
│                                                      │
│    ┌─────────────────┐    ┌─────────────────┐       │
│    │  HID MODE       │    │   SDK MODE      │       │
│    │                 │    │                 │       │
│    │  HidScanner     │    │  SdkScanner     │       │
│    │  Activity.cs    │    │  Activity.cs    │       │
│    │                 │    │                 │       │
│    │  • BLE Info     │    │  • BLE/GATT     │       │
│    │  • HID Capture  │    │  • CMD Library  │       │
│    └─────────────────┘    └─────────────────┘       │
└─────────────────────────────────────────────────────┘
```

---

## 📦 Estructura del Proyecto

```
inatek/
├── InateckMauiApp/              # Aplicación MAUI
│   ├── Platforms/Android/       # Código Android nativo
│   │   ├── MainActivity.cs      # Selector de modo
│   │   ├── HidScannerActivity.cs # Modo HID
│   │   ├── SdkScannerActivity.cs # Modo SDK (GATT)
│   │   ├── AndroidScannerService.cs # Servicio BLE
│   │   ├── InateckScannerCmd.cs # Wrapper librería nativa
│   │   ├── lib/arm64-v8a/       # libinateck_scanner_cmd.so
│   │   └── AndroidManifest.xml  # Permisos
│   ├── Services/IScannerService.cs
│   └── InateckMauiApp.csproj
│
├── publish/android/             # APK compilado
├── InateckScanner.sln
└── README.md
```

---

## 🚀 Inicio Rápido

### Requisitos

- .NET 9 SDK
- Android SDK (API 35)
- Dispositivo Android físico con Bluetooth
- Escáner Inatek BCST-75S

### Compilar e Instalar

```bash
# 1. Navegar al directorio del proyecto
cd /Users/tenaris/Documents/Tenaris/Inatek/inatek

# 2. Compilar APK
dotnet publish InateckMauiApp -f net9.0-android35.0 -c Release -p:AndroidPackageFormat=apk -o ./publish/android

# 3. Instalar en dispositivo conectado
~/Android/platform-tools/adb install -r ./publish/android/com.tenaris.inateckscanner-Signed.apk
```

---

## 📖 Uso de la Aplicación

### Modo HID
1. **Emparejar** el escáner vía Bluetooth del sistema (como teclado)
2. Abrir la app → **HID Mode**
3. Conectar al dispositivo
4. Escanear códigos - aparecen automáticamente

### Modo SDK
1. **Configurar escáner en modo GATT** usando app "Inatek Office" (iOS)
2. Abrir la app → **SDK Mode**
3. Escanear dispositivos BLE
4. Conectar a "HPRT-000E" (nombre en modo GATT)
5. Escanear códigos - datos llegan vía BLE

---

## 🔧 Permisos Android

| Permiso | Uso |
|---------|-----|
| `BLUETOOTH_SCAN` | Buscar dispositivos BLE |
| `BLUETOOTH_CONNECT` | Conectar al escáner |
| `ACCESS_FINE_LOCATION` | Requerido para BLE scan (SDK mode) |

---

## 🐛 Depuración

```bash
# Ver logs en tiempo real
~/Android/platform-tools/adb logcat -d | grep "InateckScanner"

# Limpiar logs y monitorear
~/Android/platform-tools/adb logcat -c && ~/Android/platform-tools/adb logcat | grep "InateckScanner"
```

---

## 📋 Servicios BLE (Modo SDK)

| Servicio | UUID | Uso |
|----------|------|-----|
| AE00 | `0000ae00-...` | Comunicación SDK (Write: AE01, Notify: AE02) |
| FF00 | `0000ff00-...` | Datos de escaneo (Notify: FF01) |

---

## 📝 Historial de Cambios

### v1.1.0 (Diciembre 2025)
- ✅ Agregado Modo SDK con conexión GATT directa
- ✅ Integración librería nativa CMD
- ✅ Parsing de datos de escaneo del protocolo BLE
- ✅ UI simplificada para ambos modos

### v1.0.0 (Diciembre 2025)
- ✅ Modo HID funcional
- ✅ Conexión BLE para info del dispositivo
- ✅ Captura de códigos vía teclado HID

---

## 📄 Licencia

Proyecto interno Tenaris - Uso exclusivo.
