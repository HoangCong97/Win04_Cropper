@echo off
cd /d "%~dp0"
title Screen Cropper Pro

set "EXE_DEBUG=%~dp0bin\Debug\net9.0-windows\ScreenCropperPro.exe"
set "EXE_RELEASE=%~dp0bin\Release\net9.0-windows\ScreenCropperPro.exe"

if exist "%EXE_DEBUG%" (
    start "" "%EXE_DEBUG%"
    exit /b 0
)

if exist "%EXE_RELEASE%" (
    start "" "%EXE_RELEASE%"
    exit /b 0
)

echo Dang bien dich ScreenCropperPro...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo [LOI] Bien dich that bai!
    pause
    exit /b 1
)

if exist "%EXE_DEBUG%" (
    start "" "%EXE_DEBUG%"
) else (
    dotnet run
)
