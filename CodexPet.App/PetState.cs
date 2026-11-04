namespace CodexPet.App;

internal sealed class PetState
{
    public string Gif { get; private set; } = "idle.gif";
    public string Text { get; private set; } = "AURA FARMING";
    public string Key => Gif;

    public bool Apply(HookEvent hookEvent)
    {
        var next = hookEvent.EventName switch
        {
            "SessionStart" when hookEvent.Source != "compact" => ("idle.gif", "AURA FARMING"),
            "Stop" or "SessionEnd" or "Interrupt" => ("idle.gif", "AURA FARMING"),
            "UserPromptSubmit" => ("receive.gif", "CHAT WHAT IS THIS"),
            "PreModelRequest" or "PostModelRequest" => ("thinking.gif", "LOWKEY COOKING RN"),
            "PreToolUse" or "PostToolUse" => ("working.gif", "LOCKED IN FR"),
            "PermissionRequest" => ("waiting-permission.gif", "JUST HIT APPROVE"),
            _ => (Gif, Text)
        };

        if (next.Item1 == Gif) return false;

        (Gif, Text) = next;
        return true;
    }
}
