using System;
using System.Windows;

namespace TheOrganizer
{
    public partial class App : Application
    {
        // This method runs when the app starts
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // manually create the window
                MainWindow window = new MainWindow();
                window.Show();
            }
            catch (Exception ex)
            {
                // This catches XAML parsing errors (like missing animations/colors)
                MessageBox.Show($"FATAL STARTUP ERROR:\n\n{ex.Message}\n\nInner Error: {ex.InnerException?.Message}", "App Crashing");
                Shutdown();
            }
        }
    }
}