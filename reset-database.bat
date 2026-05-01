@echo off
echo ========================================
echo   RESET DATABASE (Drop Only)
echo ========================================
echo.
echo This will DROP the VotingSystemDB database.
echo All data will be DELETED!
echo.
echo To reseed, run the application after this.
echo.
pause

echo.
echo Dropping VotingSystemDB database...
echo.

REM Try mongosh first (newer versions)
mongosh --quiet --eval "use VotingSystemDB; db.dropDatabase(); print('✓ Database dropped successfully!');" 2>nul

if %errorlevel% neq 0 (
    REM Fallback to mongo command (older versions)
    echo Trying legacy mongo command...
    mongo --quiet --eval "use VotingSystemDB; db.dropDatabase(); print('✓ Database dropped successfully!');" 2>nul

    if %errorlevel% neq 0 (
        echo.
        echo ERROR: Could not connect to MongoDB!
        echo.
        echo Please ensure:
        echo   1. MongoDB is installed
        echo   2. MongoDB service is running: net start MongoDB
        echo   3. MongoDB shell (mongosh or mongo) is in your PATH
        echo.
        echo Manual reset steps:
        echo   1. Open Command Prompt
        echo   2. Run: mongosh   (or: mongo)
        echo   3. Run: use VotingSystemDB
        echo   4. Run: db.dropDatabase()
        echo   5. Run: exit
        echo.
        pause
        exit /b 1
    )
)

echo.
echo ========================================
echo   SUCCESS!
echo ========================================
echo.
echo Database has been dropped.
echo.
echo Next steps:
echo   1. Run the application: run.bat
echo   2. Data will be automatically seeded
echo.
pause
