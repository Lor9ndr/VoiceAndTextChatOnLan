using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VoiceChat.UI.ViewModels;

namespace VoiceChat.UI.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void MainWindow_Closed(object? sender, System.EventArgs e)
        {
            (DataContext as MainWindowViewModel).Disconnect();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
