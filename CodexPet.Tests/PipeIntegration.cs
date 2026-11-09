using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
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
        if (!File.Exists(hookPath)) throw new Exception("Build CodexPet.Hook in Debug first.");

        string marker = "SENSITIVE-" + Guid.NewGuid();
        foreach (var (eventName, source, expected) in new[]
        {
            ("PreToolUse", "compact", "{\"hook_event_name\":\"PreToolUse\"}"),
            ("SessionStart", "compact", "{\"hook_event_name\":\"SessionStart\",\"source\":\"compact\"}"),
            ("SessionStart", "resume", "{\"hook_event_name\":\"SessionStart\",\"source\":\"resume\"}"),
            ("SessionStart", marker, "{\"hook_event_name\":\"SessionStart\"}"),
        })
        {
            using var pipe = new NamedPipeServerStream("CodexPet.Events", PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            string input = JsonSerializer.Serialize(new { hook_event_name = eventName, source, session_id = marker,
                prompt = marker, tool_input = new { nested = marker }, tool_response = marker, extra = new { marker } });
            var hook = RunHookAsync(hookPath, input);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(pipe, Encoding.UTF8);
            string? actual = await reader.ReadLineAsync(timeout.Token);
            await hook;
            if (actual != expected || actual.Contains(marker)) throw new Exception("Unexpected transmitted message: " + actual);
        }

        foreach (string input in new[] { "{\"hook_event_name\":\"SessionStart\"}",
            "{\"hook_event_name\":\"SessionStart\",\"source\":42}",
            "{\"hook_event_name\":\"SessionStart\",\"source\":{\"nested\":\"secret\"}}" })
        {
            using var pipe = new NamedPipeServerStream("CodexPet.Events", PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            var hook = RunHookAsync(hookPath, input);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(pipe, Encoding.UTF8);
            if (await reader.ReadLineAsync(timeout.Token) != "{\"hook_event_name\":\"SessionStart\"}")
                throw new Exception("Invalid SessionStart source was forwarded.");
            await hook;
        }

        string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexPet", "Logs", "v2");
        string[] ignored = ["PreCompact", "PostCompact", "SubagentStart", "SubagentStop"];
        var beforeIgnored = Directory.Exists(logDirectory)
            ? Directory.GetFiles(logDirectory, "hook.log*").ToDictionary(path => path, File.ReadAllText)
            : new Dictionary<string, string>();
        foreach (string name in ignored)
            await RunHookAsync(hookPath, JsonSerializer.Serialize(new { hook_event_name = name, prompt = marker }));
        var afterIgnored = Directory.Exists(logDirectory)
            ? Directory.GetFiles(logDirectory, "hook.log*").ToDictionary(path => path, File.ReadAllText)
            : new Dictionary<string, string>();
        if (beforeIgnored.Count != afterIgnored.Count || beforeIgnored.Any(item => !afterIgnored.TryGetValue(item.Key, out string? value) || value != item.Value))
            throw new Exception("An ignored event changed the hook logs.");

        foreach (string input in new[] { "", "{", "{}", "{\"hook_event_name\":42}",
            "{\"hook_event_name\":\"PreCompact\"}", "{\"hook_event_name\":\"PostCompact\"}",
            "{\"hook_event_name\":\"SubagentStart\"}", "{\"hook_event_name\":\"SubagentStop\"}",
            JsonSerializer.Serialize(new { hook_event_name = marker }), new string('x', 1024 * 1024 + 1) })
        {
            using var pipe = new NamedPipeServerStream("CodexPet.Events", PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            using var noConnection = new CancellationTokenSource();
            var connection = pipe.WaitForConnectionAsync(noConnection.Token);
            await RunHookAsync(hookPath, input);
            if (connection.IsCompletedSuccessfully) throw new Exception("Invalid or ignored input was transmitted.");
            noConnection.Cancel();
            try { await connection; } catch (OperationCanceledException) { }
        }

        var received = Channel.CreateUnbounded<HookEvent>();
        using var cancellation = new CancellationTokenSource();
        var serverTask = new PetPipeServer().RunAsync(e => { received.Writer.TryWrite(e); return Task.CompletedTask; }, cancellation.Token);
        try
        {
            var sequence = new (string Event, string? Source, string Gif, bool Changed)[]
            {
                ("UserPromptSubmit", null, "receive.gif", true), ("PreModelRequest", null, "thinking.gif", true),
                ("PostModelRequest", null, "thinking.gif", false), ("PreToolUse", null, "working.gif", true),
                ("PostToolUse", null, "working.gif", false), ("SessionStart", "compact", "working.gif", false),
                ("PermissionRequest", null, "waiting-permission.gif", true), ("Stop", null, "idle.gif", true),
                ("PreToolUse", null, "working.gif", true), ("SessionStart", null, "idle.gif", true)
            };
            var state = new PetState();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            foreach (var item in sequence)
            {
                await RunHookAsync(hookPath, JsonSerializer.Serialize(new { hook_event_name = item.Event, source = item.Source, prompt = marker }));
                var e = await received.Reader.ReadAsync(deadline.Token);
                bool changed = state.Apply(e);
                if (e.EventName != item.Event || e.Source != item.Source || state.Gif != item.Gif || changed != item.Changed)
                    throw new Exception($"Unexpected event or state: {e.EventName} / {state.Gif} / {changed}.");
            }

            // A same-user process can inject a valid event without running the hook.
            using (var forged = new NamedPipeClientStream(".", "CodexPet.Events", PipeDirection.Out, PipeOptions.Asynchronous))
            {
                await forged.ConnectAsync(2000);
                await forged.WriteAsync(Encoding.UTF8.GetBytes("{\"hook_event_name\":\"PreToolUse\"}\n"));
            }
            if ((await received.Reader.ReadAsync(deadline.Token)).EventName != "PreToolUse")
                throw new Exception("Same-user pipe behavior changed.");

            foreach (string bad in new[] { "{\n", new string('x', 5000), "{\"hook_event_name\":\"Stop\"}" })
            {
                using var client = new NamedPipeClientStream(".", "CodexPet.Events", PipeDirection.Out, PipeOptions.Asynchronous);
                await client.ConnectAsync(2000);
                try { await client.WriteAsync(Encoding.UTF8.GetBytes(bad)); }
                catch (IOException) { }
            }
            using (var stalled = new NamedPipeClientStream(".", "CodexPet.Events", PipeDirection.Out, PipeOptions.Asynchronous))
            {
                await stalled.ConnectAsync(2000);
                await Task.Delay(2300);
            }
            await RunHookAsync(hookPath, "{\"hook_event_name\":\"Stop\"}");
            if ((await received.Reader.ReadAsync(deadline.Token)).EventName != "Stop") throw new Exception("Server did not recover.");
            if (received.Reader.TryRead(out _)) throw new Exception("Invalid message was processed.");
        }
        finally { cancellation.Cancel(); await serverTask; }

        await RunHookAsync(hookPath, "{\"hook_event_name\":\"Stop\"}");
        using (var stalled = StartHook(hookPath))
        {
            await stalled.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(4));
            if (stalled.ExitCode != 0) throw new Exception("Stalled input did not exit successfully.");
        }
        if (Directory.Exists(logDirectory) && Directory.GetFiles(logDirectory).Any(path => File.ReadAllText(path).Contains(marker)))
            throw new Exception("Synthetic sensitive data appeared in a new log.");
        Console.WriteLine("Passed privacy, hook-to-pipe, failure recovery and input timeout checks.");
    }

    private static Process StartHook(string path) => Process.Start(new ProcessStartInfo(path)
    {
        UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
        RedirectStandardOutput = true, RedirectStandardError = true,
    }) ?? throw new Exception("Could not start hook.");

    private static async Task RunHookAsync(string path, string input)
    {
        using var process = StartHook(path);
        try { await process.StandardInput.WriteAsync(input); }
        catch (IOException) { }
        process.StandardInput.Close();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        if (process.ExitCode != 0 || output.Length != 0 || error.Length != 0)
            throw new Exception($"Hook failed: exit {process.ExitCode}, stdout {output}, stderr {error}.");
    }
}
