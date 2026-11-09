using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodexPet.Install;

public static class HookRegistration
{
    public static void Update(string action, string hooksPath, string hookPath, string ownershipPath)
    {
        string[] events = ["SessionStart", "SessionEnd", "Interrupt", "Stop", "UserPromptSubmit", "PreModelRequest",
            "PostModelRequest", "PreToolUse", "PostToolUse", "PermissionRequest"];
        if (action is not ("check" or "register" or "remove")) throw new ArgumentException("알 수 없는 작업입니다.");
        hooksPath = Path.GetFullPath(hooksPath);
        hookPath = Path.GetFullPath(hookPath);
        ownershipPath = Path.GetFullPath(ownershipPath);
        string command = "& '" + hookPath.Replace("'", "''") + "'";
        var options = new JsonSerializerOptions { WriteIndented = true };
        byte[]? original = File.Exists(hooksPath) ? File.ReadAllBytes(hooksPath) : null;
        JsonObject root = original is null ? new JsonObject() :
            JsonNode.Parse(new UTF8Encoding(false, true).GetString(original).TrimStart('\uFEFF')) as JsonObject ?? throw new InvalidDataException("훅 설정 최상위는 JSON 객체여야 합니다.");
        if (root["hooks"] is not null && root["hooks"] is not JsonObject)
            throw new InvalidDataException("hooks 항목이 JSON 객체가 아닙니다.");
        var hooks = root["hooks"] as JsonObject ?? new JsonObject();
        if (root["hooks"] is null) root["hooks"] = hooks;

        JsonObject? recorded = null;
        if (File.Exists(ownershipPath))
        {
            var ownership = JsonNode.Parse(File.ReadAllBytes(ownershipPath)) as JsonObject;
            if (ownership?["version"]?.GetValue<int>() != 1 || ownership["groups"] is not JsonObject groups ||
                ownership["hookPath"]?.GetValue<string>() != hookPath)
                throw new InvalidDataException("훅 소유 기록이 손상되었거나 설치 경로와 일치하지 않습니다.");
            recorded = groups;
            if (recorded.Count != events.Length || events.Any(name => recorded[name] is not JsonObject))
                throw new InvalidDataException("훅 소유 기록이 불완전합니다.");
            foreach (string name in events)
            {
                if (hooks[name] is not JsonArray array || array.Count(item => JsonNode.DeepEquals(item, recorded[name])) != 1)
                    throw new InvalidDataException($"{name}의 기존 CodexPet 훅이 변경되었거나 중복되었습니다. 설정과 소유 기록을 확인해 주세요.");
            }
        }
        else if (action == "remove")
            throw new InvalidDataException("훅 소유 기록이 없어 제거할 항목을 안전하게 식별할 수 없습니다.");

        var desired = new JsonObject();
        foreach (string name in events)
        {
            if (hooks[name] is not null && hooks[name] is not JsonArray)
                throw new InvalidDataException($"{name} 항목이 배열이 아닙니다.");
            if (hooks[name] is JsonArray groups && groups.Any(item =>
                item is not JsonObject entry || entry["hooks"] is not JsonArray handlers ||
                handlers.Any(handler => handler is not JsonObject)))
                throw new InvalidDataException($"{name}의 기존 훅 그룹을 읽을 수 없습니다.");
            var group = new JsonObject
            {
                ["hooks"] = new JsonArray(new JsonObject { ["type"] = "command", ["command"] = command, ["timeout"] = 2 })
            };
            desired[name] = group.DeepClone();
            if (hooks[name] is JsonArray existing && existing.Any(item =>
                (recorded is null || !JsonNode.DeepEquals(item, recorded[name])) &&
                item is JsonObject other && other["hooks"] is JsonArray handlers && handlers.Any(handler =>
                    handler is JsonObject value && value["command"] is JsonValue property &&
                    property.TryGetValue<string>(out var otherCommand) && otherCommand == command)))
                throw new InvalidDataException($"{name}에 소유자를 확인할 수 없는 동일한 명령이 있습니다.");
        }
        if (action == "check") return;

        foreach (string name in events)
        {
            if (recorded is not null)
            {
                var array = (JsonArray)hooks[name]!;
                int index = Enumerable.Range(0, array.Count).Single(i => JsonNode.DeepEquals(array[i], recorded[name]));
                array.RemoveAt(index);
                if (array.Count == 0) hooks.Remove(name);
            }
            if (action == "register")
            {
                if (hooks[name] is not JsonArray array) hooks[name] = array = new JsonArray();
                array.Add(desired[name]!.DeepClone());
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(hooksPath)!);
        string temporary = hooksPath + ".codexpet-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, root.ToJsonString(options) + Environment.NewLine, new UTF8Encoding(false));
            if (action == "register")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ownershipPath)!);
                var ownership = new JsonObject { ["version"] = 1, ["hookPath"] = hookPath, ["groups"] = desired };
                string ownershipTemporary = ownershipPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(ownershipTemporary, ownership.ToJsonString(options) + Environment.NewLine, new UTF8Encoding(false));
                    File.Move(temporary, hooksPath, true);
                    try { File.Move(ownershipTemporary, ownershipPath, true); }
                    catch
                    {
                        if (original is null) File.Delete(hooksPath);
                        else File.WriteAllBytes(hooksPath, original);
                        throw;
                    }
                }
                finally { if (File.Exists(ownershipTemporary)) File.Delete(ownershipTemporary); }
            }
            else
            {
                File.Move(temporary, hooksPath, true);
                try { File.Delete(ownershipPath); }
                catch
                {
                    if (original is not null) File.WriteAllBytes(hooksPath, original);
                    throw;
                }
            }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
