using ExelConverter.Core.DataWriter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ExelConverterLite.View
{
    /// <summary>
    /// Interaction logic for ExportWindowView.xaml
    /// </summary>
    public partial class ExportView : Window
    {
        public bool IsClosed { get; private set; }

        public ExportView()
        {
            InitializeComponent();
            Closing += (s, e) => IsClosed = true;
        }

        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }

        public void ScrollToRow(int index)
        {
            dg.SelectedItems.Clear();
            dg.ScrollIntoView(dg.Items[index]);
            var row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
            row.IsSelected = true;
        }
    }
}
