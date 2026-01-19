using HabitFlow.ViewModels;
using Microsoft.Maui.ApplicationModel;


namespace HabitFlow;

public partial class App : Application
{
    public MainViewModel MainVm { get; } = new MainViewModel();

    public App()
    {
        InitializeComponent();

        // Global exception logging (prevents silent crashes)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine("UNHANDLED (AppDomain): " + ex);
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("UNOBSERVED (Task): " + e.Exception);
                e.SetObserved();
            }
            catch { }
        };

        MainPage = new AppShell();
    }
}
