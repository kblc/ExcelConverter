using GalaSoft.MvvmLight;
//using ExelConverterLite.Model;
using System.Windows.Controls;

using ExelConverterLite.View;


namespace ExelConverterLite.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            Content = ViewLocator.MenuView;
        }


#region Binding Properties

        private UserControl _content;
        public UserControl Content
        {
            get { return _content; }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    RaisePropertyChanged("Content");
                }
            }
        }

#endregion

    }
}