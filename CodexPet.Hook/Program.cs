using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using CodexPet;

try
{
    const int maxInputBytes = 1024 * 1024;
    using var input = Console.OpenStandardInput();
    using var buffer = new MemoryStream();
    byte[] chunk = new byte[8192];
    using var inputTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
    while (true)
    {
        int count = await input.ReadAsync(chunk, inputTimeout.Token).AsTask().WaitAsync(inputTimeout.Token);
        if (count == 0) break;
        if (buffer.Length + count > maxInputBytes)
        {
            DiagnosticLog.Write("hook", "Input exceeded size limit");
            return 0;
        }
        buffer.Write(chunk, 0, count);
    }

    using var document = JsonDocument.Parse(buffer.ToArray());
    var root = document.RootElement;
    if (root.ValueKind != JsonValueKind.Object ||
        !root.TryGetProperty("hook_event_name", out var property) || property.ValueKind != JsonValueKind.String ||
        string.IsNullOrWhiteSpace(property.GetString()))
    {
        DiagnosticLog.Write("hook", "Input missing event name");
        return 0;
    }

    string? eventName = property.GetString();
    if (eventName is not ("SessionStart" or "SessionEnd" or "Interrupt" or "Stop" or "UserPromptSubmit" or
        "PreModelRequest" or "PostModelRequest" or "PreToolUse" or "PostToolUse" or "PermissionRequest"))
        return 0;

    string json;
    if (eventName == "SessionStart")
    {
        string? source = root.TryGetProperty("source", out var sourceProperty) && sourceProperty.ValueKind == JsonValueKind.String
            ? sourceProperty.GetString() : null;
        // Malformed or unknown sources keep the non-compact behavior without forwarding arbitrary text.
        json = source is "startup" or "resume" or "clear" or "compact" or "fork"
            ? JsonSerializer.Serialize(new { hook_event_name = eventName, source })
            : JsonSerializer.Serialize(new { hook_event_name = eventName });
    }
    else
        json = JsonSerializer.Serialize(new { hook_event_name = eventName });

    using var pipe = new NamedPipeClientStream(".", "CodexPet.Events", PipeDirection.Out, PipeOptions.Asynchronous);
    using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
    await pipe.ConnectAsync(200, deadline.Token);
    await pipe.WriteAsync(Encoding.UTF8.GetBytes(json + "\n"), deadline.Token);
}
catch (Exception ex)
{
    // Never include input or exception messages, and never block the Codex task.
    DiagnosticLog.Write("hook", $"Hook failed: {ex.GetType().Name}");
}

return 0;
