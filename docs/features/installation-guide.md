# CodexPet 설치·제거 안내

현재 구현은 Windows 11 x64 로컬 Codex용이며 설치 파일은 서명되지 않았습니다. Windows에서 게시자 미확인 또는 SmartScreen 경고가 나타날 수 있습니다. 공식 배포 위치와 SHA-256 비교값은 실제 릴리스 준비 시 제공해야 하며, 해시 일치는 게시자 신원을 증명하지 않습니다. 다른 Codex 홈, WSL, 원격·클라우드 작업은 자동 연동 대상이 아닙니다.

## 설치와 실행

1. 설치 파일을 실행합니다. 현재 사용자에게 `%LOCALAPPDATA%\CodexPet`으로 설치하며 경로는 바꿀 수 없습니다. 다른 Windows 계정에는 등록하지 않습니다.
2. 설치가 끝나면 시작 메뉴에서 CodexPet을 직접 실행합니다. 설치 후 자동 실행이나 Windows 로그인 시 자동 시작은 제공하지 않습니다.
3. Codex CLI의 `/hooks` 또는 사용 중인 Codex 앱의 훅 화면에서 새 CodexPet 훅을 검토하고 신뢰합니다. 새 훅이나 변경된 정의는 신뢰 전에는 실행되지 않을 수 있습니다. 실제 화면과 기능은 사용하는 버전에서 확인하세요. [OpenAI 공식 훅 문서](https://learn.chatgpt.com/docs/hooks)
4. 다른 로컬 프로젝트에서 Codex 작업을 시작하고 위젯 반응을 확인합니다. 위젯이 꺼져 있어도 Codex 작업은 계속됩니다. 여러 세션의 이벤트는 도착 순서대로 화면을 덮어쓸 수 있습니다.

재설치·업데이트 또는 제거 전에는 위젯을 직접 종료하세요. 훅 설정에 `config.toml`의 인라인 훅도 있으면 Codex가 동일 계층의 두 형식을 함께 읽고 경고할 수 있습니다. 이 설치 프로그램은 `config.toml`, 프로젝트 및 다른 프로그램의 훅을 수정하지 않습니다.

## 제거와 데이터

Windows의 설치된 앱 목록에서 CodexPet을 제거합니다. 제거 중 설정·이미지 사본, 새 로그, 이전 로그를 각각 삭제할지 물으며 기본적으로 보존합니다. 삭제 전 선택한 위치를 확인합니다. 원본 이미지 파일은 삭제하지 않습니다. 무인 제거 시 사용자 데이터는 모두 보존합니다.

이미지 삭제는 `images` 폴더에서 앱이 사본에 쓰는 32자리 식별자 형식의 파일명을 대상으로 합니다. 새 로그 삭제는 CodexPet의 `app.log`·`hook.log`와 각 순환 파일만 대상으로 합니다. 폴더의 다른 파일은 남을 수 있습니다.

- 사용자 문구·이미지 사본: `%LOCALAPPDATA%\CodexPet\settings.json`, `%LOCALAPPDATA%\CodexPet\images`
- 새 진단 로그: `%LOCALAPPDATA%\CodexPet\Logs\v2`
- 이전 로그: `%LOCALAPPDATA%\CodexPet\Logs\app.log`, `%LOCALAPPDATA%\CodexPet\Logs\hook.log`

이전 로그에는 세션 ID가 남아 있을 수 있습니다. 선택적으로 제거하거나 파일 탐색기에서 위 파일을 직접 삭제할 수 있습니다. [새 로그 보관 정책](security-review-results.md)도 참고하세요.

## 문제 해결

설치 또는 제거가 실패하면 안내된 `~\.codex\hooks.json`의 JSON 문법과 읽기·쓰기 권한을 확인하고, 파일을 사용하는 프로그램을 종료한 뒤 다시 시도합니다. 다른 훅을 포함한 기존 파일을 백업하고 복구하세요. 훅 등록 실패 후에는 이미 복사된 앱 파일이 남아 있을 수 있으며, 원인을 해결한 뒤 설치를 다시 실행해야 합니다. 등록이 성공하기 전에는 설치가 완료된 것으로 간주하지 않습니다.

설치 프로그램이 생성한 그룹의 정확한 사본은 `%LOCALAPPDATA%\CodexPet\Installer\hooks-owned.json`에 보관됩니다. CodexPet 그룹을 직접 변경하거나 중복시키면 설치·제거가 중단됩니다. 설정과 소유 기록을 백업한 후 `groups`에 기록된 해당 이벤트의 그룹과 실제 `hooks.json`을 비교해 CodexPet 항목을 원래대로 복구하고 다시 시도하세요. 다른 훅을 지우지 마세요. 소유 기록을 잃어버린 경우 같은 명령의 소유 여부를 자동 판별할 수 없습니다. 백업으로 복구하거나, 사용자가 직접 해당 CodexPet 항목만 확인해 제거한 뒤 설치를 다시 실행하고 정상 설치 후 제거할 수 있습니다.

훅 전달 문제는 Codex의 훅 검토·신뢰 상태, 설치된 훅 파일과 새 로그를 확인합니다. 새 로그는 오류만 기록하며 `%LOCALAPPDATA%\CodexPet\Logs\v2`에 있습니다.

## 기본 위치가 아닌 Codex 홈

설치 프로그램은 기본 사용자 홈의 `.codex\hooks.json`만 변경합니다. 다른 홈을 쓰는 사용자는 그 위치의 기존 훅을 보존하면서 직접 등록·제거해야 합니다. Windows PowerShell에서 아래 명령으로 실제 설치 경로를 사용한 **한 이벤트의 예시**를 만들 수 있습니다. 출력만 만들며 파일을 수정하지 않습니다.

```powershell
$hookPath = Join-Path $env:LOCALAPPDATA 'CodexPet\Hook\CodexPet.Hook.exe'
$hookCommand = "& '" + $hookPath.Replace("'", "''") + "'"
@{ hooks = @{ Stop = @(@{ hooks = @(@{ type = 'command'; command = $hookCommand; timeout = 2 }) }) } } } | ConvertTo-Json -Depth 10
```

대상 홈의 `hooks.json`에 기존 설정을 덮어쓰지 않도록 각 이벤트의 그룹을 병합하세요. 등록 대상은 `SessionStart`, `SessionEnd`, `Interrupt`, `Stop`, `UserPromptSubmit`, `PreModelRequest`, `PostModelRequest`, `PreToolUse`, `PostToolUse`, `PermissionRequest`입니다. Codex의 훅 검토·신뢰도 필요합니다. 이 수동 등록은 설치 프로그램의 소유 기록에 포함되지 않으므로 제거 시 사용자가 직접 정리해야 합니다.

## 개발자가 설치 파일 빌드하기

Windows x64, .NET 10 SDK와 Inno Setup 컴파일러가 필요합니다. NuGet 런타임 팩 복원에는 네트워크가 필요할 수 있습니다. 아래 명령은 앱·훅·설정 도우미를 self-contained로 게시한 뒤 설치 파일을 만듭니다. 사용자 Codex 설정을 변경하지 않습니다.

```powershell
.\installer\build.ps1 -InnoCompiler 'C:\path\to\ISCC.exe'
```

중간 파일은 실행마다 별도의 `artifacts\builds\<식별자>\publish`, 성공한 설치 파일은 `artifacts\setup\CodexPet-Setup-0.1.0-win-x64.exe`에 생성됩니다. 빌드에 실패하면 기존 설치 파일은 이번 결과가 아닙니다. 도구·고지·실패 복구와 격리 시험 방법은 [빌드 및 배포 준비](distribution-build-guide.md)를 참고하세요. 설치 파일은 서명되지 않았으며 최종 파일의 고지, 해시와 설치 시나리오는 공개 전에 다시 확인해야 합니다.
