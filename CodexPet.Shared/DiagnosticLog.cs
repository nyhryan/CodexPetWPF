using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace CodexPet;

internal static class DiagnosticLog
{
    public static void Write(string component, string message, string? eventName = null, string? sessionId = null)
    {
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexPet", "Logs");
            Directory.CreateDirectory(directory);
            string line = JsonSerializer.Serialize(new { timestamp = DateTimeOffset.Now, processId = Environment.ProcessId, message, eventName, sessionId });

            using var mutex = new Mutex(false, $"Local\\CodexPet.Log.{component}");
            bool locked;
            try { locked = mutex.WaitOne(TimeSpan.FromMilliseconds(100)); }
            catch (AbandonedMutexException) { locked = true; }
            if (!locked) return;
            try { File.AppendAllText(Path.Combine(directory, $"{component}.log"), line + Environment.NewLine); }
            finally { mutex.ReleaseMutex(); }
        }
        catch (Exception ex)
        {
            // Diagnostics must not interrupt a hook or the pet.
            Debug.WriteLine($"Log write failed: {ex.Message}");
        }
    }
}
