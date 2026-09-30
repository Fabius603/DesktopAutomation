@echo off
setlocal

echo DesktopAutomation
echo =================
echo [1] App starten (Release)
echo [2] Debug-Build erstellen und App starten
echo [3] Alle Tests und Repository-Pruefungen ausfuehren
echo [Q] Beenden
echo.

choice /C 123Q /N /M "Auswahl: "
if errorlevel 4 exit /b 0
if errorlevel 3 goto test
if errorlevel 2 goto debug
set "SCRIPT=Start-App.ps1"
goto run

:debug
set "SCRIPT=Start-Debug.ps1"
goto run

:test
set "SCRIPT=Test.ps1"

:run

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0%SCRIPT%"
set "RESULT=%ERRORLEVEL%"

echo.
if not "%RESULT%"=="0" echo Das Skript wurde mit Fehlercode %RESULT% beendet.
pause
exit /b %RESULT%
