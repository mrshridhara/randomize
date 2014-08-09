using System;
using System.Windows;

namespace Randomize
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.MainWindow = new MainWindow();
            this.MainWindow.Show();
            this.MainWindow.ContentRendered += OnMainWindowContentRendered;
            this.MainWindow.Closing += OnMainWindowClosing;
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private void OnMainWindowContentRendered(object sender, EventArgs e)
        {
            if (this.MainWindow != null)
            {
                this.MainWindow.Activate();
            }
        }

        private void OnMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.MainWindow.ContentRendered -= OnMainWindowContentRendered;
            this.MainWindow.Closing -= OnMainWindowClosing;
        }
    }
}
