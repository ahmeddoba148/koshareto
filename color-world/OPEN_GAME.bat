@echo off
cd /d "%~dp0"
where godot >nul 2>nul
if errorlevel 1 (
  echo Install Godot 4.4.1 Standard and import project.godot.
  echo Or add its executable to PATH with the name godot.exe.
  pause
  exit /b 1
)
godot --editor --path .
