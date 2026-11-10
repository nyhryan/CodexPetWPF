# 설치 및 재배포 안내 문안 초안

**구현에 반영한 문안의 검토 기록.** 사용자가 진행을 요청해 아래 안내를 바탕으로 설치 시 라이선스 화면을 구성했다. 실제 설치 화면의 문안은 `licenses/INSTALLER-LICENSE-INTRO.txt`이며 CodexPet MIT, SDK에서 수집한 Microsoft 약관과 고지를 함께 보여 준다. 이는 법률 검토나 동의의 효력을 확인했다는 뜻은 아니다. Microsoft의 조건을 이 방식으로 제시하는 것이 적절한지는 공개 전에 검토한다.

## 사용자에게 표시할 문안 후보

> CodexPet 소스 코드는 MIT 라이선스로 제공됩니다. 설치 파일에는 Microsoft .NET 런타임과 Windows Desktop 구성 요소 및 다른 외부 구성 요소가 포함됩니다. 이 구성 요소에는 각각 별도의 라이선스와 사용·재배포 조건이 적용될 수 있습니다.
>
> 설치 전에 제공되는 라이선스와 서드파티 고지를 확인해 주세요. 해당 구성 요소의 사용 및 재배포에는 해당 라이선스의 조건이 적용됩니다. CodexPet은 Microsoft가 제공하거나 보증하는 제품이 아닙니다.

## 검토할 사항

- [Microsoft .NET Library License](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm) 및 [Windows SDK License](https://learn.microsoft.com/en-us/legal/windows-sdk/license)의 배포자 의무와 외부 사용자·재배포자에 관한 약관 요건이 현재 제품 및 배포물에 어떻게 적용되는지.
- 어떤 문구와 동의 절차가 적절한지. 무인 설치, GitHub Releases를 통한 재배포, 사용자의 기존 MIT 소스 권리와의 관계 및 적용 지역의 강행법을 포함한다.
- 오프라인 사본으로 준비한 .NET SDK 10.0.403과 Windows SDK 10.0.19041의 약관이 실제 포함된 .NET 10.0.14 런타임 바이너리 및 WPF D3D 컴파일러에 적용되는 정확한 버전인지. 온라인에 게시된 조건과 문구가 다른 경우 어느 원문이 필요한지.
- Windows Desktop SDK 10.0.403의 서드파티 고지가 실제 .NET 10.0.14 Windows Desktop 런타임의 해당 구성 요소에 필요한 고지와 일치하는지.

배포자가 Microsoft에 부담할 수 있는 방어·면책 의무는 위 사용자 안내를 표시하는 것만으로 이행됐다고 확인되지 않았다. 대화형 설치에는 라이선스 확인 화면을 사용하고, 무인 설치에는 `/ACCEPTLICENSE`를 요구하도록 구현했다. 정확한 약관과 이 방식의 적절성은 별도로 검토한다.
