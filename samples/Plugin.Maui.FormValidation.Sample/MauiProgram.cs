using Microsoft.Extensions.Logging;
using Plugin.Maui.FormValidation;

namespace Plugin.Maui.FormValidation.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<SignUpViewModel>();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseMauiFormValidation(options =>
            {
                options.Trigger = ValidationTrigger.LostFocus;
                options.ShowMessage = true;
                options.AsyncDebounce = TimeSpan.FromMilliseconds(350);
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
