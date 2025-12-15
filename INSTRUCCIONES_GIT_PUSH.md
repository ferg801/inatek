# Instrucciones para Subir el Proyecto a GitHub

El proyecto ya está listo y commiteado localmente. Solo falta hacer push a GitHub.

## Estado Actual

- ✅ Repositorio Git local inicializado
- ✅ Commit inicial creado con todos los archivos
- ✅ Repositorio remoto en GitHub: https://github.com/ferg801/inatek.git
- ⏳ Falta: Push del código

## Opción 1: Usar GitHub CLI (Recomendado)

### En Windows:

1. **Instalar GitHub CLI:**
   ```powershell
   winget install GitHub.cli
   ```
   O descargar desde: https://cli.github.com/

2. **Autenticar:**
   ```bash
   gh auth login
   ```
   - Selecciona: GitHub.com
   - Selecciona: HTTPS
   - Autenticar con: Login with a web browser
   - Copia el código y pégalo en el navegador

3. **Push del código:**
   ```bash
   git push -u origin main
   ```

### En Mac (cuando clones):

1. **Instalar GitHub CLI:**
   ```bash
   brew install gh
   ```

2. **Autenticar:**
   ```bash
   gh auth login
   ```

3. **Clonar el repositorio:**
   ```bash
   gh repo clone ferg801/inatek
   ```

---

## Opción 2: Usar Personal Access Token (PAT)

### Crear PAT en GitHub:

1. Ve a: https://github.com/settings/tokens
2. Click en "Generate new token" → "Generate new token (classic)"
3. Configuración:
   - Note: `inatek-scanner-project`
   - Expiration: 90 days (o lo que prefieras)
   - Scopes: ✅ `repo` (todos los sub-items)
4. Click "Generate token"
5. **COPIA EL TOKEN** (lo necesitarás en el siguiente paso)

### Configurar Git con el Token:

#### En Windows:

```bash
git config --global credential.helper wincred
```

Luego haz push (te pedirá usuario y contraseña):
```bash
git push -u origin main
```

- **Username:** `ferg801`
- **Password:** `[PEGA_TU_TOKEN_AQUÍ]` (NO tu contraseña de GitHub)

#### En Mac:

```bash
git config --global credential.helper osxkeychain
```

Luego haz push:
```bash
git push -u origin main
```

- **Username:** `ferg801`
- **Password:** `[TU_TOKEN]`

---

## Opción 3: Usar SSH (Más Seguro)

### Generar clave SSH:

```bash
ssh-keygen -t ed25519 -C "tu-email@example.com"
```

Presiona Enter 3 veces (acepta ubicación por defecto y sin passphrase)

### Agregar la clave a GitHub:

1. Copiar la clave pública:

   **Windows:**
   ```powershell
   cat ~/.ssh/id_ed25519.pub | clip
   ```

   **Mac:**
   ```bash
   cat ~/.ssh/id_ed25519.pub | pbcopy
   ```

2. Ve a: https://github.com/settings/ssh/new
3. Pega la clave
4. Click "Add SSH key"

### Cambiar el remote a SSH:

```bash
git remote set-url origin git@github.com:ferg801/inatek.git
```

### Push:

```bash
git push -u origin main
```

---

## Verificar que funcionó

Una vez que hagas push exitosamente, verifica en:
https://github.com/ferg801/inatek

Deberías ver todos los archivos del proyecto.

---

## Para continuar en Mac

Una vez que el código esté en GitHub:

### 1. Instalar requisitos en Mac:

```bash
# Instalar Homebrew (si no lo tienes)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Instalar .NET SDK 8.0
brew install --cask dotnet-sdk

# Verificar instalación
dotnet --version

# Instalar workload de MAUI
dotnet workload install maui
```

### 2. Clonar el repositorio:

```bash
cd ~/Projects  # o donde quieras trabajar
git clone https://github.com/ferg801/inatek.git
cd inatek
```

### 3. Abrir en VS Code:

```bash
code .
```

### 4. Compilar el proyecto:

```bash
dotnet build InateckScanner.sln
```

---

## Notas Importantes

- **Mac no puede compilar la app Android directamente** porque el binding requiere Android SDK
- **Opciones en Mac:**
  1. Usar Visual Studio Code para editar código
  2. Usar Visual Studio 2022 for Mac (si está disponible)
  3. Usar una VM con Windows
  4. Hacer cambios en Mac y compilar/probar en Windows o en un dispositivo Android remoto

- **Lo que SÍ puedes hacer en Mac:**
  - Editar código C#, XAML, documentación
  - Hacer commits
  - Push/pull de GitHub
  - Revisar y planificar

- **Lo que NO puedes hacer en Mac:**
  - Compilar el Android Binding (requiere Android SDK específico)
  - Generar APK
  - Probar en dispositivo Android directamente

---

## ¿Necesitas ayuda?

Si tienes problemas con alguno de estos pasos, consulta:
- GitHub Docs: https://docs.github.com/
- .NET MAUI en Mac: https://learn.microsoft.com/dotnet/maui/get-started/installation?tabs=macos
