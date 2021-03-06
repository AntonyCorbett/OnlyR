; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "OnlyR"
#define MyAppPublisher "Antony Corbett"
#define MyAppURL "https://soundboxsoftware.com"
#define MyAppExeName "OnlyR.exe"
#define MySource "d:\ProjectsPersonal\OnlyR\OnlyR"

#define MyAppVersion GetFileVersion(MySource + '\bin\Release\net5.0-windows\OnlyR.exe');

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{880BFB38-BF5D-4B07-8DA9-5951437B16FA}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\OnlyR
DefaultGroupName={#MyAppName}
OutputDir="..\Installer\Output"
OutputBaseFilename=OnlyRSetup
SetupIconFile=iconmic.ico
SourceDir={#MySource}
Compression=lzma
SolidCompression=yes
AppContact=antony@corbetts.org.uk
DisableWelcomePage=false
SetupLogging=True
RestartApplications=False
CloseApplications=False
AppMutex=OnlyRAudioRecording

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "bin\Release\net5.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Excludes: "*.dev.json,*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[ThirdParty]
UseRelativePaths=True

