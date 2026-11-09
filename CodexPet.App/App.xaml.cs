using System.Configuration;
using System.Data;
using System.Windows;

namespace CodexPet.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly System.Threading.Mutex _running = new(false, "Local\\CodexPet.Widget");

        protected override void OnExit(ExitEventArgs e)
        {
            _running.Dispose();
            base.OnExit(e);
        }
    }

}
