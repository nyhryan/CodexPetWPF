using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace CodexPet.App;

internal sealed class StateSettings
{
    public List<string> Images { get; set; } = new();
    public List<string> Texts { get; set; } = new();
}

internal sealed class PetSettings
{
    public static readonly (string Key, string Name, string Text)[] States =
    [
        ("idle.gif", "대기", "AURA FARMING"),
        ("receive.gif", "의뢰 확인", "CHAT WHAT IS THIS"),
        ("thinking.gif", "생각 중", "LOWKEY COOKING RN"),
        ("working.gif", "작업 중", "LOCKED IN FR"),
        ("waiting-permission.gif", "허락 대기", "JUST HIT APPROVE")
    ];

    public Dictionary<string, StateSettings> Values { get; } = new();
    public List<string> Warnings { get; } = new();
    public string DirectoryPath { get; }

    public PetSettings(string? directoryPath = null)
    {
        DirectoryPath = directoryPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexPet");
        foreach (var state in States) Reset(state.Key);
    }

    public void Reset(string key)
    {
        var state = States.First(s => s.Key == key);
        Values[key] = new StateSettings { Texts = [state.Text] };
    }

    public PetSettings Copy()
    {
        var copy = new PetSettings(DirectoryPath);
        foreach (var (key, value) in Values)
            copy.Values[key] = new StateSettings { Images = [.. value.Images], Texts = [.. value.Texts] };
        copy.Warnings.AddRange(Warnings);
        return copy;
    }

    public static bool ValidateImage(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder is not GifBitmapDecoder and not PngBitmapDecoder and not JpegBitmapDecoder || decoder.Frames.Count == 0)
            throw new InvalidDataException("GIF, PNG 또는 JPEG 이미지가 아닙니다.");
        return decoder is GifBitmapDecoder;
    }

    public static PetSettings Load(string? directoryPath = null)
    {
        var settings = new PetSettings(directoryPath);
        string path = Path.Combine(settings.DirectoryPath, "settings.json");
        if (!File.Exists(path)) return settings;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var state in States)
            {
                JsonElement value;
                try
                {
                    value = document.RootElement.GetProperty(state.Key);
                }
                catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
                {
                    settings.Warnings.Add($"{state.Name}: 설정을 읽을 수 없어 기본값을 사용합니다.");
                    continue;
                }
                try
                {
                    var images = new List<string>();
                    foreach (var item in value.GetProperty("Images").EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && item.GetString() is string image)
                        {
                            if (image.StartsWith("asset:", StringComparison.OrdinalIgnoreCase))
                                settings.Warnings.Add($"{state.Name}: 이전 기본 이미지가 제거되었습니다. 원하는 이미지를 추가해 주세요.");
                            else
                            {
                                images.Add(image);
                                try { ValidateImage(image); }
                                catch (Exception)
                                {
                                    settings.Warnings.Add($"{state.Name}: 이미지를 읽을 수 없습니다: {image}");
                                }
                            }
                        }
                        else
                        {
                            settings.Warnings.Add($"{state.Name}: 이미지 설정 항목을 읽을 수 없습니다.");
                        }
                    }
                    settings.Values[state.Key].Images = images;
                }
                catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or InvalidDataException or FormatException)
                {
                    settings.Warnings.Add($"{state.Name}: 이미지 설정을 읽을 수 없어 이미지 영역을 비웁니다.");
                }
                try
                {
                    var texts = new List<string>();
                    foreach (var item in value.GetProperty("Texts").EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && item.GetString() is string text) texts.Add(text);
                        else
                        {
                            texts.Add(state.Text);
                            settings.Warnings.Add($"{state.Name}: 문구 설정을 읽을 수 없어 기본 문구를 사용합니다.");
                        }
                    }
                    if (texts.Count == 0) throw new InvalidDataException();
                    settings.Values[state.Key].Texts = texts;
                }
                catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or InvalidDataException)
                {
                    settings.Warnings.Add($"{state.Name}: 문구 설정을 읽을 수 없어 기본 문구를 사용합니다.");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            settings.Warnings.Add("설정을 읽을 수 없어 기본값을 사용합니다: " + ex.Message);
        }
        return settings;
    }

    public void Save()
    {
        foreach (var state in States)
            if (Values[state.Key].Texts.Count == 0)
                throw new InvalidOperationException($"{state.Name}에 문구를 하나 이상 등록해 주세요.");

        Directory.CreateDirectory(DirectoryPath);
        string imageDirectory = Path.Combine(DirectoryPath, "images");
        Directory.CreateDirectory(imageDirectory);
        var saved = Copy();
        foreach (var state in States)
        {
            var images = saved.Values[state.Key].Images;
            for (int i = 0; i < images.Count; i++)
            {
                if (string.Equals(Path.GetDirectoryName(Path.GetFullPath(images[i])), Path.GetFullPath(imageDirectory), StringComparison.OrdinalIgnoreCase)) continue;
                ValidateImage(images[i]);
                string destination = Path.Combine(imageDirectory, Guid.NewGuid().ToString("N") + Path.GetExtension(images[i]));
                File.Copy(images[i], destination);
                images[i] = destination;
            }
        }
        string path = Path.Combine(DirectoryPath, "settings.json");
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(saved.Values, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, true);
        foreach (var state in States) Values[state.Key] = saved.Values[state.Key];
        Warnings.Clear();
        foreach (var state in States)
            foreach (string image in Values[state.Key].Images)
                try { ValidateImage(image); }
                catch (Exception) { Warnings.Add($"{state.Name}: 이미지를 읽을 수 없습니다: {image}"); }
    }
}
