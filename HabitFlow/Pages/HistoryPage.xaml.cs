namespace HabitFlow.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();

        // Get the shared VM from App
        BindingContext = ((HabitFlow.App)Application.Current!).MainVm;
    }
}
