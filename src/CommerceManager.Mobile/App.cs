namespace CommerceManager.Mobile;
public sealed class App : Microsoft.Maui.Controls.Application
{
    public App() => MainPage = new ContentPage { Content = new Label { Text = "CommerceManager", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
}
