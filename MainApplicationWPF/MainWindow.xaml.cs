using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MainApplicationWPF.Views;

namespace MainApplicationWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent(); 
            Loaded += (s, e) => RootNavigationView.Navigate(typeof(HomeView));
        }

        public IntPtr GetHwnd()
        {
            return new WindowInteropHelper(this).Handle;
        }

        private void NavigationViewItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}