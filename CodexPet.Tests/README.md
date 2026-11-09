# 상태 표시 검증

프로젝트 루트에서 다음 명령으로 상태 전환, 설정 저장·이행, 캡션 배치와 위젯을 검증한다. 위젯 검사는 개인 `assets`나 기존 AppData 설정을 사용하지 않는 임시 폴더에서 실행하며 창이 잠시 열린다.

```powershell
dotnet run --project CodexPet.Tests
```

실제 훅 실행 파일과 파이프 서버 사이의 전달까지 검증하려면 위젯을 종료한 뒤 실행한다. 이 검사는 잠시 실제 파이프를 사용하고 진단 로그에 검증 이벤트를 남긴다.

```powershell
dotnet build CodexPet.slnx
dotnet run --project CodexPet.Tests -- --integration
```

통합 검사는 Codex의 이벤트 발생이나 GIF 화면 재생을 검증하지 않는다.
