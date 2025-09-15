@echo off
echo ========================================
echo BadStick Build Script
echo ========================================
echo.

REM Set the project directory
set PROJECT_DIR=%~dp0
set PROJECT_FILE="%PROJECT_DIR%Xbox 360 BadUpdate USB Tool.csproj"
set OUTPUT_DIR="%PROJECT_DIR%bin\Release"

echo Project Directory: %PROJECT_DIR%
echo Project File: %PROJECT_FILE%
echo Output Directory: %OUTPUT_DIR%
echo.

REM Check if MSBuild is available
echo Checking for MSBuild...

REM Try to find MSBuild in common locations
set MSBUILD_PATH=""

REM Check for Visual Studio 2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :build
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :build
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :build
)

REM Check for Visual Studio 2019
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :build
)

if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :build
)

if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :build
)

REM Check for .NET Framework MSBuild
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"
    goto :build
)

REM Check for Windows SDK MSBuild
if exist "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
    goto :build
)

REM Try using dotnet build as fallback
where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Using dotnet build as fallback...
    goto :dotnet_build
)

echo ERROR: MSBuild not found!
echo Please install Visual Studio or .NET SDK
echo.
echo Common locations checked:
echo - Visual Studio 2022 (Professional/Community/Enterprise)
echo - Visual Studio 2019 (Professional/Community/Enterprise)
echo - Visual Studio 2017 Build Tools
echo - Windows SDK
echo - .NET SDK (dotnet command)
echo.
pause
exit /b 1

:build
echo Found MSBuild: %MSBUILD_PATH%
echo.

REM Clean previous build
echo Cleaning previous build...
%MSBUILD_PATH% %PROJECT_FILE% /p:Configuration=Release /p:Platform=AnyCPU /t:Clean
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Clean failed!
    pause
    exit /b 1
)

echo.
echo Building BadStick (Release Configuration)...
%MSBUILD_PATH% %PROJECT_FILE% /p:Configuration=Release /p:Platform=AnyCPU /t:Rebuild /p:OutputType=WinExe
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

goto :success

:dotnet_build
echo Building with dotnet...
dotnet build %PROJECT_FILE% --configuration Release --output "%PROJECT_DIR%bin\Release"
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet build failed!
    pause
    exit /b 1
)

:success
echo.
echo ========================================
echo BUILD SUCCESSFUL!
echo ========================================
echo.
echo Output files location: %OUTPUT_DIR%
echo Main executable: BadStick.exe
echo.

REM List the output files
if exist %OUTPUT_DIR% (
    echo Generated files:
    dir /b %OUTPUT_DIR%\*.exe
    dir /b %OUTPUT_DIR%\*.dll 2>nul
    echo.
)

echo Build completed successfully!
echo You can find BadStick.exe in the bin\Release folder.
echo.
pause
