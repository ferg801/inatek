# ========================================
# Script de Deploy - Inatek Scanner
# ========================================

param(
    [switch]$Release,
    [switch]$Install,
    [switch]$Run
)

$configuration = if ($Release) { "Release" } else { "Debug" }

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " DEPLOY INATEK SCANNER APP          " -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuración: $configuration" -ForegroundColor Yellow
Write-Host ""

# ========================================
# VERIFICAR DISPOSITIVO ANDROID
# ========================================
Write-Host "Verificando dispositivos Android conectados..." -ForegroundColor Cyan

try {
    $adbDevices = adb devices 2>&1

    if ($adbDevices -match "device$") {
        Write-Host "✓ Dispositivo Android conectado" -ForegroundColor Green

        # Mostrar detalles del dispositivo
        $deviceModel = adb shell getprop ro.product.model
        $androidVersion = adb shell getprop ro.build.version.release

        Write-Host "  Modelo: $deviceModel" -ForegroundColor Gray
        Write-Host "  Android: $androidVersion" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "✗ ERROR: No hay dispositivos Android conectados" -ForegroundColor Red
        Write-Host ""
        Write-Host "Pasos para conectar:" -ForegroundColor Yellow
        Write-Host "  1. Conecta el dispositivo Android vía USB" -ForegroundColor White
        Write-Host "  2. Habilita 'Depuración USB' en Opciones de Desarrollador" -ForegroundColor White
        Write-Host "  3. Acepta el permiso de depuración en el dispositivo" -ForegroundColor White
        Write-Host "  4. Ejecuta: adb devices" -ForegroundColor White
        exit 1
    }
} catch {
    Write-Host "✗ ERROR: ADB (Android Debug Bridge) no encontrado" -ForegroundColor Red
    Write-Host "  Instala Android SDK Platform Tools o Visual Studio con workload Android" -ForegroundColor Yellow
    exit 1
}

# ========================================
# COMPILAR PROYECTO
# ========================================
Write-Host "Compilando proyecto para Android..." -ForegroundColor Cyan

dotnet build InateckMauiApp\InateckMauiApp.csproj -c $configuration -f net8.0-android34.0

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ ERROR: Falló la compilación" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Compilación exitosa" -ForegroundColor Green
Write-Host ""

# ========================================
# PUBLICAR APK
# ========================================
Write-Host "Generando APK..." -ForegroundColor Cyan

dotnet publish InateckMauiApp\InateckMauiApp.csproj `
    -c $configuration `
    -f net8.0-android34.0 `
    -o output

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ ERROR: Falló la generación del APK" -ForegroundColor Red
    exit 1
}

Write-Host "✓ APK generado correctamente" -ForegroundColor Green
Write-Host ""

# Buscar el APK generado
$apkPath = Get-ChildItem -Path "output" -Filter "*.apk" | Select-Object -First 1

if ($apkPath) {
    Write-Host "APK encontrado:" -ForegroundColor Cyan
    Write-Host "  $($apkPath.FullName)" -ForegroundColor Gray
    Write-Host "  Tamaño: $([math]::Round($apkPath.Length / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "⚠ No se encontró el APK en la carpeta output" -ForegroundColor Yellow
    exit 1
}

# ========================================
# INSTALAR EN DISPOSITIVO
# ========================================
if ($Install -or $Run) {
    Write-Host "Instalando APK en el dispositivo..." -ForegroundColor Cyan

    adb install -r $apkPath.FullName

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Aplicación instalada correctamente" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "✗ ERROR: Falló la instalación del APK" -ForegroundColor Red
        exit 1
    }
}

# ========================================
# EJECUTAR APLICACIÓN
# ========================================
if ($Run) {
    Write-Host "Iniciando aplicación en el dispositivo..." -ForegroundColor Cyan

    # Nombre del paquete de la app
    $packageName = "com.tenaris.inateckscanner"
    $activityName = "$packageName.MainActivity"

    adb shell am start -n "$activityName"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Aplicación iniciada" -ForegroundColor Green
        Write-Host ""
        Write-Host "Ver logs en tiempo real:" -ForegroundColor Cyan
        Write-Host "  adb logcat | grep -i 'inatek\|scanner\|tenaris'" -ForegroundColor Yellow
    } else {
        Write-Host "⚠ No se pudo iniciar la aplicación automáticamente" -ForegroundColor Yellow
        Write-Host "  Inicia manualmente desde el dispositivo" -ForegroundColor Gray
    }
}

# ========================================
# RESUMEN
# ========================================
Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host " DEPLOY COMPLETADO                  " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

if (-not $Install -and -not $Run) {
    Write-Host "Para instalar en el dispositivo:" -ForegroundColor Cyan
    Write-Host "  .\deploy.ps1 -Install" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Para instalar y ejecutar:" -ForegroundColor Cyan
    Write-Host "  .\deploy.ps1 -Run" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Para compilar en Release:" -ForegroundColor Cyan
    Write-Host "  .\deploy.ps1 -Release -Run" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Comandos útiles de ADB:" -ForegroundColor Cyan
Write-Host "  adb devices                     # Listar dispositivos" -ForegroundColor Gray
Write-Host "  adb logcat                      # Ver logs" -ForegroundColor Gray
Write-Host "  adb uninstall $packageName      # Desinstalar app" -ForegroundColor Gray
Write-Host "  adb shell pm list packages      # Listar apps instaladas" -ForegroundColor Gray
Write-Host ""
