using System.Windows;
using ExelConverterLite.ViewModel;
using ExelConverter.Core.Settings;
using ExelConverter.Core.DataAccess;

namespace ExelConverterLite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) =>
                {
                    ViewModelLocator.Cleanup();
                };
        }

        private void Window_Closed_1(object sender, System.EventArgs e)
        {
            if (SettingsProvider.DataBasesEnabled)
            {
                App.Locator.Import.SaveRules(false);
                App.Locator.Settings.Settings.SaveToRegistry();
            }
        }
    }
}