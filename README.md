# CodexPet

CodexPet은 Windows 바탕화면에서 로컬 Codex의 작업 상태를 보여 주는 작은 위젯입니다. 작업을 기다리거나, 생각하거나, 도구를 사용하거나, 허락을 기다릴 때 상태에 맞는 이미지와 문구를 표시합니다.

이미지와 문구는 직접 고를 수 있습니다. 기본 이미지는 포함되어 있지 않으므로, 처음 실행할 때는 문구와 빈 이미지 영역이 보입니다.

## 주요 기능

- Codex 이벤트에 따라 대기, 의뢰 확인, 생각 중, 작업 중, 허락 대기 상태를 표시합니다.
- 상태마다 GIF·PNG·JPEG 이미지와 문구를 등록할 수 있습니다. 이미지와 문구를 여러 개 등록하면 각각 후보 중에서 선택합니다.
- 등록한 이미지의 사본과 설정을 사용자 계정에 보관합니다. GIF는 반복 재생됩니다.
- 위젯을 오른쪽 클릭해 설정을 열거나 종료할 수 있습니다.

## 사용 환경

- Windows 11 x64와 로컬 Codex가 대상입니다.
- 설치 파일에 필요한 .NET 런타임이 포함되어 있어 .NET SDK나 런타임을 별도로 설치할 필요가 없습니다.
- Windows ARM64, WSL, 원격·클라우드 작업은 0.1.0의 지원 대상이 아닙니다. 다른 Codex 홈은 자동 설정되지 않습니다.

## 설치와 시작

1. [GitHub Releases](https://github.com/nyhryan/CodexPetWPF/releases)에서 `CodexPet-Setup-0.1.0-win-x64.exe`를 받습니다.
2. 릴리스 안내에 적힌 SHA-256과 다운로드한 파일의 값을 비교합니다. 설치 파일은 서명되지 않았으므로 Windows에서 게시자 미확인 또는 SmartScreen 경고가 나타날 수 있습니다. 해시 일치는 파일 비교 수단이며 게시자 신원을 증명하지 않습니다.
3. 설치 파일을 실행합니다. 앱은 현재 사용자의 `%LOCALAPPDATA%\CodexPet`에 설치됩니다.
4. 시작 메뉴에서 CodexPet을 직접 실행합니다. 위젯을 오른쪽 클릭해 **설정**을 열고 원하는 상태에 이미지와 문구를 추가한 뒤 저장합니다.
5. Codex CLI의 `/hooks` 또는 사용 중인 Codex 앱의 훅 화면에서 새 CodexPet 훅을 검토하고 신뢰합니다. 다른 로컬 프로젝트에서 작업하며 위젯의 상태를 확인합니다.

상세한 설치·훅 신뢰·문제 해결과 기본 위치가 아닌 Codex 홈의 수동 설정은 [설치 안내](docs/features/installation-guide.md)를 참고하세요. 자동 시작이나 자동 업데이트는 제공하지 않습니다.

### SHA-256 확인

PowerShell에서 다운로드한 파일의 실제 경로를 지정합니다.

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'C:\경로\CodexPet-Setup-0.1.0-win-x64.exe'
```

출력의 `Hash`를 해당 릴리스 안내의 값과 비교합니다. 경로 대신 PowerShell에 `Get-FileHash -Algorithm SHA256 `을 입력한 다음 파일을 끌어다 놓아도 됩니다.

## 제거와 사용자 데이터

위젯을 종료한 뒤 Windows의 설치된 앱 목록에서 CodexPet을 제거합니다. 제거 화면에서 설정·이미지 사본, 새 로그와 이전 로그의 삭제 여부를 각각 선택할 수 있으며 기본 선택은 보존입니다. 원본 이미지 파일은 삭제하지 않습니다. 자세한 위치와 실패 시 복구 방법은 [설치·제거 안내](docs/features/installation-guide.md)를 참고하세요.

## 알아둘 제한

- 여러 Codex 세션의 상태를 구분해 집계하지 않으며, 도착한 이벤트 순서에 따라 표시가 바뀝니다. 위젯이 꺼져 있어도 Codex 작업은 계속됩니다.
- 같은 Windows 계정의 다른 프로그램을 파이프 상대방으로 인증하지 않습니다. 다른 프로그램이 가짜 이벤트를 보내 상태를 바꾸거나 통신을 선점해 이벤트 종류와 일부 시작 정보를 보고 위젯 수신을 방해할 수 있습니다. 훅은 프롬프트, 도구 입력·응답과 세션 ID를 파이프로 보내지 않습니다.
- 다른 Windows 계정·권한 수준과 실제 기존 훅이 함께 동작하는 경우는 최종 배포 환경에서 추가 검증하지 않았습니다. 오류 기록 위치와 정책은 [보안 점검 결과](docs/features/security-review-results.md)를 참고하세요.

## 개발과 문의

소스 빌드에는 Windows x64, .NET 10 SDK와 Inno Setup이 필요합니다. [빌드 안내](docs/features/distribution-build-guide.md)와 [검사 안내](CodexPet.Tests/README.md)에 절차가 있습니다.

문제나 제안은 [GitHub Issues](https://github.com/nyhryan/CodexPetWPF/issues)에 남겨 주세요. 로그를 첨부하기 전 개인 정보가 포함되어 있는지 확인해 주세요.

CodexPet 소스는 [MIT 라이선스](LICENSE)를 따릅니다. 외부 구성 요소의 고지는 설치 폴더의 `Licenses`에서 확인할 수 있습니다.
