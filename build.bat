@echo off
echo ================================================
echo   Building Student Voting System
echo ================================================
echo.

cd /d "%~dp0"
"C:\Program Files\dotnet\dotnet.exe" build VotingSystem.csproj

echo.
echo Build complete!
pause
