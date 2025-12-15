# ========================================
# Script de Compilacion - Inatek Scanner
# ========================================

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " COMPILACION INATEK SCANNER BINDING " -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que dotnet esta instalado
try {
    $dotnetVersion = dotnet --version
    Write-Host "OK .NET SDK encontrado: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET SDK no encontrado" -ForegroundColor Red
    Write-Host "  Descarga e instala desde: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# ========================================
# PASO 1: RESTAURAR DEPENDENCIAS
# ========================================
Write-Host "PASO 1: Restaurando dependencias NuGet..." -ForegroundColor Cyan

dotnet restore InateckScanner.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo la restauracion de dependencias" -ForegroundColor Red
    exit 1
}

Write-Host "OK Dependencias restauradas correctamente" -ForegroundColor Green
Write-Host ""

# ========================================
# PASO 2: COMPILAR BINDING
# ========================================
Write-Host "PASO 2: Compilando Android Binding Library..." -ForegroundColor Cyan

dotnet build InateckBinding\InateckBinding.csproj -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo la compilacion del binding" -ForegroundColor Red
    Write-Host "  Revisa los errores anteriores y corrige el archivo Metadata.xml si es necesario" -ForegroundColor Yellow
    exit 1
}

Write-Host "OK Binding compilado correctamente" -ForegroundColor Green
Write-Host ""

# ========================================
# PASO 3: COMPILAR APLICACION MAUI
# ========================================
Write-Host "PASO 3: Compilando aplicacion .NET MAUI..." -ForegroundColor Cyan

dotnet build InateckMauiApp\InateckMauiApp.csproj -c Debug -f net8.0-android34.0

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo la compilacion de la app MAUI" -ForegroundColor Red
    exit 1
}

Write-Host "OK Aplicacion MAUI compilada correctamente" -ForegroundColor Green
Write-Host ""

# ========================================
# PASO 4: COMPILAR SOLUCION COMPLETA
# ========================================
Write-Host "PASO 4: Compilando solucion completa..." -ForegroundColor Cyan

dotnet build InateckScanner.sln -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Fallo la compilacion de la solucion" -ForegroundColor Red
    exit 1
}

Write-Host "OK Solucion completa compilada correctamente" -ForegroundColor Green
Write-Host ""

# ========================================
# RESUMEN
# ========================================
Write-Host "=====================================" -ForegroundColor Green
Write-Host " COMPILACION COMPLETADA CON EXITO   " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

Write-Host "Archivos generados:" -ForegroundColor Cyan
Write-Host "  - InateckBinding.dll" -ForegroundColor White
Write-Host "    -> InateckBinding\bin\Debug\net8.0-android\InateckBinding.dll" -ForegroundColor Gray
Write-Host ""
Write-Host "  - InateckMauiApp.dll" -ForegroundColor White
Write-Host "    -> InateckMauiApp\bin\Debug\net8.0-android34.0\InateckMauiApp.dll" -ForegroundColor Gray
Write-Host ""

Write-Host "Proximos pasos:" -ForegroundColor Cyan
Write-Host "  1. Conectar dispositivo Android fisico via USB" -ForegroundColor White
Write-Host "  2. Ejecutar: .\deploy.ps1" -ForegroundColor Yellow
Write-Host "     O desde Visual Studio: F5" -ForegroundColor Yellow
Write-Host ""

# Verificar si hay dispositivos Android conectados
Write-Host "Verificando dispositivos Android..." -ForegroundColor Cyan
try {
    $devices = adb devices 2>$null
    if ($devices -match "device$") {
        Write-Host "OK Dispositivo Android detectado" -ForegroundColor Green
    } else {
        Write-Host "AVISO: No se detectaron dispositivos Android conectados" -ForegroundColor Yellow
        Write-Host "  Conecta un dispositivo Android via USB para continuar" -ForegroundColor Gray
    }
} catch {
    Write-Host "AVISO: ADB no disponible (Android Debug Bridge no encontrado)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Compilacion finalizada!" -ForegroundColor Green
