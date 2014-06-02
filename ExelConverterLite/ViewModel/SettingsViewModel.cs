using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight;
using System.IO;

using ExelConverter.Core.Settings;
using System.Reflection;
using System.Windows;
using ExelConverterLite.View;
using ExelConverter.Core.DataAccess;
using System.Data.EntityClient;

namespace ExelConverterLite.ViewModel
{
    class ConnectionStringData
    {
        public string Server { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string DBName { get; set; }
    }

    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            Settings = ExelConverter.Core.Settings.SettingsProvider.CurrentSettings;
            SelectCsvPathCommand = new RelayCommand(SelectCsvPath);
        }

        private SettingsObject _settings;
        public SettingsObject Settings
        {
            get { return _settings; }
            set
            {
                if (_settings != value)
                {
                    _settings = value;
                    RaisePropertyChanged("Settings");
                }
            }
        }

        public RelayCommand SelectCsvPathCommand { get; private set; }
        private void SelectCsvPath()
        {
            Settings.CsvFilesDirectory = Model.ExelConverterDirectoryDialog.Show();
        }

        private static string ProvConStr = "provider connection string".ToLower();
    }
}
