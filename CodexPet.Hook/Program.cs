using System.Diagnostics;
using System.IO.Pipes;
using CodexPet;
using System.Text;
using System.Text.Json;

string? eventName = null;
string? sessionId = null;
DiagnosticLog.Write("hook", "Hook started");

try
{
    // Codex가 표준입력으로 전달한 이벤트
    string input = await Console.In.ReadToEndAsync();

    // 여러 줄 JSON이 들어와도 한 줄로 정규화
    using var document = JsonDocument.Parse(input);
    if (document.RootElement.ValueKind == JsonValueKind.Object)
    {
        if (document.RootElement.TryGetProperty("hook_event_name", out var eventProperty) && eventProperty.ValueKind == JsonValueKind.String)
            eventName = eventProperty.GetString();
        if (document.RootElement.TryGetProperty("session_id", out var sessionProperty) && sessionProperty.ValueKind == JsonValueKind.String)
            sessionId = sessionProperty.GetString();
    }
    DiagnosticLog.Write("hook", "Event read from stdin", eventName, sessionId);
    string json = JsonSerializer.Serialize(document.RootElement);

    using var pipe = new NamedPipeClientStream(
        serverName: ".",             // 현재 컴퓨터
        pipeName: "CodexPet.Events",
        direction: PipeDirection.Out,
        options: PipeOptions.Asynchronous
    );

    using var deadline =
        new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

    await pipe.ConnectAsync(
        timeout: 200,
        cancellationToken: deadline.Token
    );

    byte[] message = Encoding.UTF8.GetBytes(json + "\n");
    await pipe.WriteAsync(message.AsMemory(), deadline.Token);
    DiagnosticLog.Write("hook", "Event written to pipe", eventName, sessionId);
}
catch (Exception ex)
{
    // 알림 전용 프로그램:
    // 펫이 꺼져 있거나 전송에 실패해도 Codex 작업은 계속 진행.
    // 개발 중에는 예외 내용을 별도 로그에 기록.
    DiagnosticLog.Write("hook", $"Hook failed: {ex.GetType().Name}", eventName, sessionId);
}

return 0;
