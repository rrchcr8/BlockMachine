@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

set "INSTALL_DIR=C:\BlockMachine"
set "SCRIPT_DIR=%~dp0"
set "SOURCE_DIR="

echo ========================================
echo   Block Machine - Instalador
echo ========================================
echo.

if /I "%~1"=="--compilar" set "DO_BUILD=1"
if /I "%~1"=="-compilar" set "DO_BUILD=1"
if /I "%~1"=="--build" set "DO_BUILD=1"

if "%DO_BUILD%"=="1" call :BuildProject

call :FindPublishFolder

if not defined SOURCE_DIR (
    echo [ERROR] No se encontró la carpeta de publicación con BlockMachine.exe
    echo.
    echo Prueba una de estas opciones:
    echo   instalar.bat --compilar
    echo   Coloca la carpeta publish junto a instalar.bat
    echo.
    pause
    exit /b 1
)

echo Origen:  !SOURCE_DIR!
echo Destino: %INSTALL_DIR%\
echo.

if not exist "%INSTALL_DIR%" (
    echo Creando carpeta %INSTALL_DIR% ...
    mkdir "%INSTALL_DIR%"
    if errorlevel 1 (
        echo [ERROR] No se pudo crear la carpeta de instalación.
        pause
        exit /b 1
    )
)

echo Copiando archivos ^(exe + librerías necesarias^)...
xcopy /Y /E /I /Q "!SOURCE_DIR!\*" "%INSTALL_DIR%\" >nul
if errorlevel 1 (
    echo [ERROR] No se pudieron copiar los archivos.
    pause
    exit /b 1
)

if not exist "%INSTALL_DIR%\BlockMachine.exe" (
    echo [ERROR] BlockMachine.exe no quedó en la carpeta de instalación.
    pause
    exit /b 1
)

echo.
echo [OK] Block Machine instalado en:
echo      %INSTALL_DIR%\BlockMachine.exe
echo.
echo Se abrirá el asistente inicial. Completa estos pasos:
echo   1. Crea la contraseña de administrador
echo   2. Marca "Iniciar automáticamente con Windows"
echo   3. Marca "Crear acceso directo en el escritorio"
echo.
echo Después de eso, la app arrancará sola al encender la PC.
echo.
pause

start "" "%INSTALL_DIR%\BlockMachine.exe"
exit /b 0

:FindPublishFolder
set "SOURCE_DIR="

if exist "%SCRIPT_DIR%publish\BlockMachine.exe" (
    set "SOURCE_DIR=%SCRIPT_DIR%publish"
    goto :eof
)

if exist "%SCRIPT_DIR%src\BlockMachine\bin\Release\net10.0-windows\win-x64\publish\BlockMachine.exe" (
    set "SOURCE_DIR=%SCRIPT_DIR%src\BlockMachine\bin\Release\net10.0-windows\win-x64\publish"
    goto :eof
)

REM Compatibilidad: solo un exe suelto (versiones antiguas)
if exist "%SCRIPT_DIR%BlockMachine.exe" (
    set "SOURCE_DIR=%SCRIPT_DIR%"
    goto :eof
)

goto :eof

:BuildProject
echo Compilando Block Machine ^(puede tardar uno o dos minutos^)...
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] No se encontró dotnet. Instala .NET SDK o copia la carpeta publish manualmente.
    exit /b 1
)

dotnet publish "%SCRIPT_DIR%src\BlockMachine\BlockMachine.csproj" -c Release
if errorlevel 1 (
    echo [ERROR] La compilación falló.
    exit /b 1
)

echo.
echo [OK] Compilación completada.
echo.
goto :eof
