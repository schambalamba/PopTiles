#define AppVersion "1.0.0"

[Setup]
AppId={{6E0B7A52-3C1D-4F6E-9A47-2D8F5C1B7E30}
AppName=PopTiles
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\PopTiles
DisableDirPage=yes
DisableProgramGroupPage=yes
; Installation nur fuer den aktuellen Benutzer: keine Admin-Rechte noetig, und das Programm darf neben der EXE speichern.
PrivilegesRequired=lowest
MinVersion=10.0.17763
OutputDir=output
OutputBaseFilename=PopTiles-Setup-{#AppVersion}
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\PopTiles.exe
UninstallDisplayName=PopTiles
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
; Windows-Sprache erkennen, ohne Rueckfrage. Unbekannte Sprachen fallen auf den ersten Eintrag zurueck.
LanguageDetectionMethod=uilanguage
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"; InfoBeforeFile: "info-before.txt"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"; InfoBeforeFile: "info-before.de.txt"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"; InfoBeforeFile: "info-before.fr.txt"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"; InfoBeforeFile: "info-before.es.txt"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"; InfoBeforeFile: "info-before.it.txt"

[CustomMessages]
english.OpenNow=Open PopTiles now
german.OpenNow=PopTiles jetzt öffnen
french.OpenNow=Ouvrir PopTiles maintenant
spanish.OpenNow=Abrir PopTiles ahora
italian.OpenNow=Apri PopTiles ora
english.DeleteGroups=Do you also want to delete your groups?
german.DeleteGroups=Sollen auch deine Gruppen gelöscht werden?
french.DeleteGroups=Veux-tu aussi supprimer tes groupes ?
spanish.DeleteGroups=¿Quieres eliminar también tus grupos?
italian.DeleteGroups=Vuoi eliminare anche i tuoi gruppi?

[Files]
Source: "..\dist\PopTiles.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\PopTiles.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\README.*.txt"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Run]
Filename: "{app}\PopTiles.exe"; Description: "{cm:OpenNow}"; Flags: postinstall nowait skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userprograms}\PopTiles"

[Code]
// Eine offene Verwaltung oder Kachel wuerde die EXE sonst blockieren.
procedure StopRunningInstances;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM PopTiles.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopRunningInstances;
  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  StopRunningInstances;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;

  DataDir := ExpandConstant('{app}\data');
  if DirExists(DataDir) then
    if SuppressibleMsgBox(CustomMessage('DeleteGroups'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES then
    begin
      DelTree(DataDir, True, True, True);
      RemoveDir(ExpandConstant('{app}'));
    end;
end;
