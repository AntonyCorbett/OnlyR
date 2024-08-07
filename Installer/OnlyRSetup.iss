#define MyAppName "OnlyR"
#define MyAppPublisher "Antony Corbett"
#define MyAppURL "https://github.com/AntonyCorbett/OnlyR"
#define MyAppExeName "OnlyR.exe"

#define MyAppVersion GetFileVersion('..\OnlyR\bin\Release\net8.0-windows\publish\win-x86\OnlyR.exe');

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
DefaultDirName={commonpf}\OnlyR
DefaultGroupName={#MyAppName}
OutputDir="..\Installer\Output"
OutputBaseFilename=OnlyRSetup
SetupIconFile=..\OnlyR\iconmic.ico
Compression=lzma
SolidCompression=yes
AppContact=antony@corbetts.org.uk
DisableWelcomePage=false
SetupLogging=True
RestartApplications=False
CloseApplications=False
AppMutex=OnlyRAudioRecording

PrivilegesRequired=admin

[InstallDelete]
Type: filesandordirs; Name: "{app}\*.*"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\OnlyR\bin\Release\net8.0-windows\publish\win-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[ThirdParty]
UseRelativePaths=True
