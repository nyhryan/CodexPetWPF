using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace CodexPet.App;

public sealed class PetPipeServer
{
    public async Task RunAsync(
        Func<HookEvent, Task> onEvent,
        CancellationToken cancellationToken)
    {
        DiagnosticLog.Write("app", "Pipe server started");
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var pipe = new NamedPipeServerStream(
                    pipeName: "CodexPet.Events",
                    direction: PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous
                           | PipeOptions.CurrentUserOnly
                );

                // 클라이언트가 연결할 때까지 비동기 대기
                await pipe.WaitForConnectionAsync(cancellationToken);

                HookEvent? received;

                try
                {
                    using var reader = new StreamReader(
                        pipe,
                        Encoding.UTF8
                    );

                    // 연결만 하고 데이터를 보내지 않는 경우에 대비
                    using var readTimeout =
                        CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken
                        );

                    readTimeout.CancelAfter(TimeSpan.FromSeconds(2));

                    string? json =
                        await reader.ReadLineAsync(readTimeout.Token);

                    if (string.IsNullOrWhiteSpace(json))
                        continue;

                    received = JsonSerializer.Deserialize<HookEvent>(json);
                }
                catch (JsonException ex)
                {
                    DiagnosticLog.Write("app", $"Invalid JSON: {ex.GetType().Name}");
                    continue;
                }
                catch (IOException ex)
                {
                    DiagnosticLog.Write("app", $"Pipe read failed: {ex.GetType().Name}");
                    continue;
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    DiagnosticLog.Write("app", "Pipe read timed out");
                    continue;
                }

                if (received is null ||
                    string.IsNullOrWhiteSpace(received.EventName))
                {
                    DiagnosticLog.Write("app", "Event ignored: missing event name");
                    continue;
                }

                DiagnosticLog.Write("app", "Event received from pipe", received.EventName, received.SessionId);
                // 받은 이벤트를 WPF 쪽에 전달
                await onEvent(received);
                DiagnosticLog.Write("app", "Event handled by app", received.EventName, received.SessionId);

                // 반복문 끝에서 pipe가 해제되고 다음 연결을 기다림
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // 앱 종료에 따른 정상적인 수신 중단
            DiagnosticLog.Write("app", "Pipe server stopped");
        }
    }
}
