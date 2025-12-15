# 🍎 Instrucciones para Continuar en Mac

## ✅ Estado Actual

El proyecto **Inatek Scanner** ya está completamente subido a GitHub:

🔗 **Repositorio:** https://github.com/ferg801/inatek

---

## 📋 Requisitos en Mac

### 1. Instalar Homebrew (si no lo tienes)

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### 2. Instalar .NET SDK 8.0

```bash
# Instalar .NET SDK
brew install --cask dotnet-sdk

# Verificar instalación
dotnet --version
# Debe mostrar: 8.0.x
```

### 3. Instalar .NET MAUI Workload

```bash
dotnet workload install maui
```

Esto puede tardar varios minutos. Es necesario para trabajar con proyectos MAUI.

### 4. Instalar Visual Studio Code

```bash
brew install --cask visual-studio-code
```

### 5. Extensiones Recomendadas para VS Code

Una vez que abras el proyecto, instala estas extensiones:

- **C# Dev Kit** (Microsoft)
- **.NET MAUI** (Microsoft)
- **C#** (Microsoft)
- **GitLens** (opcional, para mejor integración Git)

---

## 🚀 Clonar el Proyecto

### Opción 1: Con HTTPS

```bash
# Ir a tu carpeta de proyectos
cd ~/Documents/Proyectos  # o donde quieras trabajar

# Clonar el repositorio
git clone https://github.com/ferg801/inatek.git

# Entrar al proyecto
cd inatek
```

### Opción 2: Con SSH (más seguro)

Si prefieres usar SSH (recomendado):

1. **Generar clave SSH:**
   ```bash
   ssh-keygen -t ed25519 -C "tu-email@tenaris.com"
   ```
   Presiona Enter 3 veces (ubicación por defecto, sin passphrase)

2. **Copiar la clave pública:**
   ```bash
   cat ~/.ssh/id_ed25519.pub | pbcopy
   ```

3. **Agregar a GitHub:**
   - Ve a: https://github.com/settings/ssh/new
   - Pega la clave
   - Click "Add SSH key"

4. **Clonar con SSH:**
   ```bash
   git clone git@github.com:ferg801/inatek.git
   cd inatek
   ```

---

## 💻 Abrir en VS Code

```bash
# Desde la carpeta del proyecto
code .
```

O simplemente arrastra la carpeta `inatek` al ícono de VS Code.

---

## 🔨 Compilar el Proyecto (IMPORTANTE)

### ⚠️ Limitación en Mac

**El proyecto NO se puede compilar completamente en Mac** porque:
- El Android Binding requiere Android SDK específico de Windows
- .NET MAUI para Android funciona mejor en Windows

### ✅ Lo que SÍ puedes hacer en Mac:

1. **Editar código C#:**
   - Todos los archivos `.cs`
   - ViewModels, Services, etc.

2. **Editar XAML:**
   - Interfaces de usuario (`.xaml`)

3. **Editar documentación:**
   - Archivos `.md`

4. **Control de versiones:**
   - Git commits, push, pull, branches

5. **Revisar la solución:**
   ```bash
   # Ver estructura del proyecto
   dotnet sln list

   # Restaurar dependencias (sin compilar)
   dotnet restore
   ```

### ❌ Lo que NO puedes hacer en Mac:

- Compilar el Android Binding Library
- Generar APK
- Probar en dispositivo Android
- Build completo del proyecto

---

## 🔄 Flujo de Trabajo Recomendado

### Si trabajas en Mac:

1. **Hacer cambios en Mac:**
   ```bash
   # Editar archivos en VS Code
   code .

   # Hacer commit de cambios
   git add .
   git commit -m "Descripción de cambios"

   # Subir a GitHub
   git push
   ```

2. **Compilar en Windows:**
   - Ve a tu PC con Windows
   - Abre PowerShell en la carpeta del proyecto
   - Ejecuta:
     ```bash
     git pull
     .\build.ps1
     ```

3. **Probar en dispositivo:**
   ```bash
   .\deploy.ps1 -Run
   ```

---

## 📂 Estructura del Proyecto

```
inatek/
├── InateckBinding/              # Android Binding (requiere Windows)
│   ├── Jars/                    # Archivos JAR del SDK
│   └── Additions/               # Wrapper C#
├── InateckMauiApp/              # Aplicación MAUI (editable en Mac)
│   ├── Services/                # Lógica de negocio
│   ├── ViewModels/              # MVVM ViewModels
│   ├── Views/                   # XAML interfaces
│   └── Platforms/Android/       # Código específico Android
├── docs/                        # Documentación
├── build.ps1                    # Script de compilación (Windows)
├── deploy.ps1                   # Script de deployment (Windows)
└── README.md                    # Documentación principal
```

---

## 🛠️ Comandos Útiles en Mac

### Git

```bash
# Ver estado
git status

# Ver cambios
git diff

# Crear rama nueva
git checkout -b feature/nueva-funcionalidad

# Ver ramas
git branch

# Cambiar de rama
git checkout main

# Actualizar desde GitHub
git pull

# Subir cambios
git push
```

### .NET (limitado en Mac)

```bash
# Ver versión de .NET
dotnet --version

# Listar proyectos en la solución
dotnet sln list

# Restaurar paquetes NuGet
dotnet restore

# Ver información del proyecto
dotnet list package
```

---

## 📝 Notas Importantes

### 1. Codificación de Archivos

- En Mac, los archivos se guardan con terminadores de línea LF
- En Windows, se usan CRLF
- Git está configurado para manejar esto automáticamente (`.gitattributes`)

### 2. Rutas de Archivos

- Mac usa `/` (slash)
- Windows usa `\` (backslash)
- En C#, usa `Path.Combine()` para rutas multiplataforma

### 3. Scripts PowerShell

- Los archivos `.ps1` NO funcionan en Mac
- Son solo para Windows
- En Mac, puedes crear scripts `.sh` equivalentes si necesitas

---

## 🆘 Solución de Problemas

### Error al instalar .NET MAUI workload

```bash
# Limpiar workloads
dotnet workload clean

# Reinstalar
dotnet workload install maui --source https://api.nuget.org/v3/index.json
```

### Error de permisos al instalar con Homebrew

```bash
# Dar permisos
sudo chown -R $(whoami) /usr/local/share/dotnet
```

### VS Code no reconoce archivos .cs

1. Verifica que instalaste "C# Dev Kit"
2. Reinicia VS Code
3. Abre la paleta de comandos (Cmd+Shift+P)
4. Busca: "Developer: Reload Window"

---

## 📚 Recursos Adicionales

- **Documentación .NET MAUI:** https://learn.microsoft.com/dotnet/maui/
- **.NET en Mac:** https://learn.microsoft.com/dotnet/core/install/macos
- **Git Basics:** https://git-scm.com/book/en/v2
- **VS Code C#:** https://code.visualstudio.com/docs/languages/csharp

---

## ✅ Checklist de Instalación en Mac

- [ ] Homebrew instalado
- [ ] .NET SDK 8.0 instalado (`dotnet --version`)
- [ ] .NET MAUI workload instalado
- [ ] VS Code instalado
- [ ] Extensiones de C# instaladas en VS Code
- [ ] Repositorio clonado desde GitHub
- [ ] Proyecto abierto en VS Code

---

## 🎯 Próximos Pasos

Una vez que tengas todo instalado:

1. **Familiarízate con el código:**
   - Lee [README.md](README.md)
   - Revisa [docs/PROYECTO_COMPLETO.md](docs/PROYECTO_COMPLETO.md)
   - Explora la estructura del proyecto

2. **Haz un cambio pequeño:**
   - Edita un comentario o mensaje
   - Haz commit
   - Push a GitHub
   - Verifica en Windows que funciona

3. **Planifica nuevas funcionalidades:**
   - Usa Mac para diseño y desarrollo
   - Usa Windows para compilación y pruebas

---

**¡Bienvenido al desarrollo multiplataforma!** 🚀

---

**Última actualización:** 2025-12-15
**Versión:** 1.0
