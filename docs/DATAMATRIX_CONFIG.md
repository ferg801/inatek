# 📐 Configuración DataMatrix - Inatek BCST-75S

## 🎯 Objetivo

La aplicación ha sido configurada para que el escáner Inatek BCST-75S lea **EXCLUSIVAMENTE códigos DataMatrix**, deshabilitando todos los demás tipos de códigos (1D, QR, PDF417, etc.).

---

## ⚙️ Configuración Automática

### Al Conectar

Cuando te conectas al escáner, la aplicación **automáticamente**:

1. ✅ **Habilita DataMatrix** (area 27)
2. ❌ **Deshabilita todos los códigos 1D:**
   - Codabar, IATA25, Interleaved25, Matrix25, Standard25
   - Code39, Code93, Code128
   - EAN-8, EAN-13, UPC-A, UPC-E0, UPC-E1
   - MSI, Code11, Chinese Post, USPS/FedEx
   - GS1-128
3. ❌ **Deshabilita otros códigos 2D:**
   - QR Code
   - PDF417
   - Aztec
   - MaxiCode
   - Hanxin

**Total: 24 tipos de códigos deshabilitados, 1 habilitado (DataMatrix)**

---

## 🔧 Configuración Manual

### Desde la App

1. Conectar al escáner
2. Ir a la sección "Configuración de Códigos"
3. Presionar el botón **"🔧 Configurar para DataMatrix SOLO"**
4. Esperar confirmación: "Escáner configurado: SOLO DataMatrix habilitado"

### Flujo de la App

```
┌─────────────────────┐
│  Conectar Escáner   │
└──────────┬──────────┘
           │
           ├─→ Obtiene info (versión, batería)
           │
           ├─→ Espera 500ms (estabilización)
           │
           └─→ ConfigureForDataMatrixOnlyAsync()
                     │
                     ├─→ Envía JSON con 24 configuraciones
                     │
                     └─→ Confirmación: "SOLO DataMatrix habilitado"
```

---

## 📋 Comando JSON Enviado

El siguiente JSON se envía al escáner mediante `SetSettingInfo()`:

```json
[
  {"value":"1","area":"27","name":"datamatrix_on"},      // ✅ HABILITAR DataMatrix
  {"value":"0","area":"11","name":"codabar_on"},         // ❌ Deshabilitar
  {"value":"0","area":"11","name":"iata25_on"},
  {"value":"0","area":"11","name":"interleaved25_on"},
  {"value":"0","area":"11","name":"matrix25_on"},
  {"value":"0","area":"11","name":"standard25_on"},
  {"value":"0","area":"11","name":"code39_on"},
  {"value":"0","area":"11","name":"code93_on"},
  {"value":"0","area":"11","name":"code128_on"},
  {"value":"0","area":"12","name":"ean_8_on"},
  {"value":"0","area":"12","name":"ean_13_on"},
  {"value":"0","area":"12","name":"upc_a_on"},
  {"value":"0","area":"12","name":"upc_e0_on"},
  {"value":"0","area":"12","name":"upc_e1_on"},
  {"value":"0","area":"12","name":"msi_on"},
  {"value":"0","area":"12","name":"code11_on"},
  {"value":"0","area":"12","name":"chinese_post_on"},
  {"value":"0","area":"15","name":"usps_fedex"},
  {"value":"0","area":"25","name":"aztec_on"},
  {"value":"0","area":"25","name":"maxicode_on"},
  {"value":"0","area":"26","name":"hanxin_on"},
  {"value":"0","area":"28","name":"qrcode_on"},          // ❌ Deshabilitar QR
  {"value":"0","area":"29","name":"pdf417_on"},          // ❌ Deshabilitar PDF417
  {"value":"0","area":"32","name":"gs1_128"}
]
```

---

## 🧪 Pruebas

### Qué Debe Leer

✅ **Códigos DataMatrix** - Cualquier tamaño, cualquier contenido

### Qué NO Debe Leer

❌ Códigos de barras 1D (Code 39, Code 128, EAN, UPC, etc.)
❌ Códigos QR
❌ Códigos PDF417
❌ Cualquier otro tipo de código 2D

### Cómo Probar

1. **Escanear un código DataMatrix:**
   - El escáner debe leer correctamente
   - El dato aparece en "Último Código Leído"
   - Se muestra timestamp

2. **Escanear un código QR:**
   - El escáner NO debe leer
   - No debe aparecer ningún dato nuevo

3. **Escanear un código de barras (EAN, Code128):**
   - El escáner NO debe leer
   - No debe aparecer ningún dato nuevo

---

## 📊 Referencia de Áreas y Comandos

### DataMatrix (HABILITADO)

| Código | Area | Nombre | Valor | Estado |
|--------|------|--------|-------|--------|
| DataMatrix | 27 | datamatrix_on | 1 | ✅ ON |

### Códigos 1D (DESHABILITADOS)

| Código | Area | Nombre | Valor | Estado |
|--------|------|--------|-------|--------|
| Codabar | 11 | codabar_on | 0 | ❌ OFF |
| IATA25 | 11 | iata25_on | 0 | ❌ OFF |
| Interleaved25 | 11 | interleaved25_on | 0 | ❌ OFF |
| Matrix25 | 11 | matrix25_on | 0 | ❌ OFF |
| Standard25 | 11 | standard25_on | 0 | ❌ OFF |
| Code39 | 11 | code39_on | 0 | ❌ OFF |
| Code93 | 11 | code93_on | 0 | ❌ OFF |
| Code128 | 11 | code128_on | 0 | ❌ OFF |
| EAN-8 | 12 | ean_8_on | 0 | ❌ OFF |
| EAN-13 | 12 | ean_13_on | 0 | ❌ OFF |
| UPC-A | 12 | upc_a_on | 0 | ❌ OFF |
| UPC-E0 | 12 | upc_e0_on | 0 | ❌ OFF |
| UPC-E1 | 12 | upc_e1_on | 0 | ❌ OFF |
| MSI | 12 | msi_on | 0 | ❌ OFF |
| Code11 | 12 | code11_on | 0 | ❌ OFF |
| Chinese Post | 12 | chinese_post_on | 0 | ❌ OFF |
| USPS/FedEx | 15 | usps_fedex | 0 | ❌ OFF |
| GS1-128 | 32 | gs1_128 | 0 | ❌ OFF |

### Códigos 2D (DESHABILITADOS)

| Código | Area | Nombre | Valor | Estado |
|--------|------|--------|-------|--------|
| QR Code | 28 | qrcode_on | 0 | ❌ OFF |
| PDF417 | 29 | pdf417_on | 0 | ❌ OFF |
| Aztec | 25 | aztec_on | 0 | ❌ OFF |
| MaxiCode | 25 | maxicode_on | 0 | ❌ OFF |
| Hanxin | 26 | hanxin_on | 0 | ❌ OFF |

---

## 🔄 Restaurar Configuración

Si necesitas **habilitar otros códigos** en el futuro:

### Opción 1: Modificar el código

Editar [InateckScannerWrapper.cs:400-425](../InateckBinding/Additions/InateckScannerWrapper.cs#L400-L425):

```csharp
// Para habilitar QR también:
{""value"":""1"",""area"":""28"",""name"":""qrcode_on""}  // Cambiar 0 a 1

// Para habilitar PDF417 también:
{""value"":""1"",""area"":""29"",""name"":""pdf417_on""}  // Cambiar 0 a 1
```

### Opción 2: Crear nuevo método

```csharp
public async Task<bool> ConfigureForMultipleCodesAsync()
{
    var config = @"[
        {""value"":""1"",""area"":""27"",""name"":""datamatrix_on""},
        {""value"":""1"",""area"":""28"",""name"":""qrcode_on""},
        {""value"":""1"",""area"":""29"",""name"":""pdf417_on""}
    ]";
    return await SetSettingsAsync(config);
}
```

---

## 📝 Archivos Modificados

Los siguientes archivos fueron modificados para soportar DataMatrix:

1. ✅ [IScannerService.cs](../InateckMauiApp/Services/IScannerService.cs#L128-L133)
   - Agregado método `ConfigureForDataMatrixOnlyAsync()`

2. ✅ [InateckScannerWrapper.cs](../InateckBinding/Additions/InateckScannerWrapper.cs#L385-L447)
   - Implementado `ConfigureForDataMatrixOnlyAsync()` con JSON completo

3. ✅ [AndroidScannerService.cs](../InateckMauiApp/Platforms/Android/AndroidScannerService.cs#L266-L296)
   - Implementado método del servicio

4. ✅ [MainViewModel.cs](../InateckMauiApp/ViewModels/MainViewModel.cs)
   - Agregado comando `ConfigureDataMatrixCommand`
   - Configuración automática al conectar (línea 177-179)

5. ✅ [MainPage.xaml](../InateckMauiApp/Views/MainPage.xaml#L165-L193)
   - Sección UI destacada para configuración DataMatrix

6. ✅ [MauiProgram.cs](../InateckMauiApp/MauiProgram.cs#L93)
   - Mock service actualizado

---

## 🎓 Recursos Adicionales

- **Documentación completa de comandos:** [info.md](../InateckSDK/info.md)
- **API Mapping:** [API_MAPPING.md](API_MAPPING.md)
- **Guía de compilación:** [BUILD_AND_DEPLOY.md](BUILD_AND_DEPLOY.md)

---

## ✅ Checklist de Validación

- [ ] App instalada en dispositivo físico
- [ ] Conexión exitosa al escáner Inatek BCST-75S
- [ ] Configuración automática ejecutada (mensaje confirmación)
- [ ] Código DataMatrix se lee correctamente
- [ ] Código QR NO se lee (validación negativa)
- [ ] Código de barras 1D NO se lee (validación negativa)
- [ ] Botón manual funciona para reconfigurar

---

**Última actualización:** 2025-12-15
**Versión:** 1.0
**Estado:** ✅ Implementado y listo para pruebas
