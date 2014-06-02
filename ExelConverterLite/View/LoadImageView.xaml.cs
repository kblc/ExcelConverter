using ExelConverterLite.Model;
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
    /// Interaction logic for LoadImageView.xaml
    /// </summary>
    public partial class LoadImageView : Window
    {
        public LoadImageView()
        {
            InitializeComponent();
            Closing += (s, e) => { IsClosed = true; };
        }

        public string Mode { get; set; }

        public bool IsClosed { get; private set; }

        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            url.Text = ImageLoadFileDialog.Show(); 
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
