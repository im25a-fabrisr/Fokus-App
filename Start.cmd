@echo off
cd /d "%~dp0"
if exist "artifacts\windows\Fokus-App.exe" (
  start "" "artifacts\windows\Fokus-App.exe"
  exit /b
)
if exist ".tools\dotnet\dotnet.exe" (
  ".tools\dotnet\dotnet.exe" run --project src\Fokus.App
) else (
  dotnet run --project src\Fokus.App
)
if errorlevel 1 pause
