using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CodexPet.App;

public sealed class HookEvent
{
    [JsonPropertyName("hook_event_name")]
    public string? EventName {  get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    public enum Type
    {
        PreToolUse, PermissionRequest, PostToolUse, PreCompact, PostCompact, UserPromptSubmit, SubagentStop, Stop,
        PreModelRequest, PostModelRequest,
        Interrupt,
        SessionStart, SubagentStart,
        SessionEnd
    }

    public static Type? FromString(string name)
    {
        if (Enum.TryParse(name, out Type type))
        {
            return type;
        }

        return null;
    }
}
