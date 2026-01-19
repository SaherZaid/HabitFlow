using HabitFlow.Pages;

namespace HabitFlow;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(DayDetailsPage), typeof(DayDetailsPage));
    }
}
