# 상태 및 보안 검증

프로젝트 루트에서 다음 명령으로 상태 전환, 격리된 훅 설정 등록·제거, 설정 저장·복구, 로그 회전과 위젯을 검증한다. 위젯 검사는 개인 `assets`나 기존 AppData 설정을 사용하지 않는 임시 폴더에서 실행하며 창이 잠시 열린다.

```powershell
dotnet build CodexPet.slnx -m:1 -p:UseSharedCompilation=false
dotnet run --project CodexPet.Tests --no-build
```

실제 훅 실행 파일과 파이프 서버 사이의 전달까지 검증하려면 위젯을 종료한 뒤 실행한다. 잠시 실제 파이프를 사용하고 오류 검사의 진단 메시지는 새 로그에 남는다. 테스트 중 다른 CodexPet 훅을 실행하지 않는다.

```powershell
dotnet run --project CodexPet.Tests --no-build -- --integration
```

통합 검사는 합성 데이터를 사용하며, 전송 필드와 민감 정보 제외, 이벤트 무시, 상태 전환, 입력·파이프 제한과 실패 후 복구를 확인한다. 실제 Codex의 이벤트 발생, GIF 재생 또는 설치 파일은 검증하지 않는다. 점검 범위, 로그 보관 정책과 미검증 항목은 [보안 점검 결과](../docs/features/security-review-results.md)를 참고한다.

설치 파일을 빌드한 뒤에는 아래 명령으로 별도의 시험용 설치 파일을 만들어 작업 공간 `artifacts` 아래에서 설치 실패·재설치·제거 및 사용자 데이터 보존을 검증한다. 선택적 데이터 삭제는 시험용 빌드에서 선택을 모의해 파일 처리를 확인한다. 시험 빌드는 실제 사용자 설정이나 시작 메뉴, 제거 레지스트리에 쓰지 않는다. 삭제 선택 화면과 실제 Codex 신뢰·전달은 별도로 확인해야 한다.

```powershell
.\installer\test-isolated.ps1 -InnoCompiler 'C:\path\to\ISCC.exe'
```
