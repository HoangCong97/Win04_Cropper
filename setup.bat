@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"
title Cai dat va Thiet lap ScreenCropperPro

echo ======================================================
echo        ScreenCropperPro - Auto Setup va Build
echo ======================================================
echo.

:: 1. Kiem tra xem he thong da co .NET 9 SDK chua
set "HAS_NET9_SDK=0"
where dotnet >nul 2>&1
if %errorlevel% equ 0 (
    for /f "tokens=1" %%a in ('dotnet --list-sdks 2^>nul') do (
        echo %%a | findstr /b "9\." >nul && set "HAS_NET9_SDK=1"
    )
)

if "!HAS_NET9_SDK!"=="1" (
    echo [OK] He thong da co san .NET 9 SDK.
    goto :BUILD_STEP
)

echo [THONG TIN] He thong chua co .NET 9 SDK.
echo Dang chuan bi tai va cai dat .NET 9.0 SDK tu Microsoft...
echo.

:: 2. Uu tien dung winget
set "WINGET_OK=0"
where winget >nul 2>&1
if %errorlevel% equ 0 (
    echo [Cach 1] Dang thu cai dat qua cong cu winget...
    echo (Neu man hinh hien hop thoai UAC yeu cau quyen cai dat, vui long chon Yes)
    echo.
    winget install --id Microsoft.DotNet.SDK.9 -e --accept-package-agreements --accept-source-agreements
    if !errorlevel! equ 0 (
        set "WINGET_OK=1"
    ) else (
        echo.
        echo [THONG BAO] winget khong the hoan tat tu dong (ma ket thuc: !errorlevel!).
        echo Tu dong chuyen sang Cach 2: Tai truc tiep bo cai dat tu Microsoft...
        echo.
    )
)

:: 3. Fallback: Tai truc tiep bo cai tu Microsoft bang PowerShell
if "!WINGET_OK!"=="0" (
    echo [Cach 2] Dang tai file cai dat .NET 9.0 SDK chinh thuc tu Microsoft...
    echo Vui long doi trong giay lat (dung luong khoang 200MB)...
    set "INSTALLER=%TEMP%\dotnet-sdk-9.0.318-win-x64.exe"
    powershell -NoProfile -ExecutionPolicy Bypass -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Write-Host 'Dang ket noi may chu Microsoft...'; (New-Object System.Net.WebClient).DownloadFile('https://download.microsoft.com/download/489bd17e-0836-437f-a7b1-5342abd3c07f/098de5e2-523e-45e1-bcbd-9f2ac7a5b9a6/dotnet-sdk-9.0.318-win-x64.exe', $env:INSTALLER)"
    
    if exist "!INSTALLER!" (
        echo.
        echo [OK] Da tai xong bo cai dat!
        echo Dang mo trinh cai dat .NET 9 SDK (Vui long lam theo huong dan tren man hinh va chon Install)...
        start /wait "" "!INSTALLER!"
    ) else (
        echo.
        echo [LOI] Khong the tu dong tai bo cai dat.
        echo Vui long truy cap va tai thu cong tai:
        echo https://dotnet.microsoft.com/download/dotnet/9.0
        goto :PAUSE_EXIT
    )
)

:: 4. Cap nhat PATH cho phien hien tai
set "PATH=C:\Program Files\dotnet;C:\Program Files (x86)\dotnet;%PATH%"

:: 5. Kiem tra lai xem da nhan .NET 9 SDK chua
set "HAS_NET9_SDK=0"
where dotnet >nul 2>&1
if %errorlevel% equ 0 (
    for /f "tokens=1" %%a in ('dotnet --list-sdks 2^>nul') do (
        echo %%a | findstr /b "9\." >nul && set "HAS_NET9_SDK=1"
    )
)

if "!HAS_NET9_SDK!"=="0" (
    echo.
    echo ======================================================
    echo [CHU Y] Da cai dat xong nhung cua so CMD nay chua nhan bien moi truong.
    echo Vui long DONG cua so nay lai, sau do chay file 'run.bat' de su dung!
    echo ======================================================
    goto :PAUSE_EXIT
)

echo.
echo [OK] .NET 9 SDK da san sang!
echo.

:BUILD_STEP
echo ======================================================
echo   Dang khoi phuc thu vien va bien dich ScreenCropperPro...
echo ======================================================
echo.
dotnet build -c Release
if %errorlevel% neq 0 (
    echo.
    echo [LOI] Bien dich that bai! Vui long kiem tra chi tiet loi o tren.
    goto :PAUSE_EXIT
)

echo.
echo ======================================================
echo   [THANH CONG] Ung dung da duoc bien dich thanh cong!
echo ======================================================
echo.

set /p "LAUNCH=Ban co muon mo ScreenCropperPro ngay bay gio? (Y/N, mac dinh Y): "
if "!LAUNCH!"=="" set "LAUNCH=Y"
if /i "!LAUNCH!"=="Y" (
    start "" "%~dp0bin\Release\net9.0-windows\ScreenCropperPro.exe"
)

echo.
echo Hoan tat!
pause
exit /b 0

:PAUSE_EXIT
echo.
pause
exit /b 1
