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

InstallationChecks.Run();

string logTestDirectory = Path.Combine(Path.GetTempPath(), "CodexPet-log-tests-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(logTestDirectory);
    string legacy = Path.Combine(logTestDirectory, "app.log");
    File.WriteAllText(legacy, "legacy session ID");
    string current = Path.Combine(logTestDirectory, "v2");
    for (int i = 0; i < 900; i++)
        CodexPet.DiagnosticLog.Write("app", new string('x', 1000), current);
    string[] files = Directory.GetFiles(current);
    if (files.Length != 3 || files.Any(path => new FileInfo(path).Length > 256 * 1024) ||
        File.ReadAllText(legacy) != "legacy session ID")
        throw new Exception($"Log rotation or legacy log preservation failed: {string.Join(", ", files.Select(path => Path.GetFileName(path) + ":" + new FileInfo(path).Length))}.");
    CodexPet.DiagnosticLog.Write("app", "test failure", legacy);
}
finally { if (Directory.Exists(logTestDirectory)) Directory.Delete(logTestDirectory, true); }
Console.WriteLine("Passed log size, rotation, legacy preservation and write failure checks.");

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

        string gif = Path.Combine(directory, "source.gif");
        var gifEncoder = new System.Windows.Media.Imaging.GifBitmapEncoder();
        gifEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using (var stream = File.Create(gif)) gifEncoder.Save(stream);
        string jpeg = Path.Combine(directory, "source.jpg");
        var jpegEncoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
        jpegEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using (var stream = File.Create(jpeg)) jpegEncoder.Save(stream);

        var settings = new PetSettings(Path.Combine(directory, "settings"));
        if (settings.Values.Values.Any(value => value.Images.Count != 0) ||
            settings.Values["working.gif"].Texts.Single() != "LOCKED IN FR")
            throw new Exception("Fresh settings must have empty images and the specified default texts.");
        var edit = settings.Copy();
        edit.Values["idle.gif"].Images.Add(source);
        edit.Values["receive.gif"].Images.Add(gif);
        edit.Values["thinking.gif"].Images.Add(jpeg);
        edit.Values["idle.gif"].Texts = ["안녕 lower", "", "   "];
        if (settings.Values["idle.gif"].Images.Count != 0) throw new Exception("Editing changed the live settings.");
        edit.Save();
        File.Delete(source);
        File.Delete(gif);
        File.Delete(jpeg);
        var loaded = PetSettings.Load(edit.DirectoryPath);
        if (loaded.Values["idle.gif"].Images.Count != 1 || !File.Exists(loaded.Values["idle.gif"].Images[0]) ||
            !loaded.Values["idle.gif"].Texts.SequenceEqual(new[] { "안녕 lower", "", "   " }))
            throw new Exception("Saved settings or image copy did not survive reload.");
        if (!PetSettings.ValidateImage(loaded.Values["receive.gif"].Images.Single()) ||
            PetSettings.ValidateImage(loaded.Values["thinking.gif"].Images.Single()))
            throw new Exception("Saved GIF and JPEG copies did not survive reload.");

        string copy = loaded.Values["idle.gif"].Images[0];
        string settingsFile = Path.Combine(edit.DirectoryPath, "settings.json");
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(settingsFile))!;
        json["idle.gif"]!["Images"] = new System.Text.Json.Nodes.JsonArray("asset:idle.gif", copy, "asset:working.gif");
        File.WriteAllText(settingsFile, json.ToJsonString());
        loaded = PetSettings.Load(edit.DirectoryPath);
        if (!loaded.Values["idle.gif"].Images.SequenceEqual(new[] { copy }) ||
            loaded.Values["idle.gif"].Texts[0] != "안녕 lower" || loaded.Warnings.Count == 0 || !File.Exists(copy))
            throw new Exception("Legacy migration did not preserve the user image and text.");
        loaded.Save();
        if (File.ReadAllText(settingsFile).Contains("asset:") || PetSettings.Load(edit.DirectoryPath).Warnings.Count != 0)
            throw new Exception("Legacy images were not removed on save.");

        File.Delete(copy);
        loaded = PetSettings.Load(edit.DirectoryPath);
        if (loaded.Values["idle.gif"].Images.Single() != copy || loaded.Warnings.Count == 0 ||
            loaded.Values["idle.gif"].Texts[0] != "안녕 lower")
            throw new Exception("Missing image recovery did not preserve valid text.");
        loaded.Save();
        if (loaded.Warnings.Count == 0) throw new Exception("Saving hid an unreadable saved image.");
        var failedEdit = loaded.Copy();
        failedEdit.Values["idle.gif"].Images.Add(Path.Combine(directory, "missing.png"));
        try { failedEdit.Save(); throw new Exception("A missing image was saved."); }
        catch (IOException) { }
        if (failedEdit.Values["idle.gif"].Images.Count != 2 || PetSettings.Load(edit.DirectoryPath).Values["idle.gif"].Images.Count != 1)
            throw new Exception("Failed save changed the editing or persisted settings.");

        json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(settingsFile))!;
        json["idle.gif"]!["Images"] = "invalid";
        File.WriteAllText(settingsFile, json.ToJsonString());
        var partial = PetSettings.Load(edit.DirectoryPath);
        if (partial.Values["idle.gif"].Images.Count != 0 ||
            partial.Values["idle.gif"].Texts[0] != "안녕 lower" || partial.Warnings.Count == 0)
            throw new Exception("Corrupt image settings did not preserve valid text.");
        loaded.Values["receive.gif"].Texts.Clear();
        try { loaded.Save(); throw new Exception("Empty candidate list was saved."); }
        catch (InvalidOperationException) { }
        loaded.Reset("idle.gif");
        if (loaded.Values["idle.gif"].Images.Count != 0 || !loaded.Values["idle.gif"].Texts.SequenceEqual(new[] { "AURA FARMING" }))
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

        // Instantiate the real widget with a new, isolated settings directory and
        // a working directory that contains no personal assets.
        string isolated = Path.Combine(Path.GetTempPath(), "CodexPet-widget-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(isolated);
        string previousDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = isolated;
            var fresh = PetSettings.Load(Path.Combine(isolated, "settings"));
            var window = new MainWindow(fresh);
            window.Show();
            var image = (System.Windows.Controls.Image)window.FindName("image_gif");
            if (window.EventText != "AURA FARMING" || image.Source is not null)
                throw new Exception("Fresh widget did not display an empty idle state.");
            var stateField = typeof(MainWindow).GetField("_state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var update = typeof(MainWindow).GetMethod("UpdateDisplay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var windowState = (PetState)stateField.GetValue(window)!;
            windowState.Apply(new HookEvent { EventName = "PreToolUse" });
            update.Invoke(window, null);
            if (window.EventText != "LOCKED IN FR" || image.Source is not null)
                throw new Exception("Empty working state did not display its default text.");

            string valid = Path.Combine(isolated, "valid.png");
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(System.Windows.Media.Imaging.BitmapSource.Create(
                1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 255 }, 4)));
            using (var stream = File.Create(valid)) encoder.Save(stream);
            fresh.Values["working.gif"].Images.Add(Path.Combine(isolated, "missing.png"));
            update.Invoke(window, null);
            if (image.Source is not null || fresh.Warnings.Count == 0)
                throw new Exception("An unreadable image did not leave an empty area and report a warning.");
            fresh.Values["working.gif"].Images.Add(valid);
            for (int i = 0; i < 20; i++)
            {
                update.Invoke(window, null);
                if (image.Source is null) throw new Exception("A valid image was not selected after an unreadable candidate.");
            }
            var menu = typeof(MainWindow).GetMethod("SettingsMenuItem_Click", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            menu.Invoke(window, new object?[] { window, new System.Windows.RoutedEventArgs() });
            var settingsWindow = (System.Windows.Window)typeof(MainWindow).GetField("_settingsWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(window)!;
            if (settingsWindow.FindName("WarningsText") is not System.Windows.Controls.TextBlock warnings || string.IsNullOrEmpty(warnings.Text))
                throw new Exception("The settings window did not report an unreadable image.");
            settingsWindow.Close();
            window.Close();
            var frame = new System.Windows.Threading.DispatcherFrame();
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
            Directory.Delete(isolated, true);
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
Console.WriteLine("Passed caption layout and isolated widget checks.");

if (args.Contains("--integration"))
    await PipeIntegration.RunAsync();
