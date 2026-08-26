@echo off
echo ========================================
echo   Publishing MPH Songs (win-x64)
echo ========================================
echo.
dotnet publish ChurchProjector\ChurchProjector.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
if errorlevel 1 goto error
echo.
echo Published to .\publish\
echo Copy the publish folder to any Windows 10/11 machine.
pause
exit /b

:error
echo.
echo ========================================
echo        PUBLISH FAILED!
echo ========================================
pause
exit /b
