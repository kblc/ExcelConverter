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
    /// Interaction logic for AddOperatorView.xaml
    /// </summary>
    public partial class AddOperatorView : Window
    {
        public AddOperatorView()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            Closing += (s, e) => { e.Cancel = true; Visibility = System.Windows.Visibility.Hidden; };
        }
    }
}
