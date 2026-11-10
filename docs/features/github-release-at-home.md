# 집에서 CodexPet 0.1.0 릴리스하기

이 문서는 집의 Windows 11 x64 PC에서 Git 태그와 GitHub Release를 직접 만드는 안내입니다. 대상 저장소는 [`nyhryan/CodexPetWPF`](https://github.com/nyhryan/CodexPetWPF), 버전은 `0.1.0`, 예정 태그는 `v0.1.0`입니다.

공개할 소스 커밋, 실제 확인한 설치 파일과 Release에 적을 SHA-256을 일치시켜야 합니다. 진행 전 [Spec 4 검증 기록](release-validation-and-distribution.md)과 [라이선스 검토](distribution-license-review.md)를 보고, 확인하지 않은 사항은 미검증으로 남긴 뒤 직접 공개 여부를 결정합니다.

## 1. 집에 소스 가져오기

학교에서 필요한 커밋을 GitHub에 푸시한 뒤 집에서 받습니다. 새로 받는다면 원하는 폴더에서 PowerShell을 열고 실행합니다.

```powershell
git clone https://github.com/nyhryan/CodexPetWPF.git
cd CodexPetWPF
git switch main
git status -sb
git log -3 --oneline
```

이미 저장소가 있다면 먼저 `git status -sb`로 미커밋 변경이 없는지 확인하고, `main`에서 `git pull --ff-only origin main`을 실행합니다. 변경이 있거나 fast-forward가 실패하면 내용을 확인하고 해결합니다. GitHub의 `main`에 학교에서 마지막으로 푸시한 커밋과 이 문서가 있는지도 확인합니다.

**`artifacts/`는 Git 관리 대상이 아닙니다.** Git 푸시나 클론만으로는 학교에서 만든 설치 파일, 빌드 결과와 포터블 Inno Setup이 집에 전달되지 않습니다. 커밋된 아이콘과 원본 이미지는 포함되지만, 개인용 `assets/`와 사용자 설정은 포함되지 않습니다.

## 2. 공개할 설치 파일 준비하기

두 방법 중 하나를 고르고, 이후 시험과 업로드에는 **같은 파일**을 사용합니다.

### 방법 A: 학교에서 만든 파일을 가져오기

학교의 `artifacts\setup\CodexPet-Setup-0.1.0-win-x64.exe`를 USB 등으로 복사합니다. 2026-11-10에 기록한 라이선스 화면 포함 후보의 SHA-256은 아래와 같습니다. 이후 다른 파일을 만들었다면 그 파일에 이 값을 적용하면 안 됩니다.

```text
88335F47B3D0A353A4155BDFC587487415DC69B1B61318A1D0ABFB7446E61818
```

집에서 복사한 **실제 파일 경로**를 지정해 해시를 비교합니다.

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'C:\실제_저장_경로\CodexPet-Setup-0.1.0-win-x64.exe'
```

일치하지 않으면 다른 파일로 취급하고 출처와 내용을 확인합니다. 사용자는 라이선스 화면·트레이 동작과 라이선스 확인 항목을 확인했다고 보고했지만 시험 환경과 파일 해시는 별도로 보고하지 않았습니다. 이전 Sandbox 성공 보고는 이 후보와 다른 파일에 대한 기록입니다.

### 방법 B: 집에서 다시 빌드하기

[빌드 안내](distribution-build-guide.md)에 따라 .NET 10 SDK, Inno Setup과 NuGet 복원 환경을 준비합니다. 이전 빌드에는 .NET SDK `10.0.403`, Inno Setup `7.1.1`, .NET 런타임 `10.0.14`가 사용됐습니다. 버전이 다르면 실제 포함 내용을 확인합니다.

아래 Inno Setup 경로는 예시이므로 집 PC의 실제 `ISCC.exe` 경로로 바꾸세요.

```powershell
$innoCompiler = 'C:\실제_Inno_Setup_폴더\ISCC.exe'
Test-Path -LiteralPath $innoCompiler
dotnet --version
& $innoCompiler --version
.\installer\build.ps1 -InnoCompiler $innoCompiler
```

`Test-Path`가 `True`인지 확인한 뒤 실행하고, 마지막에 **빌드 성공**이 나왔는지 확인합니다. 실패 후 `artifacts\setup`에 남은 이전 파일은 이번 빌드 결과가 아닙니다. 출력에 표시된 빌드 폴더와 `publish` 경로도 기록합니다.

다시 빌드한 파일은 학교의 파일과 다른 후보입니다. 이전 해시나 실제 화면 시험 결과를 그대로 옮기지 말고 새 해시를 계산하고 관련 검사를 다시 합니다. 예를 들어 아래의 `publish` 경로를 방금 성공한 빌드의 경로로 바꿔 격리 검사를 실행합니다.

```powershell
$publishRoot = 'C:\실제_프로젝트_경로\artifacts\builds\빌드_식별자\publish'
.\installer\test-isolated.ps1 -InnoCompiler $innoCompiler -PublishRoot $publishRoot
Get-FileHash -Algorithm SHA256 -LiteralPath '.\artifacts\setup\CodexPet-Setup-0.1.0-win-x64.exe'
```

실제 배포할 같은 파일로 설치, 라이선스 화면의 동의·거부, 트레이 숨기기·복원·설정·종료, 항상 위에 표시 동작을 확인하고 시험 환경과 해시를 기록합니다. 시험용 설치 파일의 자동 검사는 최종 파일의 화면 시험을 대신하지 않습니다. 사용자 설정과 다른 훅을 보호하고, 가능한 경우 격리된 환경과 합성 데이터를 사용합니다. 자세한 명령과 범위는 [빌드 안내](distribution-build-guide.md)와 [테스트 안내](../../CodexPet.Tests/README.md)를 따릅니다.

## 3. 해시와 공개 내용 확정하기

- 설치 파일명, 크기, 서명 상태, SHA-256과 소스 커밋을 기록합니다. 설치 파일은 **미서명**입니다. 서명은 `Get-AuthenticodeSignature -LiteralPath '실제_설치파일_경로'`로도 확인할 수 있습니다.
- [Release 노트 초안](distribution-release-draft.md)을 바탕으로 실제 파일의 해시, 지원 환경, 설치·삭제 방법, 미서명 경고, 알려진 제한과 문의처를 준비합니다. 오래된 해시나 미검증 항목을 성공으로 적지 않습니다.
- 기존 결정에 따라 Sandbox의 Codex 연동과 실제 기존 훅·다른 Windows 계정의 추가 시험은 건너뛰고 해당 범위를 미검증으로 적습니다. 이전 사용자 확인과 같은 Windows 계정의 파이프 위험 수용 범위는 [Spec 4](release-validation-and-distribution.md)와 [보안 기록](security-review-results.md)을 참고합니다.
- [라이선스 검토](distribution-license-review.md)의 사용자 확인 보고와 Microsoft 조건에 관해 확인된 범위를 읽고, 남은 불확실성과 필요한 고지를 확인한 뒤 배포 책임자로서 공개를 판단합니다.
- [GitHub Issues](https://github.com/nyhryan/CodexPetWPF/issues)의 비로그인 열람 가능 여부를 확인합니다. 아직 공개되지 않은 Release의 다운로드 확인은 공개 후에 수행합니다.

공개 전 소스나 저장소의 노트 파일을 바꾸면 이 단계에서 필요한 커밋·푸시를 마치고 공개할 커밋을 확정합니다. 설치 파일에 포함되는 내용을 바꾸거나 다시 빌드하면 해시와 필요한 검증도 다시 확인합니다.

## 4. `v0.1.0` 태그 만들고 푸시하기

태그는 기존 커밋을 가리키는 이름이므로 태그를 위한 별도 커밋은 필요하지 않습니다. 먼저 `HEAD`가 GitHub에 푸시된 **공개할 커밋**인지 확인합니다. 문서 등의 새 커밋이 추가됐다면 이전 커밋 `010a962`를 자동으로 지정하지 말고 실제 공개할 커밋을 선택합니다.

```powershell
git status -sb
git log -1 --oneline
git tag -l v0.1.0
git ls-remote --tags origin 'refs/tags/v0.1.0' 'refs/tags/v0.1.0^{}'
```

로컬과 GitHub에 `v0.1.0`이 없고 공개할 커밋이 푸시된 것을 확인한 다음 아래와 같이 생성합니다.

```powershell
git tag -a v0.1.0 -m 'CodexPet 0.1.0' HEAD
git show --no-patch --format=fuller v0.1.0
git push origin v0.1.0
git ls-remote --tags origin 'refs/tags/v0.1.0' 'refs/tags/v0.1.0^{}'
```

이미 태그가 있으면 새로 만들거나 강제 이동하지 말고 기존 태그의 대상 커밋을 확인합니다. 주석 태그는 원격에서 태그 객체와 `^{}` 참조가 함께 보일 수 있습니다. 보통 `git push`만으로는 태그가 자동 전송되지 않습니다. [Git 태그 안내](https://git-scm.com/book/en/v2/Git-Basics-Tagging)

## 5. GitHub에서 Release 만들기

1. 쓰기 권한이 있는 계정으로 로그인해 [CodexPetWPF Releases](https://github.com/nyhryan/CodexPetWPF/releases)를 열고 **Draft a new release**를 선택합니다.
2. **Choose a tag**에서 푸시한 `v0.1.0`을 고르고 대상 커밋을 확인합니다. 제목은 예를 들어 `CodexPet 0.1.0`으로 적습니다.
3. [Release 노트 초안](distribution-release-draft.md)을 바탕으로 실제 파일명·SHA-256·검증 범위·미서명 경고를 기입합니다. 공개 전 주의 문구와 미확정 값은 공개용 내용으로 정리합니다.
4. **Attach binaries**에 실제 검증한 `CodexPet-Setup-0.1.0-win-x64.exe`를 첨부합니다. GitHub가 자동 생성하는 `Source code` ZIP·tar.gz는 소스이며 설치 파일이 아닙니다.
5. 먼저 **Save draft**로 저장하고 태그·설명·첨부 파일·해시 표기를 확인합니다. 일반 정식 릴리스라면 의도한 경우가 아니면 pre-release를 선택하지 않습니다.
6. 직접 내용을 확인하고 공개하기로 결정했다면 **Publish release**를 누릅니다. 화면과 선택 사항은 [GitHub 공식 안내](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)도 참고합니다.

## 6. 공개 후 확인하기

로그아웃하거나 비공개 브라우저 창으로 Release와 Issues를 볼 수 있는지 확인하고, 첨부한 `.exe`를 다운로드합니다. 새로 받은 실제 파일 경로를 지정해 해시가 공개 전 기록 및 Release 표기와 일치하는지 확인합니다.

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'C:\실제_다운로드_경로\CodexPet-Setup-0.1.0-win-x64.exe'
```

일치하지 않으면 파일이 바뀌거나 잘못 첨부됐는지 조사하고 해결 전에는 이용자에게 안내하지 않습니다. 해시 일치는 파일 일치 여부를 확인하며 미서명 파일의 발행자를 증명하지 않습니다. 확인 시각, 공개 URL, 커밋·태그와 실제 파일 해시를 작업 기록에 남깁니다.
