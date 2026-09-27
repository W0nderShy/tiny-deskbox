@echo off
setlocal
cd /d "%~dp0"
echo START>build.log
dotnet --version>>build.log 2>&1
dotnet build DeskBox.sln -c Release>>build.log 2>&1
if %ERRORLEVEL% EQU 0 (echo BUILD_OK>>build.log) else (echo BUILD_FAIL>>build.log)
exit /b %ERRORLEVEL%
