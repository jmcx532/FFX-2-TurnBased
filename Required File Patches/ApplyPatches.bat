@echo off
setlocal enabledelayedexpansion

set "BASE=%~dp0"
set "PLACE_DIR=%BASE%PlaceYourFilesHere"
set "PATCH_DIR=%BASE%Patches"
set "OUTPUT_DIR=%BASE%Output\efl\x2"

echo =============================================
echo     FFX-2 Turn-based File Patcher
echo =============================================
echo.

if not exist "%BASE%xdelta3.exe" (
    echo ERROR: xdelta3.exe not found!
    pause
    exit /b 1
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

call :process_folder "ffx_ps2"
call :process_folder "FFX-2_Data"

echo.
echo Patching finished!
pause
exit /b 0

:: =============================================
:process_folder
:: =============================================
set "GAME_FOLDER=%~1"
echo.
echo Processing %GAME_FOLDER%...


if not exist "%PATCH_DIR%\%GAME_FOLDER%" (
    echo   [SKIP] No patches found for %GAME_FOLDER%
    exit /b 0
)
if not exist "%PLACE_DIR%\%GAME_FOLDER%" (
    echo   [SKIP] PlaceYourFilesHere\%GAME_FOLDER% not found
    exit /b 0
)

pushd "%PATCH_DIR%\%GAME_FOLDER%"
for /r "." %%F in (*.xdelta) do (
    call :apply_patch "%%F" "%GAME_FOLDER%"
)
popd
exit /b 0

:: =============================================
:apply_patch
:: =============================================
setlocal enabledelayedexpansion
set "PATCH=%~1"
set "GAME_FOLDER=%~2"

:: Get relative path by removing the patch root
set "REL=!PATCH:%PATCH_DIR%\%GAME_FOLDER%\=!"
set "TARGET=!REL:.xdelta=!"

set "ORIG=%PLACE_DIR%\%GAME_FOLDER%\!TARGET!"
set "OUT=%OUTPUT_DIR%\%GAME_FOLDER%\!TARGET!"

echo   Patching: !TARGET!

if not exist "!ORIG!" (
    echo      ^> SKIP - Original missing
    echo      ^> Expected: !ORIG!
    endlocal
    exit /b 0
)

:: Create output folder
for %%D in ("!OUT!") do mkdir "%%~dpD" 2>nul

"%BASE%xdelta3.exe" -f -d -s "!ORIG!" "!PATCH!" "!OUT!"

if errorlevel 1 (
    echo      ^> FAILED
) else (
    echo      ^> Success
)
endlocal
exit /b 0