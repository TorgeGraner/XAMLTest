using Microsoft.UI.Composition;
using Microsoft.UI.Content;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MainApplication
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OtherPage : Page
    {
        public OtherPage()
        {
            this.InitializeComponent(); 
            this.Loaded += MainPage_Loaded;
        }
        private IntPtr _hMfcStatic = IntPtr.Zero;
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private const uint SWP_SHOWWINDOW = 0x0040;
        internal static class NativeMethods
        {
            [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern IntPtr CreateMfcDialog(IntPtr hParentHwnd);

            [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern void ResizeMfcDialog(IntPtr hDlg, int width, int height);
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        }
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the HWND of the main WinUI 3 Window
            // Note: Replace App.MainWindow with your actual Window instance reference

            if (App._mainHwnd != IntPtr.Zero)
            {
                _hMfcStatic = NativeMethods.CreateMfcDialog(App._mainHwnd);
                UpdateControlBounds();
            }
        }
        private void MfcHostContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateControlBounds();
        }

        private void UpdateControlBounds()
        {
            if (_hMfcStatic == IntPtr.Zero) return;

            // Transform the placeholder container bounds relative to the main Window
            var transform = MfcHostContainer.TransformToVisual(null);
            var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

            // Get Scale Factor (DPI)
            double scale = MfcHostContainer.XamlRoot?.RasterizationScale ?? 1.0;

            // Convert logical XAML coordinates to physical pixels for Win32 API
            int x = (int)(point.X * scale);
            int y = (int)(point.Y * scale);
            int width = (int)(MfcHostContainer.ActualWidth * scale);
            int height = (int)(MfcHostContainer.ActualHeight * scale);

            // Reposition the CStatic over the XAML placeholder
            NativeMethods.SetWindowPos(_hMfcStatic, HWND_TOP, x, y, width, height, SWP_SHOWWINDOW);
            NativeMethods.ResizeMfcDialog(_hMfcStatic, width, height);
        }
    }
}
