# 📖 Manual de Usuario - Inatek Scanner Test

## Índice

1. [Introducción](#introducción)
2. [Requisitos](#requisitos)
3. [Instalación](#instalación)
4. [Interfaz de la Aplicación](#interfaz-de-la-aplicación)
5. [Guía de Uso Paso a Paso](#guía-de-uso-paso-a-paso)
6. [Funcionalidades](#funcionalidades)
7. [Lectura de Códigos](#lectura-de-códigos)
8. [Solución de Problemas](#solución-de-problemas)
9. [Especificaciones Técnicas](#especificaciones-técnicas)

---

## Introducción

**Inatek Scanner Test** es una aplicación de prueba para conectar y utilizar escáneres de códigos de barras Inatek (modelo BCST-75S y compatibles) mediante Bluetooth Low Energy (BLE).

### Características principales:
- ✅ Escaneo y detección de dispositivos BLE
- ✅ Conexión/desconexión con el escáner
- ✅ Lectura de códigos de barras (optimizado para DataMatrix)
- ✅ Consulta de información del dispositivo (versión, batería)
- ✅ Control de volumen del escáner

---

## Requisitos

### Dispositivo Android
- **Versión mínima:** Android 10 (API 29)
- **Bluetooth:** BLE 4.0 o superior
- **Ubicación:** GPS activado (requerido para escaneo BLE)

### Escáner Inatek
- Modelo compatible: **BCST-75S** o similar
- Batería cargada
- Encendido y en modo de emparejamiento

### Permisos necesarios
La aplicación solicitará los siguientes permisos:

| Permiso | Descripción |
|---------|-------------|
| Bluetooth | Para comunicación con el escáner |
| Ubicación | Requerido por Android para escaneo BLE |
| Bluetooth Scan* | Descubrir dispositivos cercanos |
| Bluetooth Connect* | Establecer conexión con el escáner |

*Permisos adicionales en Android 12+

---

## Instalación

### Opción 1: Instalar APK manualmente
1. Transferir el archivo `com.tenaris.inateckscanner-Signed.apk` al dispositivo
2. Abrir el archivo APK desde el explorador de archivos
3. Permitir instalación de fuentes desconocidas si se solicita
4. Seguir las instrucciones en pantalla

### Opción 2: Instalar desde computadora
```bash
adb install -r com.tenaris.inateckscanner-Signed.apk
```

---

## Interfaz de la Aplicación

### Pantalla Principal

```
┌─────────────────────────────────────┐
│        🔷 Inatek Scanner Test       │
├─────────────────────────────────────┤
│  Estado: [Inicializado/Conectado]   │
│  Mensaje: Presione inicializar...   │
├─────────────────────────────────────┤
│  [Inicializar]  [Escanear]  [Stop]  │
├─────────────────────────────────────┤
│  📱 Dispositivos encontrados:       │
│  ┌─────────────────────────────┐    │
│  │ BCST-75S (AA:BB:CC:DD:EE)  │    │
│  └─────────────────────────────┘    │
├─────────────────────────────────────┤
│  [Conectar]         [Desconectar]   │
├─────────────────────────────────────┤
│  📊 Información del dispositivo     │
│  Versión: v1.2.3                    │
│  Batería: 85%                       │
│  [Obtener Info]                     │
├─────────────────────────────────────┤
│  🔊 Volumen: [====|----] 2          │
│  [Aplicar Volumen]                  │
├─────────────────────────────────────┤
│  📷 Última lectura:                 │
│  "ABC123456789"                     │
├─────────────────────────────────────┤
│  ⚙️ DataMatrix                      │
│  [Configurar Solo DataMatrix]       │
└─────────────────────────────────────┘
```

### Elementos de la interfaz

| Elemento | Descripción |
|----------|-------------|
| **Estado** | Muestra el estado actual (No inicializado/Inicializado/Conectado) |
| **Mensaje** | Información en tiempo real sobre las operaciones |
| **Inicializar** | Prepara el servicio Bluetooth |
| **Escanear** | Busca dispositivos BLE cercanos |
| **Stop** | Detiene el escaneo en curso |
| **Lista de dispositivos** | Muestra los escáneres encontrados |
| **Conectar/Desconectar** | Gestiona la conexión con el escáner |
| **Información** | Versión de firmware y nivel de batería |
| **Volumen** | Control deslizante (niveles 0-4) |
| **Última lectura** | Muestra el último código escaneado |

---

## Guía de Uso Paso a Paso

### 1️⃣ Paso 1: Preparación

1. **Encienda el escáner Inatek**
   - Mantenga presionado el botón de encendido hasta que el LED parpadee
   - El escáner debe estar en modo Bluetooth

2. **Active Bluetooth y Ubicación** en su dispositivo Android
   - Configuración → Bluetooth → Activar
   - Configuración → Ubicación → Activar

3. **Abra la aplicación** "Inatek Scanner Test"

### 2️⃣ Paso 2: Inicialización

1. Presione el botón **"Inicializar Servicio"**
2. **Acepte los permisos** cuando se soliciten:
   - Bluetooth
   - Ubicación
3. Espere el mensaje: *"Servicio inicializado correctamente"*
4. El indicador de estado cambiará a **verde**

### 3️⃣ Paso 3: Escanear Dispositivos

1. Presione **"Escanear Dispositivos"**
2. Espere aproximadamente 10 segundos
3. Los dispositivos encontrados aparecerán en la lista
4. Busque su escáner (normalmente aparece como "BCST-75S" o similar)

> 💡 **Tip:** Si no encuentra su escáner, asegúrese de que esté encendido y cerca del dispositivo Android.

### 4️⃣ Paso 4: Conectar

1. **Toque sobre el escáner** en la lista para seleccionarlo
2. Presione **"Conectar"**
3. Espere los siguientes mensajes:
   - *"Conectando a BCST-75S..."*
   - *"Configurando escáner para DataMatrix..."*
   - *"✓ Conectado a BCST-75S"*
4. El estado cambiará a **"Conectado"** (indicador azul)

### 5️⃣ Paso 5: Escanear Códigos

1. **Apunte el escáner** hacia un código de barras o DataMatrix
2. **Presione el gatillo** del escáner
3. El código leído aparecerá en la sección **"Última lectura"**
4. Se escuchará un beep de confirmación (según configuración de volumen)

### 6️⃣ Paso 6: Desconectar

1. Presione **"Desconectar"**
2. Espere el mensaje: *"Desconectado"*
3. El escáner quedará disponible para nuevas conexiones

---

## Funcionalidades

### Obtener Información del Dispositivo

Después de conectar, presione **"Obtener Info"** para ver:
- **Versión del firmware:** Ej. "v1.2.3"
- **Nivel de batería:** Porcentaje actual

### Control de Volumen

1. Use el **control deslizante** para seleccionar el nivel (0-4):
   - **0:** Silencio
   - **1:** Bajo
   - **2:** Medio (predeterminado)
   - **3:** Alto
   - **4:** Máximo
2. Presione **"Aplicar Volumen"** para confirmar el cambio

### Configuración DataMatrix

La aplicación está optimizada para leer códigos **DataMatrix**:
- Al conectar, se configura automáticamente para priorizar DataMatrix
- Use el botón **"Configurar Solo DataMatrix"** para reconfigurar manualmente

---

## Lectura de Códigos

### Códigos Soportados

| Tipo | Soporte | Notas |
|------|---------|-------|
| **DataMatrix** | ✅ Óptimo | Configuración predeterminada |
| Code 128 | ⚠️ Variable | Puede requerir configuración |
| QR Code | ⚠️ Variable | Puede requerir configuración |
| EAN/UPC | ⚠️ Variable | Puede requerir configuración |

### Interpretación de Lecturas

La información del código escaneado aparece en el campo **"Última lectura"** en formato texto plano.

### Consejos para Mejor Lectura

1. **Distancia:** Mantenga el escáner a 10-30 cm del código
2. **Ángulo:** Apunte directamente al código, evite ángulos extremos
3. **Iluminación:** Evite reflejos directos sobre el código
4. **Calidad:** Asegúrese de que el código esté limpio y sin daños

---

## Solución de Problemas

### ❌ "No se encontraron dispositivos"

**Causas posibles:**
- Escáner apagado o fuera de rango
- Bluetooth desactivado
- Ubicación desactivada

**Solución:**
1. Verifique que el escáner esté encendido (LED parpadeando)
2. Active Bluetooth y Ubicación en el dispositivo
3. Acerque el escáner al dispositivo Android
4. Intente escanear nuevamente

### ❌ "Error al conectar"

**Causas posibles:**
- Escáner ya conectado a otro dispositivo
- Interferencia de señal
- Batería baja del escáner

**Solución:**
1. Desconecte el escáner de otros dispositivos
2. Reinicie el escáner (apagar y encender)
3. Cargue la batería del escáner
4. Intente conectar nuevamente

### ❌ "Permisos denegados"

**Solución:**
1. Vaya a Configuración → Aplicaciones → Inatek Scanner Test
2. Seleccione "Permisos"
3. Active Bluetooth y Ubicación
4. Reinicie la aplicación

### ❌ "El escáner no lee códigos"

**Causas posibles:**
- Tipo de código no soportado
- Código dañado o de baja calidad
- Escáner no configurado correctamente

**Solución:**
1. Verifique que el código sea legible
2. Presione "Configurar Solo DataMatrix" si está leyendo DataMatrix
3. Pruebe con otro código de barras
4. Reinicie la conexión

### ❌ "Conexión se pierde frecuentemente"

**Solución:**
1. Mantenga el escáner cerca del dispositivo (< 10 metros)
2. Evite obstáculos entre escáner y dispositivo
3. Verifique el nivel de batería del escáner
4. Desactive otros dispositivos Bluetooth cercanos

---

## Especificaciones Técnicas

### Aplicación
| Especificación | Valor |
|----------------|-------|
| Nombre | Inatek Scanner Test |
| Paquete | com.tenaris.inateckscanner |
| Versión | 1.0 |
| Plataforma | Android |
| SDK mínimo | API 29 (Android 10) |
| Framework | .NET MAUI 9 |

### Escáner Compatible
| Especificación | Valor |
|----------------|-------|
| Modelo | BCST-75S |
| Conexión | Bluetooth Low Energy (BLE) |
| Protocolo | Inatek SDK v1.x |

### Comunicación
| Característica | Descripción |
|----------------|-------------|
| Protocolo | BLE GATT |
| Servicios | Inatek proprietary |
| MTU | Automático |

---

## Contacto y Soporte

Para reportar problemas o solicitar asistencia:
- **Proyecto:** Inatek Scanner - Tenaris
- **Ubicación del código:** `/inateck-scanner/`

---

*Manual actualizado: Diciembre 2025*
