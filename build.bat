@echo off
setlocal enabledelayedexpansion
title Building Stay on Top...

set "CSC="

for %%P in (
    "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (
    if exist %%P (
        set "CSC=%%~P"
    )
)

if "!CSC!"=="" (
    echo Could not find csc.exe ^(the .NET Framework C# compiler^).
    echo This ships with Windows 10/11 by default. If it's missing, install the
    echo ".NET Framework 4.x" feature from Windows Update / Optional Features.
    pause
    exit /b 1
)

echo Using compiler: !CSC!
echo Compiling all sources in src\ ...

"!CSC!" /nologo /target:winexe /out:StayOnTop.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll src\*.cs

if errorlevel 1 (
    echo.
    echo Build failed - see errors above.
    pause
    exit /b 1
)

echo.
echo Done! StayOnTop.exe has been created in this folder.
echo Double-click StayOnTop.exe to run it. It will sit quietly in your system tray.
pause
