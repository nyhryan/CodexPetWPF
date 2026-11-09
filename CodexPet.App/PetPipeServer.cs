using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace CodexPet.App;

public sealed class PetPipeServer
{
    public async Task RunAsync(Func<HookEvent, Task> onEvent, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var pipe = new NamedPipeServerStream("CodexPet.Events", PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellationToken);

                try
                {
                    using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    readTimeout.CancelAfter(TimeSpan.FromSeconds(2));
                    byte[] bytes = new byte[4097];
                    int length = 0;
                    int newline = -1;
                    while (length < bytes.Length && newline < 0)
                    {
                        int count = await pipe.ReadAsync(bytes.AsMemory(length), readTimeout.Token);
                        if (count == 0) break;
                        newline = Array.IndexOf(bytes, (byte)'\n', length, count);
                        length += count;
                    }
                    if (newline < 0 || newline > 4096)
                    {
                        DiagnosticLog.Write("app", "Pipe message incomplete or exceeded size limit");
                        continue;
                    }

                    var received = JsonSerializer.Deserialize<HookEvent>(bytes.AsSpan(0, newline));
                    if (received is null || string.IsNullOrWhiteSpace(received.EventName))
                    {
                        DiagnosticLog.Write("app", "Event ignored: missing event name");
                        continue;
                    }
                    await onEvent(received);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    DiagnosticLog.Write("app", "Pipe read timed out");
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    DiagnosticLog.Write("app", $"Pipe event failed: {ex.GetType().Name}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}
