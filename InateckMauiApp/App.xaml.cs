namespace InateckMauiApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // MainActivity usa UI nativa Android, no se necesita MainPage MAUI
        MainPage = new ContentPage();
    }
}
