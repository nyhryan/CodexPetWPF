using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;
using CodexPet.App;

internal static class PipeIntegration
{
    public static async Task RunAsync()
    {
        if (Process.GetProcessesByName("CodexPet.App").Length != 0)
            throw new Exception("Close the widget before running the pipe integration test.");

        var hookPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../CodexPet.Hook/bin/Debug/net10.0/CodexPet.Hook.exe"));
        if (!File.Exists(hookPath))
            throw new Exception("Build CodexPet.Hook in Debug before running the pipe integration test.");

        string sessionId = "validation-" + Guid.NewGuid();
        var received = Channel.CreateUnbounded<HookEvent>();
        using var cancellation = new CancellationTokenSource();
        var server = new PetPipeServer();
        var serverTask = server.RunAsync(hookEvent =>
        {
            if (hookEvent.SessionId == sessionId)
                received.Writer.TryWrite(hookEvent);
            return Task.CompletedTask;
        }, cancellation.Token);

        try
        {
            var sequence = new (string Event, string? Source, string Gif, bool Changed)[]
            {
                ("UserPromptSubmit", null, "receive.gif", true),
                ("PreModelRequest", null, "thinking.gif", true),
                ("PostModelRequest", null, "thinking.gif", false),
                ("PreToolUse", null, "working.gif", true),
                ("PostToolUse", null, "working.gif", false),
                ("SessionStart", "compact", "working.gif", false),
                ("PermissionRequest", null, "waiting-permission.gif", true),
                ("Stop", null, "idle.gif", true),
                ("SubagentStop", null, "idle.gif", false),
            };

            foreach (var item in sequence)
            {
                var input = JsonSerializer.Serialize(new { hook_event_name = item.Event, session_id = sessionId, source = item.Source });
                await RunHookAsync(hookPath, input);
            }

            var state = new PetState();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            foreach (var item in sequence)
            {
                var hookEvent = await received.Reader.ReadAsync(deadline.Token);
                bool changed = state.Apply(hookEvent);
                if (hookEvent.EventName != item.Event || state.Gif != item.Gif || changed != item.Changed)
                    throw new Exception($"Unexpected pipe event or state: {hookEvent.EventName} / {state.Gif} / {changed}.");
            }

            Console.WriteLine($"Passed {sequence.Length} hook-to-pipe event checks in order.");
        }
        finally
        {
            cancellation.Cancel();
            await serverTask;
        }

        await RunHookAsync(hookPath, "{\"hook_event_name\":\"Stop\"}");
        await RunHookAsync(hookPath, "{\"hook_event_name\":\"SubagentStop\"}");
        Console.WriteLine("Hook exits successfully with empty stdout when the widget is unavailable.");
    }

    private static async Task RunHookAsync(string hookPath, string input)
    {
        using var process = Process.Start(new ProcessStartInfo(hookPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new Exception("Could not start hook.");

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0 || output.Length != 0 || error.Length != 0)
            throw new Exception($"Hook failed: exit {process.ExitCode}, stdout {output}, stderr {error}.");
    }
}
