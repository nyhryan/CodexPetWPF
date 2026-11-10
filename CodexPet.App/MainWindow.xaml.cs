using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using XamlAnimatedGif;
using Forms = System.Windows.Forms;

namespace CodexPet.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverTask;
    private readonly PetState _state = new();
    private PetSettings _settings;
    private SettingsWindow? _settingsWindow;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly Forms.ContextMenuStrip _trayMenu;
    private readonly Forms.ToolStripMenuItem _trayAlwaysOnTop;
    private readonly System.Drawing.Icon _trayImage;

    private bool _isClosing;
    private bool _canClose;

    private string _eventText = string.Empty;
    public string EventText
    {
        get => _eventText;
        set
        {
            if (_eventText == value) return;
            _eventText = value;

            PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(nameof(EventText)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow() : this(PetSettings.Load()) { }

    internal MainWindow(PetSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        Topmost = _settings.AlwaysOnTop;
        AlwaysOnTopMenuItem.IsChecked = Topmost;
        using (var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("CodexPet.TrayIcon.ico")!)
        using (var icon = new System.Drawing.Icon(stream))
            _trayImage = (System.Drawing.Icon)icon.Clone();
        _trayAlwaysOnTop = new Forms.ToolStripMenuItem("항상 위에 표시") { CheckOnClick = true, Checked = Topmost };
        _trayAlwaysOnTop.Click += (_, _) =>
        {
            AlwaysOnTopMenuItem.IsChecked = _trayAlwaysOnTop.Checked;
            AlwaysOnTopMenuItem_Click(AlwaysOnTopMenuItem, new RoutedEventArgs());
        };
        _trayMenu = new Forms.ContextMenuStrip();
        _trayMenu.Items.Add("창 보이기", null, (_, _) => ShowWidget());
        _trayMenu.Items.Add("설정", null, (_, _) =>
        {
            ShowWidget();
            SettingsMenuItem_Click(this, new RoutedEventArgs());
        });
        _trayMenu.Items.Add(_trayAlwaysOnTop);
        _trayMenu.Items.Add("종료", null, (_, _) => Close());
        _trayIcon = new Forms.NotifyIcon { Icon = _trayImage, Text = "CodexPet", ContextMenuStrip = _trayMenu, Visible = true };
        _trayIcon.DoubleClick += (_, _) => ShowWidget();
        Closed += (_, _) =>
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayMenu.Dispose();
            _trayImage.Dispose();
        };
        DataContext = this;
        UpdateDisplay();

        _serverTask = Task.Run(RunServerAsync);
        Closing += Window_Closing;
    }

    private async Task RunServerAsync()
    {
        try
        {
            var server = new PetPipeServer();
            await server.RunAsync(HandleEventAsync, _cts.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"파이프 서버 중단: {ex.GetType().Name}");
            DiagnosticLog.Write("app", $"Pipe server failed: {ex.GetType().Name}");
        }
    }

    private Task HandleEventAsync(HookEvent hookEvent)
    {
        return Dispatcher.InvokeAsync(
            () =>
            {
                if (_state.Apply(hookEvent))
                {
                    UpdateDisplay();
                }
            },
            DispatcherPriority.Normal,
            _cts.Token
            ).Task;
    }

    private void UpdateDisplay()
    {
        var candidates = _settings.Values[_state.Key];
        string text = candidates.Texts[Random.Shared.Next(candidates.Texts.Count)];
        EventText = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        Title = string.IsNullOrEmpty(EventText) ? "CodexPet" : $"CodexPet - {EventText}";
        AnimationBehavior.SetSourceUri(image_gif, null);
        image_gif.Source = null;
        var images = candidates.Images.ToArray();
        Random.Shared.Shuffle(images);
        foreach (string image in images)
        {
            try
            {
                if (PetSettings.ValidateImage(image))
                    AnimationBehavior.SetSourceUri(image_gif, new Uri(image, UriKind.Absolute));
                else
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(image, UriKind.Absolute);
                    bitmap.EndInit();
                    image_gif.Source = bitmap;
                }
                break;
            }
            catch (Exception)
            {
                var name = PetSettings.States.First(s => s.Key == _state.Key).Name;
                string warning = $"{name}: 이미지를 읽을 수 없습니다: {image}";
                if (!_settings.Warnings.Contains(warning)) _settings.Warnings.Add(warning);
                _settingsWindow?.ShowWarnings(_settings);
                AnimationBehavior.SetSourceUri(image_gif, null);
                image_gif.Source = null;
            }
        }
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_settings, () => _settings.AlwaysOnTop, settings =>
        {
            var before = _settings.Values[_state.Key];
            var after = settings.Values[_state.Key];
            bool changed = !before.Images.SequenceEqual(after.Images) || !before.Texts.SequenceEqual(after.Texts);
            _settings = settings;
            if (changed) UpdateDisplay();
        }) { Owner = this };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void AlwaysOnTopMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Topmost = AlwaysOnTopMenuItem.IsChecked;
        _trayAlwaysOnTop.Checked = Topmost;
        _settings.AlwaysOnTop = Topmost;
        try
        {
            _settings.SaveAlwaysOnTop();
        }
        catch (Exception ex)
        {
            if (!IsVisible) ShowWidget();
            MessageBox.Show(_settingsWindow ?? (Window)this,
                "항상 위에 표시 설정을 저장할 수 없습니다. 현재 선택은 적용되지만 앱을 다시 실행하면 유지되지 않을 수 있습니다.\n" + ex.Message,
                "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MinimizeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _settingsWindow?.Hide();
        Hide();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized) return;
        MinimizeMenuItem_Click(this, new RoutedEventArgs());
        WindowState = WindowState.Normal;
    }

    private void ShowWidget()
    {
        if (!IsVisible) Show();
        Activate();
        if (_settingsWindow is not null)
        {
            if (!_settingsWindow.IsVisible) _settingsWindow.Show();
            _settingsWindow.Activate();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_canClose) return;

        e.Cancel = true;

        if (_isClosing) return;

        _isClosing = true;
        _cts.Cancel();

        try
        {
            await _serverTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"서버 종료 중 오류: {ex.GetType().Name}");
        }
        finally
        {
            _cts.Dispose();
            _canClose = true;
            Dispatcher.BeginInvoke(new Action(Close));
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
