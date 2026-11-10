# 0.1.0 배포 구성 요소 라이선스 점검

점검일: 2026-11-10. 대상은 `b5837b6`의 실행 코드로 만든 후보 설치 파일과 `artifacts\builds\5845037ca179432e913d7d9a5f41f4ae\publish`다. 이 기록은 법적 결론이나 배포 승인이 아니다.

같은 날 트레이 기능을 포함한 새 후보 `artifacts\builds\d8ac552e034d4525a65d2bb5e714be02\publish`도 확인했다. .NET Core 및 Windows Desktop은 여전히 10.0.14이며 기존과 같은 라이선스·고지 파일이 게시 폴더에 있다. 사용자가 제공한 PNG에서 변환한 ICO는 앱 DLL의 리소스에 포함됐다. 실제 권리·배포 허용 여부를 이 파일의 출처만으로 별도 검증한 것은 아니다. 아래 .NET 조건의 미해결 판단은 새 후보에도 적용된다.

## 추가 조사와 배포 방침 — 2026-11-10

**방침:** CodexPet 자체 소스의 MIT 라이선스와 self-contained 설치 방식은 유지한다. 다만 배포 파일의 Microsoft 구성 요소 전체를 MIT로 간주하지 않고, Microsoft의 Windows 파일별 설명에 따라 .NET Library License와 Windows SDK License 및 해당 서드파티 고지를 별도로 다루는 방향으로 준비한다. MIT는 CodexPet 자체 코드의 라이선스이며 Microsoft 바이너리에 자동으로 적용되지 않는다. 라이선스 표시 차이에 관한 정확한 법적 효과와 조건 충족은 아직 확정하지 못했으므로, 이 방침은 공개 완료나 법적 적합성의 보증이 아니다.

### 확인한 근거

- 실제 후보의 `publish\App`에 `coreclr.dll`, `Microsoft.DiaSymReader.Native.amd64.dll`, `PresentationNative_cor3.dll`, `vcruntime140_cor3.dll`, `wpfgfx_cor3.dll`, `D3DCompiler_47_cor3.dll`이 있다. `publish\Hook`에도 `coreclr.dll`과 `Microsoft.DiaSymReader.Native.amd64.dll`이 있고 설치 보조 프로그램은 self-contained 단일 파일이다.
- Microsoft의 [Windows 라이선스 정보](https://github.com/dotnet/core/blob/main/license-information-windows.md)는 앞의 다섯 종류와 단일 파일에 포함된 .NET 런타임을 .NET Library License 대상으로 설명하고, `D3DCompiler_47_cor3.dll`에는 Windows SDK License를 명시한다. 문서는 참고 정보이며 그 자체가 계약은 아니다. [WPF 재배포 설명](https://github.com/dotnet/wpf/blob/main/Documentation/redistributables.md)은 해당 D3D 컴파일러가 WPF에 포함되면서 이름이 바뀐 경위를 설명한다. [Windows SDK 재배포 목록](https://learn.microsoft.com/en-us/legal/windows-sdk/redist)에는 원래 이름의 D3D 컴파일러가 Classic Windows 앱용으로 열거되어 있다. 열거 사실만으로 이 후보의 모든 배포 조건 충족이 확정되는 것은 아니다.
- [Microsoft의 .NET Library License](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm)는 조건을 지킨 앱 내 바이너리 배포를 허용하지만, 배포자·최종 사용자 약관과 Microsoft에 대한 배포자의 책임에 관한 조건도 둔다. [Windows SDK License](https://learn.microsoft.com/en-us/legal/windows-sdk/license) 역시 재배포 코드와 사용자 약관, 면책 등에 관한 조건을 둔다. 조건의 실제 적용과 이행은 배포 책임자가 검토해야 한다.
- 10.0.14 두 런타임 팩의 NuGet 표시는 각각 [MIT](https://www.nuget.org/packages/Microsoft.NETCore.App.Runtime.win-x64/10.0.14), [MIT](https://www.nuget.org/packages/Microsoft.WindowsDesktop.App.Runtime.win-x64/10.0.14)다. 그러나 Microsoft 저장소의 [메타데이터 차이 이슈 #108905](https://github.com/dotnet/runtime/issues/108905)는 8.0 팩의 동일한 문제를 다루며 담당자가 표시가 잘못됐다고 인정했다. 2026-10-21에는 10.0.13 self-contained 앱의 조건·고지·사용자 약관을 묻는 질문도 올라왔다. 2026-11-10 조회에서 이슈는 열려 있었고 그 질문에 대한 후속 답변은 확인되지 않았다. 8.0에 대한 인정이나 열린 이슈만으로 10.0.14의 계약상 효력을 단정하지 않는다.
- Windows Desktop 런타임 팩에는 별도 `THIRD-PARTY-NOTICES.TXT`가 없었으나 WPF 저장소에는 [별도의 고지](https://github.com/dotnet/wpf/blob/main/THIRD-PARTY-NOTICES.TXT)가 있다. 이 문서와 실제 10.0.14 바이너리의 대응 및 배포에 필요한 고지 범위는 확인해야 한다.

### 이 방침을 실제 공개에 적용하기 위해 남은 작업

1. `licenses/THIRD-PARTY-NOTICES.txt`에 확인한 Microsoft 자료와 구성 요소별 조건을 안내했다. 다음 배포물에는 해당 버전의 라이선스 전문과 실제로 필요한 서드파티 고지를 오프라인으로도 제공하도록 준비하고, MIT 파일 두 개만으로 Windows 바이너리의 모든 조건을 설명하지 않는다. 현재 후보는 이 변경을 포함하지 않는다.
2. .NET Library 및 Windows SDK 조건의 배포자 의무와 사용자·재배포자 약관 요건, WPF 고지 범위를 배포 책임자 또는 법률 전문가가 검토한다. 그 결과에 맞춰 설치·배포 과정의 안내 및 동의가 필요한지 결정한다. 약관 전문을 단순히 넣거나 동의란을 임의로 추가하는 것만으로 이행됐다고 단정하지 않는다. Microsoft의 미해결 이슈에 대한 답변도 확인한다.
3. 고지와 필요한 약관을 확정해 반영한 뒤 새 설치 파일을 빌드하고, 동일 파일에 대한 설치·제거·고지·해시 및 필요한 사용자 화면 시험을 다시 한다. 별도 사용자 공개 승인 전에는 게시하지 않는다.

이전 검토에서는 차이를 설명하지 못해 일반적인 문의만 남겼다. 파일별 Microsoft 자료와 기존 이슈를 확인해 위 방침을 정했지만, 그 자료만으로 배포자를 대신해 Microsoft에 대한 의무를 수락하거나 사용자 약관의 법적 효력을 보장하지는 않는다.

## 오프라인 사본과 약관 초안 준비 — 2026-11-10

- SDK 설치본에서 원본 바이트를 그대로 복사했다. `.NET SDK 10.0.403`의 `LICENSE.txt`와 Windows Desktop SDK 10.0.403의 서드파티 고지, Windows SDK 10.0.19041.0의 RTF 라이선스·서드파티 고지다. 파일명에 출처 버전을 명시했으며 Microsoft의 온라인 약관 및 배포되는 .NET 10.0.14 바이너리에 정확히 어떤 원문이 적용되는지는 아직 확인되지 않았다. 예를 들어 설치된 .NET SDK 라이선스와 온라인 .NET Library License의 표현은 동일하지 않다.
- 사본을 `licenses/`에 보관하고 `installer/build.ps1`이 게시 고지 폴더로 복사하도록 했다. `installer/test-isolated.ps1`은 이 네 파일의 존재와 설치 결과가 게시 파일과 같은지 확인한다. Windows Desktop 런타임 팩에 고지 파일이 없어서 SDK의 고지를 참고 사본으로 포함했으며, 이것이 런타임에 필요한 고지의 전부인지는 미검증이다.
- [설치 및 재배포 안내 문안](distribution-end-user-terms-draft.md)을 작성했다. 이어 사용자의 진행 요청에 따라 이를 바탕으로 라이선스 확인 화면을 추가했으며, 무인 설치에는 `/ACCEPTLICENSE`를 요구하도록 했다. Microsoft에 대한 배포자 의무, 실제 포함 바이너리의 정확한 약관·고지와 동의 절차의 적합성에 대한 검토는 별개로 남는다. [Microsoft의 미해결 이슈](https://github.com/dotnet/runtime/issues/108905)도 계속 확인한다.
- 이 준비를 포함한 검토용 설치 파일은 `artifacts\setup\CodexPet-Setup-0.1.0-win-x64.exe`, 빌드 폴더는 `artifacts\builds\0402b62054e84dfdbfb1c98075f5c5a5`, 크기는 124,418,052바이트, 서명은 `NotSigned`, SHA-256은 `8BA445A72845D296E1CAA6853DDB603A4BCF935FA1688DA0CE559FF783C91A54`다. 빌드 원본과 사본 해시가 같다. 초안이 약관으로 확정되거나 이행이 검증된 공개 파일은 아니다.
- 빌드 기준은 커밋 `b5837b6e52b5da0f7d92a29fc62991d3fb488192`에 앱·테스트·설치 스크립트·고지의 추적 파일 변경을 더한 상태이며 이 diff의 Git 식별값은 `592cb93ce1170a881c928aa3ed28a561dc7efd5c`다. diff와 미추적 아이콘 사본을 빌드 폴더에, 미추적 고지 네 파일의 실제 사본을 해당 빌드의 `publish\Licenses`에 보관했다. 원본 SDK 파일과 작업 트리의 고지 네 개가 바이트 단위로 일치했다.
- 위 게시 폴더로 별도 시험용 설치 파일을 만든 격리 검사가 통과했다. `artifacts\isolated 한글 e0afcf7329ba4b0bbdaf95d5d839a462`에서 설치·재설치·실패 복구·제거와 네 고지 파일의 존재 및 설치본과 게시본의 바이트 일치를 검사했다. 실제 검토용 설치 파일의 화면·사용자 약관 동의 또는 법률 적합성 시험은 아니다.

## 사용자 진행 요청 후 설치 화면 반영 — 2026-11-10

2026-11-10 사용자가 앞서 안내한 Microsoft 라이선스 확인 항목을 확인했다고 보고했다. 구체적인 확인 근거와 외부 법률 검토 여부는 제공되지 않았으므로 아래의 자료 차이나 미확정 범위가 해소됐다고 단정하지 않는다. 이 확인은 공개 승인이 아니다.

- 사용자의 진행 요청에 따라 `licenses/INSTALLER-LICENSE-INTRO.txt`를 바탕으로 CodexPet MIT와 SDK에서 수집한 Microsoft 약관·안내를 함께 보여 주는 설치 라이선스 화면을 구성했다. 설치 후 같은 내용은 `Licenses/CodexPet-Setup-Terms.txt`에 보관한다. 무인 설치에는 `/ACCEPTLICENSE`가 필요하다.
- 새 설치 파일은 `artifacts\setup\CodexPet-Setup-0.1.0-win-x64.exe`, 빌드 폴더는 `artifacts\builds\f6e3d553f1a04819968488e64b26d3a8`, 크기는 124,439,561바이트, 서명은 `NotSigned`, SHA-256은 `88335F47B3D0A353A4155BDFC587487415DC69B1B61318A1D0ABFB7446E61818`이다. 빌드 원본과 사본 해시는 일치한다. 추적된 앱·설치·테스트·고지 변경의 Git diff 식별값은 `da3a31d04514bad064c7b344d77bbecf268a8376`이며 diff와 미추적 아이콘 및 안내 문안을 빌드 폴더에 보관했다. 네 SDK 고지 사본은 `publish\Licenses`에 있다.
- 같은 게시 폴더로 생성한 별도 시험용 설치 파일에서 동의 인자 없는 무인 설치가 설정이나 설치 폴더를 변경하지 않고 중단되고, 인자가 있는 설치·재설치·실패 복구·제거와 고지 파일의 설치가 통과했다. 시험 폴더는 `artifacts\isolated 한글 19a69b57f0f1460787f5d28d9ad0a67c`다. 이는 일반 설치 화면에서 사용자가 약관을 읽고 동의·거부하는 실제 조작을 검증한 결과가 아니다.
- 사용자 요청은 안내 화면 구현을 진행하는 결정이다. Microsoft에 대한 배포자의 의무 이행이나 SDK 참고 사본이 .NET 10.0.14의 정확한 계약 원문인지까지 법적으로 확인한 것은 아니다. Microsoft 메타데이터 이슈가 열려 있고, 해당 조건의 적용·동의 문구의 적합성과 실제 화면 조작은 여전히 확인이 필요하다. 공개는 별도 승인 후에 한다.

## 확인 결과

| 구성 요소 | 확인한 출처·버전 | 현재 배포 준비 |
|---|---|---|
| CodexPet | 프로젝트 `LICENSE`, 버전 0.1.0 | 소스는 MIT이며 설치 고지 폴더에 `CodexPet-MIT.txt`가 있다. |
| XamlAnimatedGif | NuGet 2.3.2, 메타데이터의 원본 커밋 [`3873ad7`](https://github.com/XamlAnimatedGif/XamlAnimatedGif/tree/3873ad7a35a7d296bc7cf1899234b2c714ba55a8) | 패키지 메타데이터는 Apache-2.0이다. 배포 고지에 출처·작성자와 Apache-2.0 사본이 있다. 해당 원본 커밋의 전체 트리에서 `LICENSE.txt`는 확인했고 별도의 이름에 `NOTICE`가 들어간 파일은 찾지 못했다. 파일명 조사만으로 모든 소스 주석의 고지를 조사한 것은 아니다. |
| .NET Core 및 Windows Desktop 런타임 | 각각 `Microsoft.NETCore.App.Runtime.win-x64` 및 `Microsoft.WindowsDesktop.App.Runtime.win-x64` 10.0.14 | 복원된 각 NuGet 패키지의 `.nuspec`은 MIT를 표시하고 패키지에 들어 있는 라이선스도 MIT다. 후보 빌드는 각 파일과 제공된 .NET Core 서드파티 고지를 모았다. Windows Desktop 팩에서는 같은 이름의 서드파티 고지 파일이 발견되지 않았다. 적용 조건과 필요 고지는 아래 확인이 남아 있다. |
| Inno Setup | 로컬 컴파일러 7.1.1, Authenticode `Valid`, 서명자 `Pyrsys B.V.` | 배포 고지에 출처와 도구의 `license.txt` 사본이 있다. [공식 안내](https://jrsoftware.org/isinfo.php)는 상업적 사용자에게 라이선스 구매를 요청한다. 서명 확인만으로 도구 폴더 전체의 출처·무결성을 확정하지는 않는다. |

## .NET Windows 런타임: 공개 전 확인 필요

실제 복원된 두 Windows 런타임 팩의 `.nuspec`은 `MIT`를 표시하고, 설치 고지에 복사된 팩 내부 라이선스도 MIT다. 반면 Microsoft의 [.NET 라이선스 설명](https://github.com/dotnet/core/blob/main/license-information.md)은 Windows 제품 배포와 런타임 팩에 .NET Library License를 언급하고, self-contained 배포에도 런타임 일부가 포함된다고 설명한다. [.NET 에셋 라이선스 모델](https://github.com/dotnet/runtime/blob/main/docs/project/licensing-assets.md)도 Windows 런타임 팩을 제품 배포로 설명한다. 두 표시가 이 **구체적인 NuGet 팩과 앱 배포**에 어떻게 적용되는지는 이 자료만으로 확정하지 못했다.

[Microsoft .NET Library License](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm)에는 적용되는 경우 앱의 최종 사용자·배포자에 대한 조건과 배포자의 책임에 관한 조항이 있다. 단순히 라이선스 파일을 하나 더 넣는 것만으로 모든 조건이 충족되는지 판단할 수 없다. CodexPet 자체의 MIT 선택은 외부 런타임의 조건을 결정하지 않는다.

공개 전에 배포 책임자 또는 라이선스 전문가가 두 팩의 실제 취득 경로와 적용 조건을 확인한다. 필요하면 Microsoft에 `Microsoft.NETCore.App.Runtime.win-x64`와 `Microsoft.WindowsDesktop.App.Runtime.win-x64` **10.0.14**, NuGet 메타데이터의 MIT 표시, Windows self-contained 배포와 공식 문서의 .NET Library License 설명을 함께 제시해 적용 조건을 문의한다. 필요한 사용자 약관·고지가 확인되면 이를 반영하고 설치 파일을 다시 빌드·검증한다. 조건을 확인하기 전에는 이 항목을 완료하거나 후보 파일을 공개 승인된 파일로 표시하지 않는다.

## 검사 한계

후보 파일의 게시 폴더와 시험용 격리 설치에서 고지 파일의 존재를 확인했다. 실제 후보 설치 파일의 전체 구성에 대한 독립적인 법률 검토나 모든 구성 요소의 권리 검토를 완료했다는 뜻은 아니다. 빌드가 바뀌면 포함 버전, 팩의 메타데이터·고지와 설치된 결과를 다시 확인한다.
