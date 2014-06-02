using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using ExelConverter.Core.Settings;

namespace ExelConverterLite.ViewModel
{
    public sealed class MenuViewModel : ViewModelBase
    {
        public MenuViewModel()
        {
            //if (SettingsProvider.DataBasesEnabled)
            //{
                ImportControl = View.ViewLocator.ImportView;
                ExportLogControl = View.ViewLocator.ExportLogView;
            //}
            //else
            //{
            //    ImportControl = View.ViewLocator.MessageView;
            //    App.Locator.Message.Message = "При подключении к базе данных возникла ошибка." + Environment.NewLine + SettingsProvider.DataBaseError;
            //}
            SettingsControl = View.ViewLocator.SettingsView;
        }

        private void SelectionChanged()
        {
            

        }

        #region Properties

        

        private UserControl _importControl;
        public UserControl ImportControl
        {
            get { return _importControl; }
            set
            {
                if (_importControl != value)
                {
                    _importControl = value;
                    RaisePropertyChanged("ImportControl");
                }
            }
        }

        private UserControl _settingsControl;
        public UserControl SettingsControl
        {
            get { return _settingsControl; }
            set
            {
                if (_settingsControl != value)
                {
                    _settingsControl = value;
                    RaisePropertyChanged("SettingsControl");
                }
            }
        }

        private UserControl _exportLogControl;
        public UserControl ExportLogControl
        {
            get { return _exportLogControl; }
            set
            {
                if (_exportLogControl != value)
                {
                    _exportLogControl = value;
                    RaisePropertyChanged("ExportLogControl");
                }
            }
        }
        #endregion
    }
}
