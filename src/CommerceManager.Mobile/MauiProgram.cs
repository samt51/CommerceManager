using Microsoft.Extensions.Logging;
namespace CommerceManager.Mobile;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri("https://api.example.com/"));
        return builder.Build();
    }
}
