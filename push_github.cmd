@echo off
setlocal

cd /d "%~dp0"

where git >nul 2>nul
if errorlevel 1 (
    echo Git was not found. Please install Git for Windows first.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo This folder is not a Git repository.
    pause
    exit /b 1
)

echo Current branch and status:
git status --short --branch
echo.

git add .

git diff --cached --quiet
if not errorlevel 1 (
    echo No staged changes to commit.
    echo Pushing current branch...
    git push
    pause
    exit /b %errorlevel%
)

if "%~1"=="" (
    set "COMMIT_MESSAGE=Update project files"
) else (
    set "COMMIT_MESSAGE=%~1"
)

echo.
echo Creating commit: %COMMIT_MESSAGE%
git commit -m "%COMMIT_MESSAGE%"
if errorlevel 1 (
    echo Commit failed.
    pause
    exit /b 1
)

echo.
echo Pushing to GitHub...
git push
if errorlevel 1 (
    echo Push failed.
    pause
    exit /b 1
)

echo.
echo Done.
pause
