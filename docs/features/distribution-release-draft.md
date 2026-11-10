# CodexPet 0.1.0 릴리스 노트

> **공개 전 초안:** 아래 해시는 오프라인 라이선스 참고 사본과 확인 화면을 포함한 빌드의 값입니다. 사용자가 화면 동작과 라이선스 확인 항목을 확인했다고 보고했지만 시험 파일 해시·환경과 확인 근거는 제공되지 않았습니다. 공개에는 별도 승인이 필요하며, 파일을 다시 빌드하면 해시를 갱신합니다.

CodexPet은 로컬 Codex의 작업 상태를 Windows 바탕화면 위젯으로 보여 줍니다. 첫 배포에서는 상태별 이미지와 문구를 직접 설정할 수 있습니다.

## 주요 기능

- Codex 작업의 대기, 의뢰 확인, 생각 중, 작업 중, 허락 대기 상태를 표시합니다.
- 상태마다 GIF·PNG·JPEG 이미지와 문구를 여러 개 등록할 수 있습니다. 기본 이미지는 포함되어 있지 않습니다.
- 현재 Windows 사용자 계정에 설치하고, Codex에서 훅을 검토하고 신뢰한 뒤 로컬 프로젝트에서 사용할 수 있습니다.
- 위젯의 설정 화면에서 이미지와 문구를 관리합니다. 이미지는 사본으로 보관하고 GIF는 반복 재생합니다.
- 우클릭 메뉴의 항상 위에 표시 설정으로 위젯을 일반 창보다 앞에 유지할 수 있습니다.
- 위젯을 트레이로 숨기고 트레이 아이콘에서 복원하거나 설정·종료할 수 있습니다.

## 사용 환경과 설치

- **지원 대상:** Windows 11 x64의 로컬 Codex. 설치 파일에 필요한 .NET 런타임이 포함되어 있습니다.
- **설치 파일:** `CodexPet-Setup-0.1.0-win-x64.exe`
- **설치 위치:** 현재 사용자의 `%LOCALAPPDATA%\CodexPet`
- 설치 후 시작 메뉴에서 위젯을 직접 실행하고, Codex CLI의 `/hooks` 또는 사용 중인 Codex 앱의 훅 화면에서 CodexPet 훅을 검토·신뢰해 주세요.
- 자세한 사용법과 제거 방법은 [설치 안내](https://github.com/nyhryan/CodexPetWPF/blob/main/docs/features/installation-guide.md)를 참고해 주세요.

## 설치 파일 확인

설치 파일은 **서명되지 않았습니다.** Windows에서 게시자 미확인 또는 SmartScreen 경고가 표시될 수 있습니다. [공식 GitHub Releases](https://github.com/nyhryan/CodexPetWPF/releases)에서 받은 파일의 SHA-256을 아래 값과 비교해 주세요. 해시는 파일의 일치 여부를 확인하며 게시자의 신원을 증명하지 않습니다.

**검토용 빌드의 SHA-256 — 다시 빌드하면 교체 필요:**

```text
88335F47B3D0A353A4155BDFC587487415DC69B1B61318A1D0ABFB7446E61818
```

PowerShell에서 다운로드한 파일의 실제 경로를 지정합니다.

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'C:\경로\CodexPet-Setup-0.1.0-win-x64.exe'
```

## 알려진 제한과 검증 범위

- Windows ARM64, WSL, 원격·클라우드 작업은 지원 대상이 아닙니다. 자동 시작과 자동 업데이트는 제공하지 않습니다.
- 여러 세션의 상태는 도착한 이벤트 순서대로 표시됩니다. 기본 위치가 아닌 Codex 홈은 자동 설정되지 않습니다.
- 같은 Windows 계정의 다른 프로그램은 가짜 이벤트로 위젯 상태를 바꾸거나 통신을 선점해 이벤트명과 일부 시작 정보를 보고 수신을 방해할 수 있습니다. 훅은 프롬프트, 도구 입력·응답과 세션 ID를 파이프로 보내지 않습니다.
- 이전 후보 파일의 Windows Sandbox 설치·실행과 이미지 재생이 성공했다는 사용자 보고가 있습니다. 해당 시험에서 `dotnet` 명령은 발견되지 않았으며 Codex 연동은 확인하지 않았습니다. 새로운 후보에서는 해당 시험을 다시 하지 않았으며 실제 기존 훅과 다른 Windows 계정·권한 수준의 동작도 추가 검증하지 않았습니다.
- 제거할 때 설정·이미지 사본과 로그는 기본적으로 보존하며, 삭제할 항목은 제거 화면에서 선택할 수 있습니다. 원본 이미지 파일은 삭제하지 않습니다.

## 문의와 라이선스

문제나 제안은 [GitHub Issues](https://github.com/nyhryan/CodexPetWPF/issues)에 남겨 주세요. 로그를 공유하기 전 개인 정보가 포함되어 있는지 확인해 주세요. 새 로그는 `%LOCALAPPDATA%\CodexPet\Logs\v2`에 있습니다.

CodexPet 소스는 [MIT 라이선스](https://github.com/nyhryan/CodexPetWPF/blob/main/LICENSE)를 따르며 외부 구성 요소 고지는 설치 폴더의 `Licenses`에 있습니다. Microsoft 구성 요소의 조건과 확인 범위는 [라이선스 검토](distribution-license-review.md)에 기록했습니다.
