@echo off
echo ========================================
echo   RESET AND RESEED DATABASE
echo ========================================
echo.
echo This will:
echo   1. Drop the VotingSystemDB database
echo   2. Restart the application
echo   3. Seed fresh data (100 students, 3 elections)
echo.
echo WARNING: All existing data will be DELETED!
echo.
pause

echo.
echo ========================================
echo   STEP 1: Dropping Database
echo ========================================
echo.

REM Drop the database using MongoDB shell
mongosh --quiet --eval "use VotingSystemDB; db.dropDatabase(); print('Database dropped successfully!');"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: MongoDB shell (mongosh) not found!
    echo.
    echo Please ensure MongoDB is installed and mongosh is in your PATH.
    echo Alternatively, you can manually drop the database:
    echo.
    echo   1. Open Command Prompt
    echo   2. Run: mongosh
    echo   3. Run: use VotingSystemDB
    echo   4. Run: db.dropDatabase()
    echo   5. Run: exit
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo   STEP 2: Starting Application
echo ========================================
echo.
echo The application will now start and automatically seed data...
echo.
echo Watch the console for seeding progress:
echo   [1/7] Creating 100 students...
echo   [2/7] Creating verification requests...
echo   [3/7] Creating elections...
echo   ... and so on
echo.
echo When seeding is complete, press Ctrl+C to stop the app
echo or close this window after testing.
echo.
echo ========================================
echo.

cd /d "%~dp0"
"C:\Program Files\dotnet\dotnet.exe" run --project VotingSystem.csproj

pause
