using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MainApplication
{

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        struct NavigationItem
        {
            public string Tag;
            public Type PageType;
            public NavigationItem(string tag, Type pageType)
            {
                Tag = tag;
                PageType = pageType;
            }
        }

        List<NavigationItem> navigationItems = new List<NavigationItem>
        {
            new NavigationItem("Direct2DPage", typeof(Direct2DPage)),
            new NavigationItem("OtherPage", typeof(OtherPage))
        };

        public MainWindow()
        {
            InitializeComponent();
            foreach (var item in navigationItems)
            {
                navView.MenuItems.Add(item.Tag);
            }
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args != null) 
            {
                string? tag = args.InvokedItem as string;
                if (tag == null) return;
                NavigationItem selectedItem = navigationItems.FirstOrDefault(item => item.Tag == tag);
                Type type = selectedItem.PageType;

                MainFrame.Navigate(type);
                MainFrame.BackStack.Clear();
                MainFrame.ForwardStack.Clear();
            }
        }
    }
}
