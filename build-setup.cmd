@echo off
rem Builds the installer dist\iStream-Setup-%VER%.exe.
rem Needs: build.cmd run first (build\iStream.ico) and the portable package dist\iStream-v%VER%-win-x64.zip.
setlocal
cd /d "%~dp0"
if not defined VER set VER=1.3.1
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set ZIP=dist\iStream-v%VER%-win-x64.zip
if not exist "%CSC%" (echo csc.exe not found & exit /b 1)
if not exist build\iStream.ico (echo Run build.cmd first & exit /b 1)
if not exist "%ZIP%" (echo Missing %ZIP% & exit /b 1)

set REFS=-r:System.dll -r:System.Core.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:Microsoft.CSharp.dll -r:System.IO.Compression.dll
set SRC=src\Setup.cs src\Controls.cs src\IconArt.cs
set OPTS=-nologo -platform:x64 -target:winexe -optimize+ -win32icon:build\iStream.ico -win32manifest:src\app.manifest

rem 1) uninstaller: same code, no payload
"%CSC%" %OPTS% %REFS% -out:build\uninstall.exe %SRC% || exit /b 1

rem 2) installer: payload + uninstaller embedded
"%CSC%" %OPTS% %REFS% -resource:"%ZIP%",payload.zip -resource:build\uninstall.exe,uninstall.exe ^
  -out:dist\iStream-Setup-%VER%.exe %SRC% || exit /b 1

echo OK: dist\iStream-Setup-%VER%.exe
