namespace HabitFlow.Pages;

public partial class RemindersPage : ContentPage
{
    public RemindersPage()
    {
        InitializeComponent();
        BindingContext = ((HabitFlow.App)Application.Current!).MainVm;
    }
}
