REM Run from dev command line

@ECHO OFF

VERIFY ON

D:
cd \ProjectsPersonal\OnlyR
rd OnlyR\bin /q /s
rd Installer\Output /q /s

ECHO.
ECHO Publishing OnlyR
dotnet publish OnlyR\OnlyR.csproj -p:PublishProfile=FolderProfile -c:Release
IF %ERRORLEVEL% NEQ 0 goto ERROR

ECHO.
ECHO Creating installer
"D:\Program Files (x86)\Inno Setup 6\iscc" Installer\onlyrsetup.iss
IF %ERRORLEVEL% NEQ 0 goto ERROR

ECHO.
ECHO Creating portable zip
powershell Compress-Archive -Path OnlyR\bin\Release\net5.0-windows\publish\win-x86\* -DestinationPath Installer\Output\OnlyRPortable.zip 
IF %ERRORLEVEL% NEQ 0 goto ERROR

goto SUCCESS

:ERROR
ECHO.
ECHO ******************
ECHO An ERROR occurred!
ECHO ******************

:SUCCESS
