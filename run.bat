@echo off
setlocal enabledelayedexpansion
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
    echo.
    echo [LOI] Bien dich that bai!
    echo Co the he thong chua cai dat .NET 9 SDK hoac thieu thu vien.
    echo Ban co the chay file 'setup.bat' de tu dong cai dat moi thu.
    echo.
    set /p "RUN_SETUP=Ban co muon chay setup.bat ngay bay gio? (Y/N, mac dinh Y): "
    if "!RUN_SETUP!"=="" set "RUN_SETUP=Y"
    if /i "!RUN_SETUP!"=="Y" (
        call "%~dp0setup.bat"
        exit /b %ERRORLEVEL%
    )
    pause
    exit /b 1
)

if exist "%EXE_DEBUG%" (
    start "" "%EXE_DEBUG%"
) else (
    dotnet run
    if %ERRORLEVEL% NEQ 0 pause
)
