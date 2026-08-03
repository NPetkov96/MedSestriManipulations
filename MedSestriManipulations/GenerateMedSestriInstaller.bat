@echo off
setlocal

dotnet publish "C:\Users\Nikolay Petkov\source\repos\MedSestri\MedSestriManipulations\MedSestriManipulations\MedSestriManipulations.csproj" ^
  -f net9.0-android ^
  -c Release ^
  -p:ApplicationTitle=MedSestri ^
  -o "C:\Users\Nikolay Petkov\OneDrive\Desktop\MedSestri NewVersion"

if errorlevel 1 (
    echo Publish failed.
    pause
    exit /b 1
)

echo Publish completed successfully.
pause