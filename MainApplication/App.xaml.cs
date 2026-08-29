using Microsoft.UI.Xaml;
namespace MainApplication
{
    public partial class App : Application
    {
        public static Window? _window { get; private set; }

        public App()
        {
            InitializeComponent();
        
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
