# 🚀 Scripts de Compilación y Deploy

Este proyecto incluye scripts automatizados para facilitar la compilación y deploy de la aplicación Inatek Scanner.

---

## 📋 Scripts Disponibles

### 1. `build.ps1` - Script de Compilación

**Función:** Compila toda la solución (Binding + App MAUI)

**Uso:**
```powershell
.\build.ps1
```

**Qué hace:**
1. ✅ Verifica que .NET SDK esté instalado
2. ✅ Restaura dependencias NuGet
3. ✅ Compila el Android Binding Library
4. ✅ Compila la aplicación .NET MAUI
5. ✅ Compila la solución completa
6. ✅ Verifica dispositivos Android conectados

**Salida esperada:**
```
=====================================
 COMPILACIÓN COMPLETADA CON ÉXITO
=====================================

Archivos generados:
  • InateckBinding.dll
  • InateckMauiApp.dll

Próximos pasos:
  1. Conectar dispositivo Android físico vía USB
  2. Ejecutar: .\deploy.ps1
```

---

### 2. `deploy.ps1` - Script de Deploy

**Función:** Genera APK e instala en dispositivo Android

**Uso Básico:**
```powershell
# Solo generar APK
.\deploy.ps1

# Generar APK e instalar
.\deploy.ps1 -Install

# Generar, instalar y ejecutar
.\deploy.ps1 -Run

# Compilar en Release y ejecutar
.\deploy.ps1 -Release -Run
```

**Parámetros:**

| Parámetro | Descripción |
|-----------|-------------|
| `-Release` | Compila en modo Release (optimizado) |
| `-Install` | Instala el APK en el dispositivo |
| `-Run` | Instala y ejecuta la aplicación |

**Ejemplos:**

```powershell
# Desarrollo normal
.\deploy.ps1 -Run

# Para producción
.\deploy.ps1 -Release -Install

# Solo generar APK (sin instalar)
.\deploy.ps1
```

**Qué hace:**
1. ✅ Verifica dispositivo Android conectado
2. ✅ Muestra información del dispositivo
3. ✅ Compila el proyecto
4. ✅ Genera el APK
5. ✅ (Opcional) Instala en el dispositivo
6. ✅ (Opcional) Ejecuta la aplicación

**Salida esperada:**
```
=====================================
 DEPLOY COMPLETADO
=====================================

✓ Dispositivo Android conectado
  Modelo: Samsung Galaxy S21
  Android: 13

✓ Compilación exitosa
✓ APK generado correctamente
  output\com.tenaris.inateckscanner-Signed.apk
  Tamaño: 45.2 MB

✓ Aplicación instalada correctamente
✓ Aplicación iniciada
```

---

## 🔧 Requisitos Previos

### 1. .NET SDK
```powershell
# Verificar instalación
dotnet --version

# Debe mostrar: 8.0.x o superior
```

**Si no está instalado:**
- Descargar de: https://dotnet.microsoft.com/download

### 2. Android SDK / ADB
```powershell
# Verificar instalación
adb version

# Debe mostrar: Android Debug Bridge version 1.x.x
```

**Si no está instalado:**
- Instalar Android SDK Platform Tools
- O instalar Visual Studio con workload "Desarrollo para dispositivos móviles con .NET"

### 3. Dispositivo Android
- Conectado vía USB
- Modo Desarrollador habilitado
- Depuración USB activada
- Permisos de depuración aceptados

**Verificar conexión:**
```powershell
adb devices

# Debe mostrar:
# List of devices attached
# ABC123XYZ    device
```

---

## 📝 Flujo de Trabajo Completo

### Primera vez:

```powershell
# 1. Compilar solución
.\build.ps1

# 2. Conectar dispositivo Android

# 3. Instalar y ejecutar
.\deploy.ps1 -Run
```

### Desarrollo iterativo:

```powershell
# Hacer cambios en el código...

# Compilar y ejecutar rápidamente
.\deploy.ps1 -Run
```

### Preparar para producción:

```powershell
# Compilar en Release
.\build.ps1

# Generar APK firmado
.\deploy.ps1 -Release -Install
```

---

## 🐛 Solución de Problemas

### Error: ".NET SDK no encontrado"

**Solución:**
```powershell
# Instalar .NET SDK 8.0
# https://dotnet.microsoft.com/download
```

### Error: "ADB no encontrado"

**Solución:**
```powershell
# Opción 1: Instalar Android SDK Platform Tools
# https://developer.android.com/studio/releases/platform-tools

# Opción 2: Agregar ADB al PATH
$env:Path += ";C:\Users\TU_USUARIO\AppData\Local\Android\Sdk\platform-tools"
```

### Error: "No hay dispositivos Android conectados"

**Solución:**
1. Conectar dispositivo vía USB
2. En el dispositivo:
   - Ir a Configuración → Acerca del teléfono
   - Tocar "Número de compilación" 7 veces
   - Ir a Configuración → Opciones de desarrollador
   - Activar "Depuración USB"
3. Aceptar permiso de depuración en el dispositivo
4. Verificar: `adb devices`

### Error: "Falló la compilación del binding"

**Solución:**
1. Revisar errores en la salida
2. Verificar que los JARs existen en `InateckBinding/Jars/`
3. Revisar `InateckBinding/Transforms/Metadata.xml`
4. Compilar solo el binding para ver errores:
   ```powershell
   dotnet build InateckBinding\InateckBinding.csproj -v detailed
   ```

### Error: "Falló la instalación del APK"

**Solución:**
```powershell
# Desinstalar versión anterior
adb uninstall com.tenaris.inateckscanner

# Reinstalar
.\deploy.ps1 -Install
```

---

## 📊 Comandos Útiles Adicionales

### Ver logs en tiempo real:
```powershell
# Logs de la aplicación
adb logcat | Select-String "inatek|scanner|tenaris"

# Solo errores
adb logcat *:E | Select-String "tenaris"

# Guardar logs a archivo
adb logcat -d > logs.txt
```

### Gestión de la app:
```powershell
# Desinstalar app
adb uninstall com.tenaris.inateckscanner

# Ver info de la app instalada
adb shell dumpsys package com.tenaris.inateckscanner

# Limpiar datos de la app
adb shell pm clear com.tenaris.inateckscanner

# Forzar cierre
adb shell am force-stop com.tenaris.inateckscanner
```

### Inspección del dispositivo:
```powershell
# Información del dispositivo
adb shell getprop ro.product.model
adb shell getprop ro.build.version.release

# Espacio disponible
adb shell df -h

# Apps instaladas
adb shell pm list packages | Select-String "tenaris"
```

---

## 🎯 Atajos de Teclado (Visual Studio)

Si prefieres usar Visual Studio en lugar de scripts:

| Acción | Atajo |
|--------|-------|
| Compilar solución | `Ctrl + Shift + B` |
| Ejecutar (Debug) | `F5` |
| Ejecutar (Sin Debug) | `Ctrl + F5` |
| Limpiar solución | `Build → Clean Solution` |
| Reconstruir | `Build → Rebuild Solution` |

---

## 📦 Estructura de Salida

Después de ejecutar los scripts:

```
Inatek/
├── InateckBinding/
│   └── bin/Debug/net8.0-android/
│       └── InateckBinding.dll          ← Binding compilado
│
├── InateckMauiApp/
│   └── bin/Debug/net8.0-android34.0/
│       ├── InateckMauiApp.dll          ← App compilada
│       └── com.tenaris.inateckscanner-Signed.apk
│
└── output/
    └── com.tenaris.inateckscanner-Signed.apk  ← APK listo para instalar
```

---

## 🚀 Quick Start

**Para desarrolladores nuevos en el proyecto:**

```powershell
# 1. Clonar/Abrir proyecto
cd "C:\...\Inatek"

# 2. Compilar todo
.\build.ps1

# 3. Conectar Android y ejecutar
.\deploy.ps1 -Run

# ¡Listo! La app está corriendo en tu dispositivo
```

---

## 📞 Soporte

Si tienes problemas con los scripts:

1. Revisa la sección "Solución de Problemas" arriba
2. Consulta [BUILD_AND_DEPLOY.md](docs/BUILD_AND_DEPLOY.md) para más detalles
3. Verifica que todos los requisitos previos están instalados

---

**Última actualización:** 2025-12-15
**Versión:** 1.0
