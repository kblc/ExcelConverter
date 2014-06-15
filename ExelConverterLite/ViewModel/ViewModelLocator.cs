using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using ExelConverter.Core.DataAccess;

namespace ExelConverterLite.ViewModel
{

    public class ViewModelLocator
    {
        private MainWindowViewModel _mainWindow;
        public MainWindowViewModel MainWindow
        {
            get
            {
                if (_mainWindow == null)
                {
                    _mainWindow = new MainWindowViewModel();
                }
                return _mainWindow;
            }
        }

        private ImportViewModel _import;
        public ImportViewModel Import
        {
            get
            {
                if(_import == null)
                {
                    _import = new ImportViewModel(new DataAccess());
                }
                return _import;
            }
        }

        private MenuViewModel _menu;
        public MenuViewModel Menu
        {
            get
            {
                if (_menu == null)
                {
                    _menu = new MenuViewModel();
                }
                return _menu;
            }
        }

        private OptionsViewModel _options;
        public OptionsViewModel Options
        {
            get
            {
                if (_options == null)
                {
                    _options = new OptionsViewModel();
                }
                return _options;
            }
        }

        private AddOperatorViewModel _addOperator;
        public AddOperatorViewModel AddOperator
        {
            get 
            {
                if (_addOperator == null)
                {
                    _addOperator = new AddOperatorViewModel(new DataAccess());
                }
                return _addOperator; 
            }
            
        }

        private SettingsViewModel _settings;
        public SettingsViewModel Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new SettingsViewModel();
                }
                return _settings;
            }
        }

        private MessageViewModel _message;
        public MessageViewModel Message
        {
            get
            {
                if (_message == null)
                {
                    _message = new MessageViewModel();
                }
                return _message;
            }
        }

        private OperatorSettingsViewModel _operatorSettings;
        public OperatorSettingsViewModel OperatorSettings
        {
            get
            {
                return new OperatorSettingsViewModel(new DataAccess());
            }
        }

        private ExportViewModel _export;
        public ExportViewModel Export
        {
            get
            {
                if (_export == null)
                {
                    _export = new ExportViewModel();
                    //_export.Initialize();
                }
                return _export;
            }
            //set
            //{
            //    _export = value;
            //}
        }

        private ExportSetupViewModel _exportSetup;
        public ExportSetupViewModel ExportSetup
        {
            get
            {
                return new ExportSetupViewModel();
            }
        }

        private ExportLogViewModel _exportLog;
        public ExportLogViewModel ExportLog
        {
            get
            {
                if (_exportLog == null)
                {
                    _exportLog = new ExportLogViewModel();
                }
                return _exportLog;
            }
        }

        private ReExportProgressViewModel _reExportProgress;
        public ReExportProgressViewModel ReExportProgress
        {
            get
            {
                if (_reExportProgress == null)
                {
                    _reExportProgress = new ReExportProgressViewModel();
                }
                return _reExportProgress;
            }
        }

        public static void Cleanup()
        {

        }
    }
}