@echo off
setlocal EnableExtensions
title CrystalM2 - Release Build

rem Always build from the repository root, even when launched from elsewhere.
pushd "%~dp0"
if errorlevel 1 (
    echo ERROR: Could not open the repository folder.
    pause
    exit /b 1
)

rem Both projects target .NET 8. Project references are built automatically.
where dotnet >nul 2>&1
if errorlevel 1 goto :missing_sdk

dotnet --version >nul 2>&1
if errorlevel 1 goto :missing_sdk

if not exist "Client\Client.csproj" goto :missing_projects
if not exist "Server.MirForms\Server.csproj" goto :missing_projects

echo ============================================================
echo   CrystalM2 - Release build
echo ============================================================
echo.
echo [1/2] Building the Windows client...
echo.
set "CLIENT_STATUS=FAILED"
dotnet build "Client\Client.csproj" --configuration Release --nologo
if not errorlevel 1 set "CLIENT_STATUS=OK"

echo.
echo [2/2] Building the Windows server...
echo.
set "SERVER_STATUS=FAILED"
rem The server executable project also builds Server.Library and Shared.
dotnet build "Server.MirForms\Server.csproj" --configuration Release --nologo
if not errorlevel 1 set "SERVER_STATUS=OK"

echo.
echo ============================================================
echo   Build results
echo ============================================================
echo   Client: %CLIENT_STATUS%
echo   Server: %SERVER_STATUS%
echo.
echo   Client output: "%CD%\Build\Client\Release\"
echo   Server output: "%CD%\Build\Server\Release\"
echo.

if "%CLIENT_STATUS%"=="OK" if "%SERVER_STATUS%"=="OK" goto :success
echo ERROR: One or more builds failed. Review the errors above.
set "EXIT_CODE=1"
goto :finish

:success
echo SUCCESS: Both projects built in Release configuration.
set "EXIT_CODE=0"
goto :finish

:missing_sdk
echo ERROR: The .NET SDK could not be found.
echo Install the .NET 8 SDK or a compatible newer SDK, then retry.
set "EXIT_CODE=1"
goto :finish

:missing_projects
echo ERROR: Expected Client or Server project file was not found.
echo Run this batch file from the CrystalM2-New repository root.
set "EXIT_CODE=1"

:finish
echo.
popd
pause
exit /b %EXIT_CODE%
