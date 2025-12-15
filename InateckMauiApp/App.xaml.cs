using InateckMauiApp.Views;

namespace InateckMauiApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new MainPage(
            Handler!.MauiContext!.Services.GetRequiredService<ViewModels.MainViewModel>()));
    }
}
