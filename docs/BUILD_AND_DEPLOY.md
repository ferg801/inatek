# 📦 GUÍA DE COMPILACIÓN Y DEPLOY
## Inatek Scanner - Android Binding + .NET MAUI

---

## 🎯 REQUISITOS PREVIOS

### Software Necesario

1. **Visual Studio 2022 (17.8 o superior)**
   - Workload: "Desarrollo para dispositivos móviles con .NET"
   - Componentes opcionales:
     - Android SDK (API 21-34)
     - Android NDK
     - Java Development Kit (JDK 11 o superior)

2. **.NET 8 SDK**
   ```powershell
   dotnet --version
   # Debe mostrar: 8.0.x o superior
   ```

3. **Android SDK**
   - Instalado a través de Visual Studio
   - O standalone: https://developer.android.com/studio

4. **Dispositivo Android físico**
   - Android 5.0 (API 21) o superior
   - Bluetooth Low Energy (BLE) soportado
   - **IMPORTANTE**: No funciona en emuladores (requiere Bluetooth real)

---

## 📁 ESTRUCTURA DEL PROYECTO

```
Inatek/
├── InateckBinding/                   ← Android Binding Library
│   ├── Jars/
│   │   ├── inateck-scanner-ble-2-0-0.jar
│   │   ├── jna-min.jar
│   │   └── jna-platform.jar
│   ├── Transforms/
│   │   └── Metadata.xml
│   ├── Additions/
│   │   └── InateckScannerWrapper.cs
│   └── InateckBinding.csproj
│
├── InateckMauiApp/                   ← Aplicación MAUI
│   ├── Services/
│   │   └── IScannerService.cs
│   ├── Platforms/Android/
│   │   ├── AndroidScannerService.cs
│   │   ├── MainActivity.cs
│   │   └── AndroidManifest.xml
│   ├── ViewModels/
│   │   └── MainViewModel.cs
│   ├── Views/
│   │   ├── MainPage.xaml
│   │   └── MainPage.xaml.cs
│   └── InateckMauiApp.csproj
│
└── InateckScanner.sln                ← Solution principal
```

---

## 🔨 PASO 1: COMPILAR EL BINDING

### Opción A: Visual Studio

1. Abrir `InateckScanner.sln`
2. Click derecho en proyecto `InateckBinding`
3. Seleccionar **"Compilar"** (Build)
4. Esperar a que termine la compilación
5. Revisar la ventana de salida para errores

### Opción B: Línea de comandos

```powershell
# Navegar a la carpeta del binding
cd InateckBinding

# Restaurar dependencias
dotnet restore

# Compilar en Release
dotnet build -c Release

# Verificar DLL generada
ls bin\Release\net8.0-android\InateckBinding.dll
```

### Solución de Errores Comunes del Binding

#### Error: "jar2xml failed"
**Solución:**
```powershell
# Verificar que los JARs existen
ls Jars\

# Si faltan, copiarlos desde:
cp ..\InateckSDK\libs-for-binding\*.jar Jars\
```

#### Error: "Duplicate method/class"
**Solución:** Editar `Transforms\Metadata.xml` y agregar:
```xml
<remove-node path="/api/package[@name='ruta.conflictiva']/class[@name='NombreClase']" />
```

#### Error: "Kotlin types not found"
**Solución:** Verificar que `Xamarin.Kotlin.StdLib` está en el .csproj:
```xml
<PackageReference Include="Xamarin.Kotlin.StdLib" Version="1.9.10" />
```

---

## 🏗️ PASO 2: COMPILAR LA APP MAUI

### Opción A: Visual Studio

1. Click derecho en proyecto `InateckMauiApp`
2. Seleccionar **"Establecer como proyecto de inicio"**
3. En la barra de herramientas:
   - Configuración: **Debug**
   - Plataforma: **Android**
   - Dispositivo: Seleccionar tu dispositivo físico
4. Presionar **F5** o click en "Ejecutar"

### Opción B: Línea de comandos

```powershell
# Navegar a la carpeta del proyecto MAUI
cd InateckMauiApp

# Restaurar dependencias
dotnet restore

# Compilar para Android (Debug)
dotnet build -c Debug -f net8.0-android34.0

# Publicar APK instalable
dotnet publish -c Debug -f net8.0-android34.0 -o ..\output
```

---

## 📱 PASO 3: PREPARAR EL DISPOSITIVO ANDROID

### Habilitar Modo Desarrollador

1. Ir a **Configuración** > **Acerca del teléfono**
2. Tocar **Número de compilación** 7 veces
3. Aparecerá mensaje: "Ahora eres desarrollador"

### Habilitar Depuración USB

1. Ir a **Configuración** > **Opciones de desarrollador**
2. Activar **Depuración USB**
3. (Opcional) Activar **Instalación vía USB**

### Conectar el Dispositivo

```powershell
# Conectar el dispositivo vía USB

# Verificar conexión con ADB
adb devices

# Debe mostrar algo como:
# List of devices attached
# ABC123XYZ    device
```

Si no reconoce el dispositivo:
```powershell
# Instalar drivers USB del fabricante
# O usar drivers universales: https://adb.clockworkmod.com/
```

---

## 🚀 PASO 4: INSTALAR LA APP

### Método 1: Desde Visual Studio (Recomendado)

1. Con el dispositivo conectado
2. Presionar **F5** (Run with Debugging)
   - O **Ctrl+F5** (Run without Debugging)
3. Visual Studio instalará automáticamente el APK
4. La app se abrirá en el dispositivo

### Método 2: Instalación Manual con ADB

```powershell
# Navegar a la carpeta de salida
cd output

# Listar APKs generados
ls *.apk

# Instalar APK (reemplaza nombre si es diferente)
adb install -r com.tenaris.inateckscanner-Signed.apk

# Verificar instalación
adb shell pm list packages | grep tenaris
```

### Método 3: Copiar APK al dispositivo

1. Copiar el archivo APK a la carpeta `Download` del dispositivo
2. Abrir el explorador de archivos en el dispositivo
3. Tocar el APK
4. Permitir instalación desde fuentes desconocidas (si se solicita)
5. Tocar "Instalar"

---

## ⚙️ PASO 5: CONFIGURACIÓN INICIAL EN EL DISPOSITIVO

### Permisos Necesarios

Al abrir la app por primera vez, solicitará permisos:

1. **Bluetooth**
   - Requerido para escanear dispositivos
   - Android 12+: "BLUETOOTH_SCAN" y "BLUETOOTH_CONNECT"

2. **Ubicación**
   - Requerido por Android para escaneo BLE
   - Seleccionar: "Permitir solo mientras se usa la app"

3. **Bluetooth (Hardware)**
   - Asegurarse que el Bluetooth está activado en el dispositivo

### Solución si no aparecen los permisos:

```powershell
# Forzar apertura de configuración de permisos
adb shell am start -a android.settings.APPLICATION_DETAILS_SETTINGS -d package:com.tenaris.inateckscanner

# O desinstalar y reinstalar
adb uninstall com.tenaris.inateckscanner
adb install -r output\*.apk
```

---

## 🧪 PASO 6: PROBAR LA APLICACIÓN

### Flujo de Prueba Básico

1. **Inicializar el servicio**
   - Tocar botón "Inicializar Servicio"
   - Esperar mensaje: "Servicio inicializado correctamente"

2. **Escanear dispositivos**
   - Encender el escáner Inatek BCST-75S
   - Asegurarse que está en modo emparejamiento
   - Tocar "Escanear Dispositivos"
   - Esperar 10 segundos
   - Verificar que aparece en la lista

3. **Conectar al escáner**
   - Seleccionar el dispositivo en la lista
   - Tocar "Conectar"
   - Esperar mensaje: "Conectado a [nombre]"

4. **Obtener información**
   - Tocar "Actualizar Información"
   - Verificar que se muestran:
     - Versión del firmware
     - Nivel de batería

5. **Probar lectura de código**
   - Escanear un código de barras con el dispositivo
   - Verificar que aparece en "Último Código Leído"

6. **Configurar volumen**
   - Mover el slider de volumen (0-4)
   - Tocar "Aplicar Volumen"
   - Verificar confirmación

7. **Desconectar**
   - Tocar "Desconectar"
   - Verificar mensaje de desconexión

---

## 🔍 DEBUGGING Y LOGS

### Ver Logs en Tiempo Real

```powershell
# Ver todos los logs de la app
adb logcat | grep -i "tenaris\|inatek\|scanner"

# Filtrar solo errores
adb logcat *:E | grep -i "tenaris"

# Guardar logs a archivo
adb logcat -d > logs_inatek.txt
```

### Logs desde Visual Studio

1. Menú: **Ver** > **Salida**
2. Seleccionar: "Depuración" en el desplegable
3. Filtrar por "Inatek" o "Scanner"

### Breakpoints en C#

1. Colocar breakpoint en `AndroidScannerService.cs`
2. Ejecutar con F5 (modo debug)
3. Interactuar con la app
4. El código se detendrá en el breakpoint

---

## 🐛 SOLUCIÓN DE PROBLEMAS

### Problema: "No se encuentran dispositivos al escanear"

**Posibles causas:**
- Permisos no otorgados
- Bluetooth apagado
- Escáner en modo incorrecto
- Escáner ya emparejado con otro dispositivo

**Soluciones:**
```powershell
# Verificar permisos
adb shell dumpsys package com.tenaris.inateckscanner | grep permission

# Verificar estado de Bluetooth
adb shell settings get global bluetooth_on
# Debe retornar: 1

# Limpiar caché de Bluetooth (requiere root)
adb shell pm clear com.android.bluetooth
```

### Problema: "Error al conectar al dispositivo"

**Soluciones:**
1. Apagar y encender el escáner
2. Olvidar dispositivo en configuración de Bluetooth de Android
3. Reiniciar la app
4. Verificar que no está conectado a otro dispositivo

### Problema: "La app se cierra inesperadamente"

**Revisar logs:**
```powershell
adb logcat | grep -i "crash\|exception\|error"
```

**Verificar binding compilado correctamente:**
```powershell
cd InateckBinding
dotnet build -c Debug
# Revisar warnings y errors
```

### Problema: "No compila el Binding"

**Errores de Java/Kotlin:**
- Revisar `Transforms\Metadata.xml`
- Agregar transformaciones para clases conflictivas
- Actualizar `Xamarin.Kotlin.StdLib` a última versión

**Errores de JARs faltantes:**
```powershell
# Verificar JARs
ls InateckBinding\Jars\

# Si faltan, copiar desde SDK
cp InateckSDK\libs-for-binding\*.jar InateckBinding\Jars\
```

---

## 📊 CONFIGURACIONES AVANZADAS

### Cambiar API Level de Android

Editar `InateckMauiApp.csproj`:
```xml
<TargetFrameworks>net8.0-android33.0</TargetFrameworks>
<SupportedOSPlatformVersion>21</SupportedOSPlatformVersion>
```

### Habilitar Proguard (Ofuscación)

Editar `InateckMauiApp.csproj`:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <AndroidEnableProguard>true</AndroidEnableProguard>
  <AndroidLinkMode>Full</AndroidLinkMode>
</PropertyGroup>
```

### Firmar APK para Release

```powershell
# Generar keystore
keytool -genkey -v -keystore inatek.keystore -alias inatek_key -keyalg RSA -keysize 2048 -validity 10000

# Editar InateckMauiApp.csproj
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <AndroidKeyStore>true</AndroidKeyStore>
  <AndroidSigningKeyStore>inatek.keystore</AndroidSigningKeyStore>
  <AndroidSigningKeyAlias>inatek_key</AndroidSigningKeyAlias>
  <AndroidSigningKeyPass>tu_contraseña</AndroidSigningKeyPass>
  <AndroidSigningStorePass>tu_contraseña</AndroidSigningStorePass>
</PropertyGroup>

# Compilar Release
dotnet publish -c Release -f net8.0-android34.0
```

---

## 🎯 CHECKLIST DE DEPLOY

Antes de considerar la app lista para producción:

- [ ] Binding compila sin errores ni warnings
- [ ] App MAUI compila correctamente
- [ ] Todos los permisos se solicitan correctamente
- [ ] Escaneo de dispositivos funciona
- [ ] Conexión al escáner exitosa
- [ ] Lectura de códigos de barras funciona
- [ ] Información del dispositivo se obtiene correctamente
- [ ] Control de volumen funciona
- [ ] Desconexión limpia
- [ ] No hay crashes en uso normal
- [ ] Logs limpios (sin errores críticos)
- [ ] Probado en al menos 2 dispositivos diferentes
- [ ] Probado en Android 12+ y Android 10-
- [ ] APK firmado para Release

---

## 📞 SOPORTE

### Recursos Oficiales

- **SDK Inatek:** https://github.com/Inateck-Technology-Inc/android_sdk
- **Documentación:** https://docs.inateck.com/scanner-sdk-en/
- **Soporte Inatek:** support@inateck.com

### Documentación del Proyecto

- [API_MAPPING.md](API_MAPPING.md) - Mapeo de la API del SDK
- [FASE_1_RESUMEN.md](FASE_1_RESUMEN.md) - Resumen de la investigación inicial
- [BUILD_AND_DEPLOY.md](BUILD_AND_DEPLOY.md) - Esta guía

---

## 📝 NOTAS FINALES

### Limitaciones Conocidas

1. **Solo funciona en Android**
   - El binding es específico para Android
   - No hay versión para iOS/Windows/macOS

2. **Requiere dispositivo físico**
   - El emulador no soporta Bluetooth real
   - Pruebas solo posibles en hardware real

3. **Lectura de códigos por investigar**
   - El callback específico para datos del escáner requiere validación
   - Ver sección 1 del [API_MAPPING.md](API_MAPPING.md#limitaciones-identificadas)

### Próximos Pasos Recomendados

1. **Probar con dispositivo real Inatek BCST-75S**
2. **Validar protocolo de lectura de códigos**
3. **Implementar manejo robusto de reconexión**
4. **Agregar logging estructurado (ej: Serilog)**
5. **Implementar tests unitarios**
6. **Considerar UI/UX mejorada para producción**

---

**Última actualización:** 2025-12-15
**Versión del documento:** 1.0
