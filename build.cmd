@echo off
rem Builds iStream.exe with the C# compiler that ships with Windows (.NET Framework 4).
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (echo csc.exe not found & exit /b 1)
if not exist build mkdir build

"%CSC%" -nologo -platform:x64 -out:build\mkicon.exe -r:System.Drawing.dll src\IconArt.cs src\MkIcon.cs || exit /b 1
build\mkicon.exe build\iStream.ico || exit /b 1

"%CSC%" -nologo -platform:x64 -target:winexe -optimize+ ^
  -win32icon:build\iStream.ico -win32manifest:src\app.manifest ^
  -r:System.dll -r:System.Core.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:Microsoft.CSharp.dll -r:System.Web.Extensions.dll ^
  -out:iStream.exe src\iStream.cs src\Controls.cs src\Strings.cs src\IconArt.cs src\Updater.cs || exit /b 1

echo OK: iStream.exe
