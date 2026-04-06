REM Run from dev command line

@ECHO OFF

setlocal

VERIFY ON

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

pushd "%ROOT%"
IF %ERRORLEVEL% NEQ 0 goto ERROR

set "ISCC=%INNO_ISCC%"
if not defined ISCC (
    for /f "delims=" %%I in ('where iscc.exe 2^>nul') do (
        set "ISCC=%%I"
        goto ISCC_FOUND
    )
)

:ISCC_FOUND
if not defined ISCC set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\iscc.exe"
if not exist "%ISCC%" (
    ECHO.
    ECHO Inno Setup compiler not found.
    ECHO Set INNO_ISCC to your iscc.exe path or add iscc.exe to PATH.
    goto ERROR
)

rd OnlyR\bin /q /s
rd Installer\Output /q /s

ECHO.
ECHO Publishing OnlyR
dotnet publish OnlyR\OnlyR.csproj -p:PublishProfile=FolderProfile -c:Release
IF %ERRORLEVEL% NEQ 0 goto ERROR

ECHO.
ECHO Removing unwanted x64 DLLs
del OnlyR\bin\Release\net10.0-windows\publish\win-x86\libmp3lame.64.dll

ECHO.
ECHO Creating installer
"%ISCC%" Installer\onlyrsetup.iss
IF %ERRORLEVEL% NEQ 0 goto ERROR

ECHO.
ECHO Creating portable zip
powershell Compress-Archive -Path OnlyR\bin\Release\net10.0-windows\publish\win-x86\* -DestinationPath Installer\Output\OnlyRPortable.zip 
IF %ERRORLEVEL% NEQ 0 goto ERROR

goto SUCCESS

:ERROR
ECHO.
ECHO ******************
ECHO An ERROR occurred!
ECHO ******************

:SUCCESS

popd
endlocal

PAUSE