@echo off
setlocal

cd /d "%~dp0"

echo Restoring NuGet packages...
dotnet restore JellyfinMPCShim.sln
if errorlevel 1 (
    echo Restore failed.
    exit /b %errorlevel%
)

echo Publishing self-contained single-file Tray executable...
dotnet publish src\JellyfinMPCShim.Tray\JellyfinMPCShim.Tray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\tray-standalone
if errorlevel 1 (
    echo Publish failed.
    exit /b %errorlevel%
)

echo.
echo Build complete! Output located at:
echo   %~dp0publish\tray-standalone\JellyfinMPCShim.Tray.exe
endlocal
