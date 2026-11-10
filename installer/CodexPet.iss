#define VersionFile FileOpen(AddBackslash(SourcePath) + "..\version.txt")
#if VersionFile == 0
  #error Cannot read version.txt
#endif
#define MyAppVersion Trim(FileRead(VersionFile))
#call FileClose(VersionFile)
#ifndef PublishRoot
  #error Run installer/build.ps1 to publish files and supply PublishRoot
#endif
[Setup]
AppId={{4D6B1288-E743-4C2C-9C60-30BFF4A06383}
AppName=CodexPet
AppVersion={#MyAppVersion}
LicenseFile={#PublishRoot}\Licenses\CodexPet-Setup-Terms.txt
#ifdef TestRoot
DefaultDirName={#TestRoot}\CodexPet
CreateUninstallRegKey=no
OutputBaseFilename=CodexPet-isolated-test
#else
DefaultDirName={localappdata}\CodexPet
OutputBaseFilename=CodexPet-Setup-{#MyAppVersion}-win-x64
#endif
DisableDirPage=yes
DisableProgramGroupPage=yes
DefaultGroupName=CodexPet
UsePreviousAppDir=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir=..\artifacts\setup
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
AllowCancelDuringInstall=no
UninstallDisplayIcon={app}\App\CodexPet.App.exe

[Files]
Source: "{#PublishRoot}\App\*"; DestDir: "{app}\App"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishRoot}\Hook\*"; DestDir: "{app}\Hook"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishRoot}\Install\CodexPet.Install.exe"; DestDir: "{app}\Installer"; Flags: ignoreversion
Source: "{#PublishRoot}\Install\CodexPet.Install.exe"; Flags: dontcopy
Source: "{#PublishRoot}\Licenses\*"; DestDir: "{app}\Licenses"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
#ifndef TestRoot
Name: "{autoprograms}\CodexPet"; Filename: "{app}\App\CodexPet.App.exe"; WorkingDir: "{app}\App"
#endif

[Code]
var
  RegistrationFailed: Boolean;

function HooksPath: String;
begin
#ifdef TestRoot
  Result := '{#TestRoot}\user\.codex\hooks.json';
#else
  if GetEnv('USERPROFILE') = '' then
    RaiseException('Windows 사용자 홈을 찾지 못했습니다.');
  Result := GetEnv('USERPROFILE') + '\.codex\hooks.json';
#endif
end;

function HookArguments(Action: String): String;
begin
  Result := Action + ' ' + AddQuotes(HooksPath) + ' ' +
    AddQuotes(ExpandConstant('{app}\Hook\CodexPet.Hook.exe')) + ' ' +
    AddQuotes(ExpandConstant('{app}\Installer\hooks-owned.json'));
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode, Index: Integer;
  ExpectedPath: String;
  AcceptedLicense: Boolean;
begin
  Result := '';
  if WizardSilent then begin
    AcceptedLicense := False;
    for Index := 1 to ParamCount do
      if CompareText(ParamStr(Index), '/ACCEPTLICENSE') = 0 then AcceptedLicense := True;
    if not AcceptedLicense then begin
      Result := '무인 설치에는 라이선스를 확인한 후 /ACCEPTLICENSE를 지정해야 합니다.';
      exit;
    end;
  end;
#ifdef TestRoot
  ExpectedPath := '{#TestRoot}\CodexPet';
#else
  ExpectedPath := ExpandConstant('{localappdata}\CodexPet');
#endif
  if CompareText(RemoveBackslash(ExpandConstant('{app}')), RemoveBackslash(ExpectedPath)) <> 0 then begin
    Result := '설치 경로를 변경할 수 없습니다. 고정 위치: ' + ExpectedPath;
    exit;
  end;
  if CheckForMutexes('Local\CodexPet.Widget') then begin
    Result := 'CodexPet이 실행 중입니다. 위젯을 종료한 뒤 설치를 다시 시도해 주세요.';
    exit;
  end;
  ExtractTemporaryFile('CodexPet.Install.exe');
  if not Exec(ExpandConstant('{tmp}\CodexPet.Install.exe'), HookArguments('check'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := '훅 설정 검사 프로그램을 실행하지 못했습니다.'
  else if ResultCode <> 0 then
    Result := '훅 설정을 안전하게 변경할 수 없습니다. ' + HooksPath +
      ' 및 ' + ExpandConstant('{app}\Installer\hooks-owned.json') + '을 확인하고 복구한 후 다시 시도해 주세요.';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then begin
    RegistrationFailed := not Exec(ExpandConstant('{tmp}\CodexPet.Install.exe'), HookArguments('register'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if not RegistrationFailed then RegistrationFailed := ResultCode <> 0;
    if RegistrationFailed then
      SuppressibleMsgBox('훅 등록에 실패하여 설치가 완료되지 않았습니다. ' + HooksPath +
        ' 및 ' + ExpandConstant('{app}\Installer\hooks-owned.json') + '을 확인하고 복구한 뒤 설치를 다시 시도해 주세요. 복사된 파일이 남아 있을 수 있습니다.', mbError, MB_OK, IDOK);
  end;
end;

function GetCustomSetupExitCode: Integer;
begin
  if RegistrationFailed then Result := 10 else Result := 0;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then begin
    if RegistrationFailed then begin
      WizardForm.FinishedHeadingLabel.Caption := 'CodexPet 설치가 완료되지 않았습니다';
      WizardForm.FinishedLabel.Caption := '훅 등록에 실패했습니다. ' + HooksPath + ' 및 ' + ExpandConstant('{app}\Installer\hooks-owned.json') +
        '을 확인하고 복구한 뒤 설치를 다시 시도해 주세요. 복사된 파일이 남아 있을 수 있습니다.';
    end else
      WizardForm.FinishedLabel.Caption := 'CodexPet 설치가 완료되었습니다. 시작 메뉴에서 위젯을 직접 실행하고 Codex의 /hooks에서 새 훅을 검토하고 신뢰해 주세요.' + #13#10 + #13#10 +
      '기본 위치가 아닌 Codex 홈은 자동 설정되지 않습니다. 설치 안내의 수동 설정 방법을 참고해 주세요.';
  end;
end;

function DeleteImageCopies(Directory: String): Boolean;
var
  Item: TFindRec;
  Name: String;
  Index: Integer;
  Owned: Boolean;
begin
  Result := True;
  if FindFirst(Directory + '\*', Item) then begin
    try
      repeat
        Name := Item.Name;
        Owned := (Length(Name) >= 32) and
          ((Length(Name) = 32) or (Copy(Name, 33, 1) = '.')) and
          (Item.Attributes and FILE_ATTRIBUTE_DIRECTORY = 0);
        if Owned then
          for Index := 1 to 32 do
            if Pos(Lowercase(Copy(Name, Index, 1)), '0123456789abcdef') = 0 then Owned := False;
        if Owned then
          if not DeleteFile(Directory + '\' + Name) then Result := False;
      until not FindNext(Item);
    finally
      FindClose(Item);
    end;
  end;
  RemoveDir(Directory);
end;

function RemoveNewLogs(Directory: String): Boolean;
var
  Index: Integer;
  Name: String;
begin
  Result := True;
  for Index := 0 to 5 do begin
    if Index mod 2 = 0 then Name := 'app.log' else Name := 'hook.log';
    if Index >= 2 then Name := Name + '.' + IntToStr(Index div 2);
    if FileExists(Directory + '\' + Name) and not DeleteFile(Directory + '\' + Name) then Result := False;
  end;
  RemoveDir(Directory);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DeleteSettings, DeleteNewLogs, DeleteOldLogs: Boolean;
  Answer, ResultCode: Integer;
  Summary, DataRoot: String;
begin
  if CurUninstallStep <> usUninstall then exit;
  if CheckForMutexes('Local\CodexPet.Widget') then begin
    SuppressibleMsgBox('CodexPet이 실행 중입니다. 위젯을 종료한 뒤 제거를 다시 시도해 주세요.', mbError, MB_OK, IDOK);
    Abort;
  end;
  DataRoot := ExpandConstant('{app}');
  DeleteSettings := False;
  DeleteNewLogs := False;
  DeleteOldLogs := False;
  if not UninstallSilent then begin
    Answer := MsgBox('설정 및 저장된 이미지 사본을 삭제할까요?' + #13#10 + DataRoot + '\settings.json' + #13#10 + DataRoot + '\images' + #13#10 + '아니요를 선택하면 보존합니다.', mbConfirmation, MB_YESNOCANCEL or MB_DEFBUTTON2);
    if Answer = IDCANCEL then Abort;
    DeleteSettings := Answer = IDYES;
    Answer := MsgBox('새 진단 로그를 삭제할까요?' + #13#10 + DataRoot + '\Logs\v2' + #13#10 + '아니요를 선택하면 보존합니다.', mbConfirmation, MB_YESNOCANCEL or MB_DEFBUTTON2);
    if Answer = IDCANCEL then Abort;
    DeleteNewLogs := Answer = IDYES;
    Answer := MsgBox('이전 진단 로그를 삭제할까요? 세션 ID가 포함되어 있을 수 있습니다.' + #13#10 + DataRoot + '\Logs\app.log' + #13#10 + DataRoot + '\Logs\hook.log' + #13#10 + '아니요를 선택하면 보존합니다.', mbConfirmation, MB_YESNOCANCEL or MB_DEFBUTTON2);
    if Answer = IDCANCEL then Abort;
    DeleteOldLogs := Answer = IDYES;
    Summary := '선택한 삭제 대상:' + #13#10;
    if DeleteSettings then Summary := Summary + '- 설정 및 이미지 사본: ' + DataRoot + '\settings.json, images' + #13#10;
    if DeleteNewLogs then Summary := Summary + '- 새 로그: ' + DataRoot + '\Logs\v2' + #13#10;
    if DeleteOldLogs then Summary := Summary + '- 이전 로그: ' + DataRoot + '\Logs\app.log, hook.log' + #13#10;
    if not (DeleteSettings or DeleteNewLogs or DeleteOldLogs) then Summary := Summary + '- 사용자 데이터 보존' + #13#10;
    if MsgBox(Summary + #13#10 + '훅을 정리하고 CodexPet을 제거할까요?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) <> IDYES then Abort;
  end;
#ifdef TestRoot
#ifdef TestDeleteData
  DeleteSettings := True;
  DeleteNewLogs := True;
  DeleteOldLogs := True;
#endif
#endif
  if not Exec(ExpandConstant('{app}\Installer\CodexPet.Install.exe'), HookArguments('remove'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then begin
    SuppressibleMsgBox('훅 정리 프로그램을 실행할 수 없어 제거를 중단했습니다. 설치 파일은 보존됩니다.', mbError, MB_OK, IDOK);
    Abort;
  end;
  if ResultCode <> 0 then begin
    SuppressibleMsgBox('훅을 안전하게 정리할 수 없어 제거를 중단했습니다. ' + HooksPath + ' 및 ' + ExpandConstant('{app}\Installer\hooks-owned.json') + '을 확인하고 복구한 뒤 다시 시도해 주세요. 설치 파일은 보존됩니다.', mbError, MB_OK, IDOK);
    Abort;
  end;
  if DeleteSettings then begin
    if FileExists(DataRoot + '\settings.json') and not DeleteFile(DataRoot + '\settings.json') then
      SuppressibleMsgBox('설정을 삭제하지 못했습니다: ' + DataRoot + '\settings.json', mbError, MB_OK, IDOK);
    if DirExists(DataRoot + '\images') and not DeleteImageCopies(DataRoot + '\images') then
      SuppressibleMsgBox('일부 이미지 사본을 삭제하지 못했습니다: ' + DataRoot + '\images', mbError, MB_OK, IDOK);
  end;
  if DeleteNewLogs then
    if DirExists(DataRoot + '\Logs\v2') then
      if not RemoveNewLogs(DataRoot + '\Logs\v2') then
        SuppressibleMsgBox('일부 새 로그를 삭제하지 못했습니다: ' + DataRoot + '\Logs\v2', mbError, MB_OK, IDOK);
  if DeleteOldLogs then begin
    if FileExists(DataRoot + '\Logs\app.log') and not DeleteFile(DataRoot + '\Logs\app.log') then
      SuppressibleMsgBox('이전 로그를 삭제하지 못했습니다: ' + DataRoot + '\Logs\app.log', mbError, MB_OK, IDOK);
    if FileExists(DataRoot + '\Logs\hook.log') and not DeleteFile(DataRoot + '\Logs\hook.log') then
      SuppressibleMsgBox('이전 로그를 삭제하지 못했습니다: ' + DataRoot + '\Logs\hook.log', mbError, MB_OK, IDOK);
  end;
end;
