using HabitFlow.ViewModels;

namespace HabitFlow;

public partial class MainPage : TabbedPage
{
    public MainPage()
    {
        InitializeComponent();

        // Shared VM for both tabs
        BindingContext = new MainViewModel();
    }
}
