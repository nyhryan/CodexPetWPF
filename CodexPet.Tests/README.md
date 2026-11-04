# 상태 표시 검증

프로젝트 루트에서 다음 명령으로 상태 전환, 연속 이벤트와 캡션 배치를 창을 열지 않고 검증한다.

```powershell
dotnet run --project CodexPet.Tests
```

실제 훅 실행 파일과 파이프 서버 사이의 전달까지 검증하려면 위젯을 종료한 뒤 실행한다. 이 검사는 잠시 실제 파이프를 사용하고 진단 로그에 검증 이벤트를 남긴다.

```powershell
dotnet build CodexPet.slnx
dotnet run --project CodexPet.Tests -- --integration
```

통합 검사는 Codex의 이벤트 발생이나 GIF 화면 재생을 검증하지 않는다.
