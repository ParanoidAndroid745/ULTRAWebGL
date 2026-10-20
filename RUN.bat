@echo off
title ULTRAKILL WebGL - servidor local
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serve-webgl.ps1" %*
if errorlevel 1 pause
