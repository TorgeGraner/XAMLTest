using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualBasic;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using static ABI.System.Collections.Generic.IList_Delegates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MainApplication
{
    public class DataRow : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isGroupHovered;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public int Level { get; set; } = 0; // 0 = Parent, 1 = Child

        // Link to parent row for children, or self reference for parent
        public DataRow ParentGroupRow { get; set; }

        public ObservableCollection<DataRow> Children { get; set; } = new();

        // Background color updates dynamically based on hover state
        public Brush RowBackground => IsGroupHovered
            ? new SolidColorBrush(Colors.LightBlue)
            : new SolidColorBrush(Colors.Transparent);

        public bool IsGroupHovered
        {
            get => _isGroupHovered;
            set
            {
                if (_isGroupHovered != value)
                {
                    _isGroupHovered = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RowBackground));
                }
            }
        }

        // Chevron & Spacer Visibilities
        public Visibility ChevronVisibility => (Children != null && Children.Count > 0)
            ? Visibility.Visible
            : Visibility.Collapsed;

        public Visibility SpacerVisibility => (Children == null || Children.Count == 0)
            ? Visibility.Visible
            : Visibility.Collapsed;

        public string ChevronIcon => IsExpanded ? "\uE70D" : "\uE76C";
        public Thickness IndentMargin => new Thickness(Level * 20, 0, 0, 0);

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ChevronIcon));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public sealed partial class OtherPage : Page
    {
        public ObservableCollection<DataRow> VisibleRows { get; set; } = new();
        public OtherPage()
        {
            this.InitializeComponent();
            
            LoadData();
        }
        private void LoadData()
        {
            var laptopParent1 = new DataRow
            {
                Id = 1,
                Name = "Laptops",
                Category = "Hardware",
                Price = 3500.00m,
                Level = 0,
                Children =
                {
                }
            };

            var laptopParent = new DataRow
            {
                Id = 1,
                Name = "Laptops",
                Category = "Hardware",
                Price = 3500.00m,
                Level = 0,
                Children =
                {
                    new DataRow { Id = 101, Name = "XPS 15 Laptop", Category = "Laptops", Price = 1500.00m, Level = 1 },
                    new DataRow { Id = 102, Name = "MacBook Pro 16", Category = "Laptops", Price = 2000.00m, Level = 1 }
                }
            };

            var laptopParent2 = new DataRow
            {
                Id = 1,
                Name = "Laptops",
                Category = "Hardware",
                Price = 3500.00m,
                Level = 0,
                Children =
                {
                }
            };

            var pcParent = new DataRow
            {
                Id = 2,
                Name = "Desktop PCs",
                Category = "Hardware",
                Price = 3299.00m,
                Level = 0,
                Children =
                {
                    new DataRow { Id = 201, Name = "Custom Gaming Rig", Category = "PCs", Price = 2500.00m, Level = 1 },
                    new DataRow { Id = 202, Name = "Mac Mini", Category = "PCs", Price = 799.00m, Level = 1 }
                }
            };
            var laptopParent3 = new DataRow
            {
                Id = 1,
                Name = "Laptops",
                Category = "Hardware",
                Price = 3500.00m,
                Level = 0,
                Children =
                {
                }
            };

            VisibleRows.Add(laptopParent1);
            VisibleRows.Add(laptopParent);
            VisibleRows.Add(laptopParent2);
            VisibleRows.Add(pcParent);
            VisibleRows.Add(laptopParent3);
        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var parentRow = (DataRow)button.DataContext;

            if (parentRow == null) return;

            // Execute collection modifications on the DispatcherQueue to prevent DataGrid index sync crashes
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (parentRow.IsExpanded)
                {
                    CollapseRow(parentRow);
                }
                else
                {
                    ExpandRow(parentRow);
                }
            });
        }

        private void ExpandRow(DataRow parentRow)
        {
            int parentIndex = VisibleRows.IndexOf(parentRow);
            if (parentIndex == -1) return;

            // Insert direct children right below parent
            for (int i = 0; i < parentRow.Children.Count; i++)
            {
                VisibleRows.Insert(parentIndex + 1 + i, parentRow.Children[i]);
            }

            parentRow.IsExpanded = true;
        }

        private void CollapseRow(DataRow parentRow)
        {
            // Recursively collapse sub-children first to clean up nested levels safely
            foreach (var child in parentRow.Children)
            {
                if (child.IsExpanded)
                {
                    CollapseRow(child);
                }

                // Direct object removal avoids index mismatch errors with items below
                VisibleRows.Remove(child);
            }

            parentRow.IsExpanded = false;
        }
    }
}
