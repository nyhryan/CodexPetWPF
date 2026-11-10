# CodexPet 0.1.0 빌드 및 배포 준비

이 절차는 Windows 11 x64 개발 PC에서 미서명 시험용 설치 파일을 만든다. 최종 배포는 Spec 2·3·4의 검증과 공개 판단이 끝난 뒤 별도로 진행한다. 빌드한 PC와 결과물이 같다는 뜻은 설치 대상 PC에서 별도 검증을 마쳤다는 뜻이 아니다.

## 준비

- 현재 소스의 커밋과 변경 사항을 `git log -1 --oneline`, `git status --short`로 기록한다. 미커밋 변경이 있으면 커밋 ID만으로 빌드 소스를 식별할 수 없다.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)와 [Inno Setup](https://jrsoftware.org/isdl.php)을 준비한다. 2026-11-10 검증에는 SDK `10.0.403`, Inno Setup `7.1.1` x64를 사용했다. `dotnet --version`과 `ISCC.exe --version`을 기록하고, 다른 버전을 사용하면 결과를 다시 검증한다. NuGet 패키지와 런타임 팩의 첫 복원에는 네트워크가 필요할 수 있다.
- Inno Setup은 공식 설치 프로그램으로 설치하거나 `/PORTABLE=1`로 포터블 폴더에 설치할 수 있다. `ISCC.exe` 하나가 아닌 Inno Setup 폴더 전체가 필요하다. 예를 들어 포터블 설치 폴더가 `artifacts\Inno-Setup-7`이면 컴파일러는 `artifacts\Inno-Setup-7\ISCC.exe`다. `artifacts`는 Git에 포함되지 않는다. [공식 설치 및 검증 안내](https://jrsoftware.org/isdl-verify.php)
- 제품 버전은 저장소 루트 `version.txt`에서 한 번만 변경한다. .NET 프로젝트와 Inno Setup이 이 값을 사용한다. 현재 값은 `0.1.0`이다.

## 빌드

저장소 루트의 PowerShell에서 실제 컴파일러 경로를 지정한다. 스크립트 실행 정책에 따라 로컬 스크립트 실행이 허용되어 있어야 한다.

```powershell
$innoCompiler = (Resolve-Path '.\artifacts\Inno-Setup-7\ISCC.exe').Path
.\installer\build.ps1 -InnoCompiler $innoCompiler
```

이 PC에서는 Windows PowerShell의 실행 정책이 스크립트를 막아 현재 PowerShell 프로세스에만 적용되는 `Set-ExecutionPolicy -Scope Process RemoteSigned`가 필요할 수 있다. 조직에서 관리하는 정책은 따르며 정책을 변경할 수 없는 경우 담당자에게 문의한다.

스크립트는 실행마다 새로운 `artifacts\builds\<식별자>\publish`에 앱·훅·설치 보조 프로그램을 각각 Release, win-x64, self-contained로 게시하고 고지를 모은 뒤 같은 빌드 폴더에 설치 파일을 만든다. 성공한 경우에만 `artifacts\setup\CodexPet-Setup-0.1.0-win-x64.exe`로 복사한다. 출력에 표시된 빌드 폴더와 도구·런타임 버전을 소스 상태와 함께 기록한다. 앱과 훅은 각각 자체 런타임을 포함하며 설치 보조 프로그램은 단일 파일로 게시한다. 버전 확인 실패나 게시·고지 수집·컴파일 실패 시 스크립트는 실패한다.

실패하면 출력된 단계와 복원·경로·권한 오류를 해결하고 전체 명령을 다시 실행한다. `artifacts\setup`에 남은 기존 파일은 실패한 실행의 결과가 아니므로 성공 메시지 없이 배포하지 않는다. 빌드 폴더는 자동 삭제되지 않으며 디스크를 사용한다. 필요 없는 빌드 폴더와 이전 설치 파일은 내용을 확인한 뒤 수동으로 정리할 수 있다. `artifacts\Inno-Setup-7`이나 개인 자료를 함께 삭제하지 않도록 주의한다.

Inno Setup GUI에서 `.iss`만 열어 컴파일하면 해당 빌드의 게시 결과와 고지를 준비하지 못한다. 위 스크립트가 게시 폴더를 지정해 컴파일하는 절차를 사용한다.

## 검증과 해시

빌드 출력에 표시된 **같은 빌드의 게시 폴더**를 지정해 격리 시험을 실행한다. 실제 사용자 Codex 설정과 시작 메뉴를 사용하지 않는다.

```powershell
$publishRoot = 'C:\path\to\CodexPet\artifacts\builds\<식별자>\publish'
.\installer\test-isolated.ps1 -InnoCompiler $innoCompiler -PublishRoot $publishRoot
```

게시 폴더와 시험 설치 결과에서 앱·훅·설치 보조 프로그램, `Licenses`의 고지와 런타임이 있는지 확인하고, 개인 `assets` 및 이전 GIF가 포함되지 않았는지 검사한다. 사용자 설정과 이미지 원본을 검사 대상으로 삼지 않는다. 별도 SDK와 런타임이 없는 Windows 11 x64 계정·PC에서 최종 파일을 설치하고 빈 이미지 상태와 이미지 등록을 검증하는 작업은 아직 남아 있다.

공개할 파일이 정해진 **후** 아래 명령으로 SHA-256을 계산하고 파일명 및 출처와 함께 릴리스 안내에 기록한다. 새로 빌드하면 해시를 다시 계산한다. 해시는 파일 일치 여부를 확인하며 게시자의 신원을 증명하지 않는다.

```powershell
Get-FileHash -Algorithm SHA256 .\artifacts\setup\CodexPet-Setup-0.1.0-win-x64.exe
```

## 라이선스와 포함 구성

- CodexPet 소스는 [MIT 라이선스](../../LICENSE), 저작권자는 Yun Hyeok Nam, 2026이다. 외부 구성 요소에는 각각의 조건이 적용된다.
- 직접 참조하는 [XamlAnimatedGif 2.3.2](https://www.nuget.org/packages/XamlAnimatedGif/2.3.2)는 Apache-2.0이다. NuGet 메타데이터의 작성자는 Thomas Levesque이며 해당 패키지 자체에는 별도의 NOTICE 또는 라이선스 파일이 없었다. 배포물에는 구성 요소 출처와 Apache-2.0 사본을 포함한다. 원본 소스의 추가 고지는 공개 전에 확인한다.
- 실제 게시된 .NET Core 및 Windows Desktop 런타임의 버전을 빌드 출력과 `runtimeconfig.json`에서 확인한다. 빌드는 복원된 해당 런타임 팩의 라이선스와 제공되는 서드파티 고지를 수집한다. 이번 10.0.14 팩의 NuGet 메타데이터와 포함된 라이선스는 각각 MIT를 표시한다. 한편 [.NET의 배포 에셋 라이선스 설명](https://github.com/dotnet/runtime/blob/main/docs/project/licensing-assets.md)은 Windows 제품 배포에 .NET Library License를 명시한다. 실제 획득·배포 구성에 어떤 조건이 적용되는지 공개 전 확인한다. [Microsoft의 .NET Library 조건](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm)은 배포와 최종 사용자 조건에 관한 조항을 포함한다. self-contained 런타임은 PC의 런타임 업데이트로 자동 갱신되지 않는다.
- Inno Setup 라이선스 사본과 출처를 포함한다. 제작자는 상업적 사용자에게 라이선스 구매를 요청한다. [Inno Setup 안내](https://jrsoftware.org/isinfo.php)
- 설치 프로그램은 CodexPet MIT와 SDK에서 확보한 Microsoft 참고 약관 등을 표시한다. 무인 설치 시 사용자가 약관을 확인하고 동의한 경우에만 `/ACCEPTLICENSE`를 지정한다. SDK 사본과 실제 런타임에 적용될 정확한 조건의 관계 및 이 동의 방식의 적합성은 [라이선스 검토](distribution-license-review.md)를 따른다.
- 설치 후 고지는 `%LOCALAPPDATA%\CodexPet\Licenses`에 있다. 변경된 의존성이나 런타임과 고지의 적합성은 최종 배포 전에 다시 확인한다.

릴리스 안내의 작성 틀은 [0.1.0 릴리스 초안](distribution-release-draft.md)을 따른다.
