using System.Windows;
using Optimizer.Views;

namespace Optimizer
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.Exit += App_Exit;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            if (MainWindow is MainWindow mainWin)
            {
                mainWin.Shutdown();
            }
        }
    }
}