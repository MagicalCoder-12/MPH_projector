@echo off
:menu
cls
echo ========================================
echo     ChurchProjector Launcher
echo ========================================
echo.
echo [1] Build and Run (Release) - No debug output
echo [2] Build and Run (Debug)  - Shows debug output
echo [3] Run only (Release)     - Quick launch
echo [4] Run only (Debug)       - Quick launch with debug
echo [5] Clean and Build        - Fresh build
echo [Q] Quit
echo.
set /p choice="Enter your choice: "

if /i "%choice%"=="1" goto build_release
if /i "%choice%"=="2" goto build_debug
if /i "%choice%"=="3" goto run_release
if /i "%choice%"=="4" goto run_debug
if /i "%choice%"=="5" goto clean_build
if /i "%choice%"=="Q" goto end
echo Invalid choice!
timeout /t 2 >nul
goto menu

:build_release
echo.
echo Building Release...
dotnet build ChurchProjector\ChurchProjector.csproj -c Release
if errorlevel 1 goto error
echo Running Release...
dotnet run --project ChurchProjector\ChurchProjector.csproj -c Release --no-build
goto pause_end

:build_debug
echo.
echo Building Debug...
dotnet build ChurchProjector\ChurchProjector.csproj -c Debug
if errorlevel 1 goto error
echo Running Debug...
dotnet run --project ChurchProjector\ChurchProjector.csproj -c Debug --no-build
goto pause_end

:run_release
echo.
echo Running Release (no build)...
dotnet run --project ChurchProjector\ChurchProjector.csproj -c Release --no-build
goto pause_end

:run_debug
echo.
echo Running Debug (no build)...
dotnet run --project ChurchProjector\ChurchProjector.csproj -c Debug --no-build
goto pause_end

:clean_build
echo.
echo Cleaning solution...
dotnet clean ChurchProjector\ChurchProjector.csproj
echo Building Release...
dotnet build ChurchProjector\ChurchProjector.csproj -c Release
if errorlevel 1 goto error
echo Running Release...
dotnet run --project ChurchProjector\ChurchProjector.csproj -c Release --no-build
goto pause_end

:error
echo.
echo ========================================
echo        BUILD FAILED!
echo ========================================
echo Check the error messages above.
goto pause_end

:pause_end
echo.
pause
goto menu

:end
echo.
echo Goodbye!
pause