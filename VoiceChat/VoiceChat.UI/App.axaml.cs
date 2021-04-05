using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Splat;
using VoiceChat.UI.ViewModels;
using VoiceChat.UI.Views;

namespace VoiceChat.UI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Initialize suspension hooks.
            var suspension = new AutoSuspendHelper(ApplicationLifetime);
            RxApp.SuspensionHost.CreateNewAppState = () => new MainWindowViewModel();
            RxApp.SuspensionHost.SetupDefaultSuspendResume(new NewtonsoftJsonSuspensionDriver("appstate.json"));
            suspension.OnFrameworkInitializationCompleted();

            // Read main view model state from disk.
            var state = RxApp.SuspensionHost.GetAppState<MainWindowViewModel>();
            Locator.CurrentMutable.RegisterConstant<IScreen>(state);

            // Show the main window.
            new MainWindow { DataContext = Locator.Current.GetService<IScreen>() }.Show();
            base.OnFrameworkInitializationCompleted();
        }

    }
}
