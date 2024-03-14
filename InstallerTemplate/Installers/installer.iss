#define VerFile FileOpen("..\Builds\Version.txt")
#define ApplicationVersion FileRead(VerFile)
#expr FileClose(VerFile)
#undef VerFile

#define CompanyFile FileOpen("..\Builds\CompanyName.txt")
#define ApplicationCompanyName FileRead(CompanyFile)
#expr FileClose(CompanyFile)
#undef CompanyFile

#define ProductNameFile FileOpen("..\Builds\ProductName.txt")
#define ApplicationName FileRead(ProductNameFile)
#expr FileClose(ProductNameFile)
#undef ProductNameFile

#define IDFile FileOpen("..\Builds\ApplicationID.txt")
#define ApplicationID FileRead(IDFile)
#expr FileClose(IDFile)
#undef IDFile

[Setup]
AppId={#ApplicationID}
AppName={#ApplicationName}
AppVersion={#ApplicationVersion}
AppVerName={#ApplicationName} {#ApplicationVersion}
AppPublisher={#ApplicationCompanyName}
DefaultDirName={commonpf}\{#ApplicationCompanyName}\{#ApplicationName}
DisableProgramGroupPage=yes
OutputDir=.\
OutputBaseFilename={#ApplicationName}Setup-{#ApplicationVersion}
SetupIconFile=Resources\SetupIcon.ico
Compression=lzma
SolidCompression=yes
UninstallDisplayIcon={app}\InstallerApp.exe
WizardSmallImageFile=Resources\banner.bmp
WizardImageFile=Resources\welcome.bmp
DirExistsWarning=no
DisableWelcomePage=no
SignedUninstaller=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\Builds\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Run]
Filename: "{app}\InstallerApp.exe"; Description: "{cm:LaunchProgram, InstallerApp}"; Flags: nowait postinstall skipifsilent

[Messages]
SetupWindowTitle=Install - %1 - {#ApplicationVersion}