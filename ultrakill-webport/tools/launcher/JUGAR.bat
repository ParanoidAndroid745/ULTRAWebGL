@echo off
title ULTRAKILL WebGL - servidor (no cerrar)
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0servidor.ps1"
echo.
echo Servidor parado.
pause
