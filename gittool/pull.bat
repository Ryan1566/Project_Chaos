@echo off
REM ============================================================================
REM  gittool / pull.bat  --  Windows entry point (double-click friendly)
REM
REM  All the real logic lives in pull.sh. This script only locates the bash.exe
REM  that ships with Git for Windows and hands off to it, so there is exactly one
REM  implementation to maintain instead of a .sh and a .ps1 drifting apart.
REM
REM  BASH DISCOVERY -- why it is done this way:
REM    Git is NOT always installed in %ProgramFiles%\Git (on the machine this was
REM    developed on it lives at E:\Git), so fixed paths are not enough. Worse,
REM    "where bash" is actively harmful: when Git's own usr\bin is absent from
REM    PATH -- which is the case for a plain double-click from Explorer -- the
REM    only match left is C:\Windows\System32\bash.exe, the WSL launcher. Not
REM    having WSL installed then pops up a "installing WSL" prompt out of nowhere.
REM
REM    So instead: enumerate every git.exe we can find, derive the Git root from
REM    it, and look for bash.exe inside that root. System32 is rejected outright.
REM
REM  This file is intentionally ASCII-only: cmd.exe parses batch files using the
REM  console code page, and keeping it ASCII avoids every CJK encoding pitfall.
REM
REM  --discard is destructive and therefore opt-in. It runs "git reset --hard
REM  HEAD", permanently discarding uncommitted changes to TRACKED files. It
REM  NEVER runs "git clean" -- untracked files are always left alone, and an
REM  uncommitted extra checkout here can hold hundreds of scripts and assets.
REM  It also refuses to discard unpushed commits; it only reports them.
REM
REM  Usage:  pull.bat [--dry-run] [--yes] [--discard] [--help] [--where]
REM ============================================================================

chcp 65001 >nul 2>&1
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "BASH_EXE="
set "GIT_EXE="

REM ---- 1. every git.exe on PATH, in PATH order -------------------------------
for /f "delims=" %%i in ('where git 2^>nul') do call :ConsiderGit "%%i"

REM ---- 2. common install locations as a fallback -----------------------------
call :ConsiderGit "%ProgramFiles%\Git\cmd\git.exe"
call :ConsiderGit "%ProgramFiles(x86)%\Git\cmd\git.exe"
call :ConsiderGit "%LOCALAPPDATA%\Programs\Git\cmd\git.exe"
call :ConsiderGit "%USERPROFILE%\scoop\apps\git\current\cmd\git.exe"
call :ConsiderGit "C:\Git\cmd\git.exe"

REM ---- diagnostic switch -----------------------------------------------------
if /i "%~1"=="--where" call :ShowWhere
if /i "%~1"=="--where" exit /b 0

if not defined BASH_EXE goto :NoBash

"%BASH_EXE%" "%SCRIPT_DIR%pull.sh" %*
set "RC=%ERRORLEVEL%"

REM When the user double-clicks the .bat, cmd runs it with /c and the window
REM would vanish before they can read anything. Pause only in that case.
echo %CMDCMDLINE% | find "/c" >nul 2>&1
if not errorlevel 1 pause

exit /b %RC%

REM ============================================================================
REM  :ConsiderGit <path-to-git.exe>
REM  From a git.exe path, work out where bash.exe would live inside the same
REM  Git installation, and remember it. Handles the usual layouts:
REM      <root>\cmd\git.exe          -> <root>\bin\bash.exe
REM      <root>\mingw64\bin\git.exe  -> <root>\bin\bash.exe
REM  Also tries one level higher, so deeper nesting still resolves.
REM ============================================================================
:ConsiderGit
if "%~1"=="" exit /b 0
if not exist "%~1" exit /b 0

REM Never accept anything from the WSL shim directory
echo(%~1| findstr /i /c:"\System32\" >nul && exit /b 0

if not defined GIT_EXE set "GIT_EXE=%~1"
if defined BASH_EXE exit /b 0

call :TryBash "%~dp1..\bin\bash.exe"          "%~1"
call :TryBash "%~dp1..\usr\bin\bash.exe"      "%~1"
call :TryBash "%~dp1..\..\bin\bash.exe"       "%~1"
call :TryBash "%~dp1..\..\usr\bin\bash.exe"   "%~1"
exit /b 0

REM ============================================================================
REM  :TryBash <candidate-bash-path> <git-exe-that-led-us-here>
REM  Accept the candidate only if it really exists and is not the WSL launcher.
REM ============================================================================
:TryBash
if defined BASH_EXE exit /b 0

REM %%~f normalises the ".." segments into a real path
for %%r in ("%~1") do set "BASH_CAND=%%~fr"
if not exist "!BASH_CAND!" exit /b 0

echo(!BASH_CAND!| findstr /i /c:"\System32\" >nul && exit /b 0

set "BASH_EXE=!BASH_CAND!"
set "GIT_EXE=%~2"
exit /b 0

REM ============================================================================
:ShowWhere
echo.
if defined GIT_EXE (echo   git  : %GIT_EXE%) else (echo   git  : NOT FOUND)
if defined BASH_EXE (echo   bash : %BASH_EXE%) else (echo   bash : NOT FOUND)
echo.
exit /b 0

REM ============================================================================
:NoBash
echo.
echo   [x] Could not find the bash.exe that ships with Git for Windows.
echo.
echo       gittool/pull.sh needs it. Looked next to every git.exe found on this
echo       machine:
echo.
if defined GIT_EXE (echo         %GIT_EXE%) else (echo         no git.exe found on PATH at all)
echo.
echo       Options:
echo         1. Install Git for Windows ^(https://git-scm.com^), or
echo         2. Use the Unity editor window instead, which calls git.exe directly
echo            and does not need bash at all:
echo              menu  Tool  -^>  GitToolWindow
echo            ^(see gittool/README.md for the exact menu label^)
echo.
echo       Run "pull.bat --where" to see what was found.
echo.
pause
exit /b 1
