using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CodexPet;

internal static class DiagnosticLog
{
    public static void Write(string component, string message, string? directory = null)
    {
        try
        {
            directory ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexPet", "Logs", "v2");
            Directory.CreateDirectory(directory);
            string line = JsonSerializer.Serialize(new { timestamp = DateTimeOffset.Now, processId = Environment.ProcessId, message }) + Environment.NewLine;
            byte[] bytes = Encoding.UTF8.GetBytes(line);
            const int maxBytes = 256 * 1024;
            if (bytes.Length > maxBytes) return;

            using var mutex = new Mutex(false, $"Local\\CodexPet.Log.v2.{component}");
            bool locked;
            try { locked = mutex.WaitOne(TimeSpan.FromMilliseconds(100)); }
            catch (AbandonedMutexException) { locked = true; }
            if (!locked) return;
            try
            {
                string path = Path.Combine(directory, $"{component}.log");
                if (File.Exists(path) && new FileInfo(path).Length + bytes.Length > maxBytes)
                {
                    if (File.Exists(path + ".1")) File.Move(path + ".1", path + ".2", true);
                    File.Move(path, path + ".1", true);
                }
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                stream.Write(bytes);
            }
            finally { mutex.ReleaseMutex(); }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Log write failed: {ex.GetType().Name}");
        }
    }
}
