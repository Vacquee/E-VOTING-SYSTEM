@echo off
echo ================================================
echo   Student Voting System
echo   Starting the application...
echo ================================================
echo.

cd /d "%~dp0"
"C:\Program Files\dotnet\dotnet.exe" run --project VotingSystem.csproj

pause
