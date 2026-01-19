namespace HabitFlow.Pages;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        InitializeComponent();
        BindingContext = ((HabitFlow.App)Application.Current!).MainVm;
    }
}
