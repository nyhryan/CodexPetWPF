using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace CodexPet.App;

internal partial class SettingsWindow : Window
{
    private readonly PetSettings _editing;
    private readonly Action<PetSettings> _apply;
    private string CurrentKey => ((StateChoice)StatePicker.SelectedItem).Key;
    private sealed record StateChoice(string Key, string Name);

    public SettingsWindow(PetSettings settings, Action<PetSettings> apply)
    {
        InitializeComponent();
        _editing = settings.Copy();
        _apply = apply;
        WarningsText.Text = string.Join("\n", _editing.Warnings.Distinct());
        StatePicker.ItemsSource = PetSettings.States.Select(s => new StateChoice(s.Key, s.Name)).ToArray();
        StatePicker.SelectedIndex = 0;
    }

    private void RefreshLists()
    {
        var state = _editing.Values[CurrentKey];
        ImagesList.ItemsSource = state.Images.Select(path => path.StartsWith("asset:") ? "기본 이미지 (" + path[6..] + ")" : Path.GetFileName(path)).ToArray();
        TextsList.ItemsSource = state.Texts.Select(text => string.IsNullOrWhiteSpace(text) ? (text.Length == 0 ? "(빈 문구)" : "(공백 문구)") : text).ToArray();
    }

    private void StatePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatePicker.SelectedItem is not null) RefreshLists();
    }

    private void AddImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "이미지 파일|*.gif;*.png;*.jpg;*.jpeg|모든 파일|*.*", Multiselect = true };
        if (dialog.ShowDialog(this) != true) return;
        foreach (string path in dialog.FileNames)
        {
            try
            {
                PetSettings.ValidateImage(path);
                _editing.Values[CurrentKey].Images.Add(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"이미지를 추가할 수 없습니다.\n{Path.GetFileName(path)}: {ex.Message}", "이미지 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        RefreshLists();
    }

    private void DeleteImage_Click(object sender, RoutedEventArgs e)
    {
        if (ImagesList.SelectedIndex >= 0) _editing.Values[CurrentKey].Images.RemoveAt(ImagesList.SelectedIndex);
        RefreshLists();
    }

    private void AddText_Click(object sender, RoutedEventArgs e)
    {
        _editing.Values[CurrentKey].Texts.Add(CaptionInput.Text);
        CaptionInput.Clear();
        RefreshLists();
    }

    private void DeleteText_Click(object sender, RoutedEventArgs e)
    {
        if (TextsList.SelectedIndex >= 0) _editing.Values[CurrentKey].Texts.RemoveAt(TextsList.SelectedIndex);
        RefreshLists();
    }

    private void ResetState_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "선택한 상태를 기본값으로 초기화할까요?", "초기화 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _editing.Reset(CurrentKey);
        RefreshLists();
    }

    private void ResetAll_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "모든 상태를 기본값으로 초기화할까요?", "초기화 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        foreach (var state in PetSettings.States) _editing.Reset(state.Key);
        RefreshLists();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _editing.Save();
            _apply(_editing);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "설정을 저장할 수 없습니다.\n" + ex.Message, "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
