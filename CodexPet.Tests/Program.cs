using System.Text.Json;
using System.IO;
using CodexPet.App;

var state = new PetState();
if (state.Gif != "idle.gif" || state.Text != "AURA FARMING")
    throw new Exception("The initial state must be idle.");

// Each entry is processed immediately after the preceding entry, including
// events from different sessions. A false result must not restart the GIF.
var events = new (string Json, string Gif, bool Changed)[]
{
    ("{\"hook_event_name\":\"UserPromptSubmit\",\"session_id\":\"a\"}", "receive.gif", true),
    ("{\"hook_event_name\":\"PreModelRequest\",\"session_id\":\"a\"}", "thinking.gif", true),
    ("{\"hook_event_name\":\"PostModelRequest\",\"session_id\":\"a\"}", "thinking.gif", false),
    ("{\"hook_event_name\":\"PreToolUse\",\"session_id\":\"a\"}", "working.gif", true),
    ("{\"hook_event_name\":\"PostToolUse\",\"session_id\":\"a\"}", "working.gif", false),
    ("{\"hook_event_name\":\"SessionStart\",\"source\":\"compact\"}", "working.gif", false),
    ("{\"hook_event_name\":\"PreCompact\"}", "working.gif", false),
    ("{\"hook_event_name\":\"PostCompact\"}", "working.gif", false),
    ("{\"hook_event_name\":\"SubagentStart\"}", "working.gif", false),
    ("{\"hook_event_name\":\"SubagentStop\"}", "working.gif", false),
    ("{\"hook_event_name\":\"UnknownEvent\"}", "working.gif", false),
    ("{\"hook_event_name\":\"PermissionRequest\",\"session_id\":\"a\"}", "waiting-permission.gif", true),
    ("{\"hook_event_name\":\"PermissionRequest\"}", "waiting-permission.gif", false),
    ("{\"hook_event_name\":\"PostToolUse\"}", "working.gif", true),
    ("{\"hook_event_name\":\"Stop\",\"session_id\":\"b\"}", "idle.gif", true),
    ("{\"hook_event_name\":\"Stop\"}", "idle.gif", false),
    ("{\"hook_event_name\":\"UserPromptSubmit\"}", "receive.gif", true),
    ("{\"hook_event_name\":\"SessionStart\",\"source\":\"resume\"}", "idle.gif", true),
    ("{\"hook_event_name\":\"PreToolUse\"}", "working.gif", true),
    ("{\"hook_event_name\":\"SessionStart\"}", "idle.gif", true),
    ("{\"hook_event_name\":\"PreToolUse\"}", "working.gif", true),
    ("{\"hook_event_name\":\"SessionEnd\"}", "idle.gif", true),
    ("{\"hook_event_name\":\"PreToolUse\"}", "working.gif", true),
    ("{\"hook_event_name\":\"Interrupt\"}", "idle.gif", true),
};

foreach (var (json, gif, changed) in events)
{
    var hookEvent = JsonSerializer.Deserialize<HookEvent>(json)!;
    var actualChanged = state.Apply(hookEvent);
    string expectedText = gif switch
    {
        "idle.gif" => "AURA FARMING",
        "receive.gif" => "CHAT WHAT IS THIS",
        "thinking.gif" => "LOWKEY COOKING RN",
        "working.gif" => "LOCKED IN FR",
        "waiting-permission.gif" => "JUST HIT APPROVE",
        _ => throw new Exception($"Unexpected GIF: {gif}")
    };
    if (state.Gif != gif || state.Text != expectedText || actualChanged != changed)
        throw new Exception($"{json}: expected {gif} / {expectedText} / {changed}, got {state.Gif} / {state.Text} / {actualChanged}.");
}

Console.WriteLine($"Passed {events.Length} consecutive event checks.");

Exception? settingsError = null;
var settingsThread = new Thread(() =>
{
    string directory = Path.Combine(Path.GetTempPath(), "CodexPet-tests-" + Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(directory);
        string source = Path.Combine(directory, "source.png");
        var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(1, 1, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 255 }, 4);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using (var stream = File.Create(source)) encoder.Save(stream);

        var settings = new PetSettings(Path.Combine(directory, "settings"));
        var edit = settings.Copy();
        edit.Values["idle.gif"].Images.Add(source);
        edit.Values["idle.gif"].Texts = ["안녕 lower", "", "   "];
        if (settings.Values["idle.gif"].Images.Count != 1) throw new Exception("Editing changed the live settings.");
        edit.Save();
        File.Delete(source);
        var loaded = PetSettings.Load(edit.DirectoryPath);
        if (loaded.Values["idle.gif"].Images.Count != 2 || !File.Exists(loaded.Values["idle.gif"].Images[1]) ||
            !loaded.Values["idle.gif"].Texts.SequenceEqual(new[] { "안녕 lower", "", "   " }))
            throw new Exception("Saved settings or image copy did not survive reload.");

        File.Delete(loaded.Values["idle.gif"].Images[1]);
        loaded = PetSettings.Load(edit.DirectoryPath);
        if (loaded.Values["idle.gif"].Images[1] != "asset:idle.gif" || loaded.Warnings.Count == 0 ||
            loaded.Values["idle.gif"].Texts[0] != "안녕 lower")
            throw new Exception("Missing image recovery did not preserve valid text.");
        var failedEdit = loaded.Copy();
        failedEdit.Values["idle.gif"].Images.Add(Path.Combine(directory, "missing.png"));
        try { failedEdit.Save(); throw new Exception("A missing image was saved."); }
        catch (IOException) { }
        if (failedEdit.Values["idle.gif"].Images.Count != 3 || PetSettings.Load(edit.DirectoryPath).Values["idle.gif"].Images.Count != 2)
            throw new Exception("Failed save changed the editing or persisted settings.");

        string settingsFile = Path.Combine(edit.DirectoryPath, "settings.json");
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(settingsFile))!;
        json["idle.gif"]!["Images"] = "invalid";
        File.WriteAllText(settingsFile, json.ToJsonString());
        var partial = PetSettings.Load(edit.DirectoryPath);
        if (!partial.Values["idle.gif"].Images.SequenceEqual(new[] { "asset:idle.gif" }) ||
            partial.Values["idle.gif"].Texts[0] != "안녕 lower" || partial.Warnings.Count == 0)
            throw new Exception("Corrupt image settings did not preserve valid text.");
        loaded.Values["receive.gif"].Texts.Clear();
        try { loaded.Save(); throw new Exception("Empty candidate list was saved."); }
        catch (InvalidOperationException) { }
        loaded.Reset("idle.gif");
        if (!loaded.Values["idle.gif"].Texts.SequenceEqual(new[] { "AURA FARMING" }))
            throw new Exception("Reset did not restore defaults.");
    }
    catch (Exception ex) { settingsError = ex; }
    finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
});
settingsThread.SetApartmentState(ApartmentState.STA);
settingsThread.Start();
settingsThread.Join();
if (settingsError is not null) throw new Exception("Settings checks failed.", settingsError);
Console.WriteLine("Passed settings persistence and recovery checks.");

Exception? captionError = null;
var captionThread = new Thread(() =>
{
    try
    {
        var app = new System.Windows.Application();
        var caption = new OutlinedCaption
        {
            FontFamily = new System.Windows.Media.FontFamily("Impact"),
            FontSize = 40
        };
        var typeface = new System.Windows.Media.Typeface(caption.FontFamily,
            caption.FontStyle, caption.FontWeight, caption.FontStretch);
        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface) ||
            !glyphTypeface.FamilyNames.Values.Contains("Impact"))
            throw new Exception("Impact is not available on this system.");
        var viewbox = new System.Windows.Controls.Viewbox
        {
            Width = 256,
            Height = 48,
            StretchDirection = System.Windows.Controls.StretchDirection.DownOnly,
            Child = caption
        };
        foreach (string text in new[] { "", "AURA FARMING", "CHAT WHAT IS THIS", "LOWKEY COOKING RN", "LOCKED IN FR", "JUST HIT APPROVE" })
        {
            caption.Text = text;
            viewbox.Measure(new System.Windows.Size(256, 48));
            viewbox.Arrange(new System.Windows.Rect(0, 0, 256, 48));
            if (text.Length == 0) continue;

            var visual = new System.Windows.Media.DrawingVisual();
            using (var context = visual.RenderOpen())
                typeof(OutlinedCaption).GetMethod("OnRender", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .Invoke(caption, new object[] { context });

            var outline = (System.Windows.Media.GeometryDrawing)visual.Drawing.Children[0];
            if (Math.Abs(outline.Pen.Thickness / 2 - outline.Geometry.Bounds.Height / 15) > 0.0001)
                throw new Exception($"Unexpected outline width for {text}.");
            if (outline.Pen.LineJoin != System.Windows.Media.PenLineJoin.Round)
                throw new Exception($"Unexpected outline join for {text}.");
        }
        app.Shutdown();
    }
    catch (Exception ex)
    {
        captionError = ex;
    }
});
captionThread.SetApartmentState(ApartmentState.STA);
captionThread.Start();
captionThread.Join();
if (captionError is not null) throw new Exception("Caption layout failed.", captionError);
Console.WriteLine("Passed caption layout checks without opening a window.");

if (args.Contains("--integration"))
    await PipeIntegration.RunAsync();
