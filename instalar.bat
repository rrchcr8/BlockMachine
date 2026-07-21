@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

set "INSTALL_DIR=C:\BlockMachine"
set "SCRIPT_DIR=%~dp0"
set "SOURCE_EXE="

echo ========================================
echo   Block Machine - Instalador
echo ========================================
echo.

if /I "%~1"=="--compilar" set "DO_BUILD=1"
if /I "%~1"=="-compilar" set "DO_BUILD=1"
if /I "%~1"=="--build" set "DO_BUILD=1"

if "%DO_BUILD%"=="1" call :BuildProject

call :FindExecutable

if not defined SOURCE_EXE (
    echo [ERROR] No se encontró BlockMachine.exe
    echo.
    echo Prueba una de estas opciones:
    echo   instalar.bat --compilar
    echo   dotnet publish src/BlockMachine -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
    echo   Coloca BlockMachine.exe en la misma carpeta que instalar.bat
    echo.
    pause
    exit /b 1
)

echo Origen:  !SOURCE_EXE!
echo Destino: %INSTALL_DIR%\BlockMachine.exe
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

echo Copiando ejecutable...
copy /Y "!SOURCE_EXE!" "%INSTALL_DIR%\BlockMachine.exe" >nul
if errorlevel 1 (
    echo [ERROR] No se pudo copiar el archivo.
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

:FindExecutable
set "SOURCE_EXE="

if exist "%SCRIPT_DIR%BlockMachine.exe" (
    set "SOURCE_EXE=%SCRIPT_DIR%BlockMachine.exe"
    goto :eof
)

if exist "%SCRIPT_DIR%publish\BlockMachine.exe" (
    set "SOURCE_EXE=%SCRIPT_DIR%publish\BlockMachine.exe"
    goto :eof
)

if exist "%SCRIPT_DIR%src\BlockMachine\bin\Release\net10.0-windows\win-x64\publish\BlockMachine.exe" (
    set "SOURCE_EXE=%SCRIPT_DIR%src\BlockMachine\bin\Release\net10.0-windows\win-x64\publish\BlockMachine.exe"
    goto :eof
)

if exist "%SCRIPT_DIR%src\BlockMachine\bin\Debug\net10.0-windows\BlockMachine.exe" (
    echo [AVISO] Usando compilación Debug. Para la PC de producción usa --compilar.
    set "SOURCE_EXE=%SCRIPT_DIR%src\BlockMachine\bin\Debug\net10.0-windows\BlockMachine.exe"
)

goto :eof

:BuildProject
echo Compilando Block Machine ^(puede tardar uno o dos minutos^)...
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] No se encontró dotnet. Instala .NET SDK o copia BlockMachine.exe manualmente.
    exit /b 1
)

dotnet publish "%SCRIPT_DIR%src\BlockMachine\BlockMachine.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if errorlevel 1 (
    echo [ERROR] La compilación falló.
    exit /b 1
)

echo.
echo [OK] Compilación completada.
echo.
goto :eof
