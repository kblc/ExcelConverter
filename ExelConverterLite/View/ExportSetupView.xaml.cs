using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for OperatorSettingsView.xaml
    /// </summary>
    public partial class ExportSetupView : Window
    {
        public bool IsClosed { get; set; }

        public ExportSetupView()
        {
            InitializeComponent();
            Closed += (s, e) => IsClosed = true;
        }

        public bool? ShowDialog(Window owner)
        {
            IsClosed = false;
            Owner = owner;
            return ShowDialog();
        }
    }
}
