@echo off
echo Building ChurchProjector...
dotnet build ChurchProjector\ChurchProjector.csproj -c Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b %ERRORLEVEL%
)
echo Running ChurchProjector...
dotnet run --project ChurchProjector\ChurchProjector.csproj -c Release --no-build
