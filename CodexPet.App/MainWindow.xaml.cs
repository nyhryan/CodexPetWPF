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

namespace CodexPet.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverTask;
    private readonly PetState _state = new();

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

    public MainWindow()
    {
        InitializeComponent();
        EventText = _state.Text;
        DataContext = this;

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
            Debug.WriteLine($"파이프 서버 중단: {ex}");
            DiagnosticLog.Write("app", $"Pipe server failed: {ex.GetType().Name}");
        }
    }

    private Task HandleEventAsync(HookEvent hookEvent)
    {
        return Dispatcher.InvokeAsync(
            () =>
            {
                Debug.WriteLine($"이벤트 수신: {hookEvent.EventName}");

                if (_state.Apply(hookEvent))
                {
                    Title = _state.Text;
                    EventText = _state.Text;
                    ChangeGif(_state.Gif);
                }
            },
            DispatcherPriority.Normal,
            _cts.Token
            ).Task;
    }

    private void ChangeGif(string fileName)
    {
        if (Uri.TryCreate($"pack://application:,,,/assets/{fileName}", UriKind.Absolute, out var gifFile))
        {
            AnimationBehavior.SetSourceUri(image_gif, gifFile);
        }
        else
        {
            Console.Error.WriteLine($"파일 {fileName}을 찾을 수 없습니다.");
            DiagnosticLog.Write("app", $"파일 {fileName}을 찾을 수 없습니다.");
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
            Debug.WriteLine($"서버 종료 중 오류: {ex}");
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
