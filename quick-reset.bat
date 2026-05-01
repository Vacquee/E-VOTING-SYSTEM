@echo off
REM Quick reset without confirmation - use with caution!

echo Dropping VotingSystemDB...
mongosh --quiet --eval "use VotingSystemDB; db.dropDatabase(); print('✓ Database dropped');" 2>nul

if %errorlevel% neq 0 (
    mongo --quiet --eval "use VotingSystemDB; db.dropDatabase(); print('✓ Database dropped');" 2>nul
)

echo Starting application...
echo.
cd /d "%~dp0"
"C:\Program Files\dotnet\dotnet.exe" run --project VotingSystem.csproj
