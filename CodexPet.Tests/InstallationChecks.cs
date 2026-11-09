using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using CodexPet.Install;

internal static class InstallationChecks
{
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "CodexPet-install-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string config = Path.Combine(root, "user", ".codex", "hooks.json");
            string state = Path.Combine(root, "data", "hooks-owned.json");
            string hook = Path.Combine(root, "공백 ' $기호", "CodexPet.Hook.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(config)!);
            string existing = "{\"description\":\"keep\",\"hooks\":{\"Stop\":[{\"matcher\":\"x\",\"hooks\":[{\"type\":\"command\",\"command\":\"other\"}]}],\"Custom\":[{\"hooks\":[]}]},\"extra\":{\"x\":1}}";
            File.WriteAllText(config, existing, new UTF8Encoding(true));
            HookRegistration.Update("check", config, hook, state);
            if (File.Exists(state)) throw new Exception("Preflight changed ownership.");
            HookRegistration.Update("register", config, hook, state);
            JsonObject actual = (JsonObject)JsonNode.Parse(File.ReadAllText(config))!;
            if (actual["description"]!.GetValue<string>() != "keep" || actual["hooks"]!["Stop"]!.AsArray().Count != 2 ||
                actual["hooks"]!["Custom"]!.AsArray().Count != 1 || actual["extra"]!["x"]!.GetValue<int>() != 1)
                throw new Exception("Existing configuration was not preserved.");
            string command = actual["hooks"]!["Stop"]![1]!["hooks"]![0]!["command"]!.GetValue<string>();
            if (command != "& '" + hook.Replace("'", "''") + "'") throw new Exception("Hook path quoting is incorrect.");
            string shellTest = Path.Combine(root, "공백 ' $기호", "quote-test.ps1");
            Directory.CreateDirectory(Path.GetDirectoryName(shellTest)!);
            File.WriteAllText(shellTest, "Write-Output 'path-ok'");
            using (var shell = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", "& '" + shellTest.Replace("'", "''") + "'" }
            }) ?? throw new Exception("PowerShell did not start."))
            {
                string output = shell.StandardOutput.ReadToEnd();
                shell.WaitForExit();
                if (shell.ExitCode != 0 || output.Trim() != "path-ok") throw new Exception("PowerShell did not execute the quoted path.");
            }
            HookRegistration.Update("register", config, hook, state);
            if (JsonNode.Parse(File.ReadAllText(config))!["hooks"]!["Stop"]!.AsArray().Count != 2)
                throw new Exception("Reinstallation duplicated a hook.");
            actual = (JsonObject)JsonNode.Parse(File.ReadAllText(config))!;
            actual["hooks"]!["Stop"]![1]!["hooks"]![0]!["timeout"] = 3;
            File.WriteAllText(config, actual.ToJsonString());
            string modified = File.ReadAllText(config);
            try { HookRegistration.Update("remove", config, hook, state); throw new Exception("Modified hook was removed."); }
            catch (InvalidDataException) { }
            if (File.ReadAllText(config) != modified || !File.Exists(state)) throw new Exception("Failed removal modified files.");
            actual["hooks"]!["Stop"]![1]!["hooks"]![0]!["timeout"] = 2;
            File.WriteAllText(config, actual.ToJsonString());
            HookRegistration.Update("remove", config, hook, state);
            actual = (JsonObject)JsonNode.Parse(File.ReadAllText(config))!;
            if (actual["hooks"]!["Stop"]!.AsArray().Count != 1 || actual["hooks"]!["Custom"] is null || File.Exists(state))
                throw new Exception("Removal did not preserve other hooks.");

            File.WriteAllText(config, "{invalid");
            try { HookRegistration.Update("register", config, hook, state); throw new Exception("Invalid JSON was accepted."); }
            catch (System.Text.Json.JsonException) { }
            if (File.ReadAllText(config) != "{invalid" || File.Exists(state)) throw new Exception("Invalid file was changed.");
            const string malformedGroup = "{\"hooks\":{\"Stop\":[{\"hooks\":\"bad\"}]}}";
            File.WriteAllText(config, malformedGroup);
            try { HookRegistration.Update("register", config, hook, state); throw new Exception("Malformed group was accepted."); }
            catch (InvalidDataException) { }
            if (File.ReadAllText(config) != malformedGroup) throw new Exception("Malformed group was changed.");
            byte[] invalidUtf8 = [0x7b, 0x22, 0x78, 0x22, 0x3a, 0x22, 0xff, 0x22, 0x7d];
            File.WriteAllBytes(config, invalidUtf8);
            try { HookRegistration.Update("register", config, hook, state); throw new Exception("Invalid UTF-8 was accepted."); }
            catch (DecoderFallbackException) { }
            if (!File.ReadAllBytes(config).SequenceEqual(invalidUtf8)) throw new Exception("Invalid UTF-8 was changed.");
            File.Delete(config);
            HookRegistration.Update("register", config, hook, state);
            string ownGroup = File.ReadAllText(config);
            File.Delete(state);
            try { HookRegistration.Update("register", config, hook, state); throw new Exception("Unowned matching hook was accepted."); }
            catch (InvalidDataException) { }
            try { HookRegistration.Update("remove", config, hook, state); throw new Exception("Removal without ownership was accepted."); }
            catch (InvalidDataException) { }
            if (File.ReadAllText(config) != ownGroup) throw new Exception("Unowned hook was changed.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        Console.WriteLine("Passed isolated installation, ownership, update, quoting and failure checks.");
    }
}
