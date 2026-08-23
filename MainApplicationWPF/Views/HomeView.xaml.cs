using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MainApplicationWPF.Views
{
    public partial class HomeView : UserControl
    {
        internal static class NativeMethods
        {
            [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CreateMfcDialog(IntPtr hParentHwnd);
            [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ResizeMfcDialog(IntPtr hDlg, int width, int height);
        }

        public HomeView()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window? window = Application.Current.MainWindow;
            var interopHelper = new WindowInteropHelper(window);
            NativeMethods.CreateMfcDialog(interopHelper.Handle);
        }
    }
}
