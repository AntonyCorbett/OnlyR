REM Run from dev command line
D:
cd \ProjectsPersonal\OnlyR
rd OnlyR\bin /q /s
rd Installer\Output /q /s

REM build / publish
dotnet publish OnlyR\OnlyR.csproj -p:PublishProfile=FolderProfile -c:Release

REM Create installer
"C:\Program Files (x86)\Inno Setup 6\iscc" Installer\onlyrsetup.iss

REM create portable zip
powershell Compress-Archive -Path OnlyR\bin\Release\net5.0-windows\publish\* -DestinationPath Installer\Output\OnlyRPortable.zip 