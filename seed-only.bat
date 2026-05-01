@echo off
echo ========================================
echo   SEED DATA WITHOUT RESET
echo ========================================
echo.
echo This will run the application with seeding enabled.
echo.
echo NOTE: If data already exists, seeding will be skipped.
echo       Use reset-and-seed.bat to drop and reseed.
echo.
pause

echo.
echo Starting application...
echo Watch for seeding messages in the console.
echo.

cd /d "%~dp0"
"C:\Program Files\dotnet\dotnet.exe" run --project VotingSystem.csproj

pause
