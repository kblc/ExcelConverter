using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverterLite.View
{
    public static class ViewLocator
    {
        #region Views

        private static LogInView _logInView;
        public static LogInView LogInView
        {
            get
            {
                if (_logInView == null)
                {
                    _logInView = new LogInView();
                }
                return _logInView;
            }
        }

        private static ImportView _importView;
        public static ImportView ImportView
        {
            get
            {
                if (_importView == null)
                {
                    _importView = new ImportView();
                }
                return _importView;
            }
        }

        private static MenuView _menuView;
        public static MenuView MenuView
        {
            get
            {
                if (_menuView == null)
                {
                    _menuView = new MenuView();
                }
                return _menuView;
            }
        }

        private static OptionsView _optionsView;
        public static OptionsView OptionsView
        {
            get
            {
                if (_optionsView == null)
                {
                    _optionsView = new OptionsView();
                }
                return _optionsView;
            }
        }

        private static AddOperatorView _addOperatorView;
        public static AddOperatorView AddOperatorView
        {
            get
            {
                if (_addOperatorView == null)
                {
                    _addOperatorView = new AddOperatorView();
                }
                return _addOperatorView;
            }
        }

        private static SettingsView _settingsView;
        public static SettingsView SettingsView
        {
            get
            {
                if (_settingsView == null)
                {
                    _settingsView = new SettingsView();
                }
                return _settingsView;
            }
        }


        private static MessageView _messageView;
        public static MessageView MessageView
        {
            get
            {
                if (_messageView == null)
                {
                    _messageView = new MessageView();
                }
                return _messageView;
            }
        }

        private static OperatorSettingsView _operatorSettingsView;
        public static OperatorSettingsView OperatorSettingsView
        {
            get
            {
                if (_operatorSettingsView == null || _operatorSettingsView.IsWindowClosed)
                {
                    _operatorSettingsView = new OperatorSettingsView();
                }
                return _operatorSettingsView;
            }
        }

        private static ExportView _exportView;
        public static ExportView ExportView
        {
            get
            {
                if (_exportView == null || _exportView.IsClosed) 
                {
                    //App.Locator.Export = null;
                    var export = App.Locator.Export;
                    export.Initialize();
                    _exportView = new ExportView();
                    _exportView.Closing += (s, e) => { export.Closing(); };
                }
                return _exportView;
            }
        }

        private static ExportSetupView _exportSetupView;
        public static ExportSetupView ExportSetupView
        {
            get
            {
                if (_exportSetupView == null || _exportSetupView.IsClosed)
                {
                    _exportSetupView = new ExportSetupView();
                }
                return _exportSetupView;
            }
        }

        private static ExportLogView _exportLogView;
        public static ExportLogView ExportLogView
        {
            get
            {
                if (_exportLogView == null)
                {
                    _exportLogView = new ExportLogView();
                }
                return _exportLogView;
            }
        }

        private static ImageParsingView _imageParsingView;
        public static ImageParsingView ImageParsingView
        {
            get 
            {
                if (_imageParsingView == null || _imageParsingView.IsClosed)
                {
                    _imageParsingView = new ImageParsingView();
                }
                return _imageParsingView;
            }
        }

        private static LoadImageView _loadImageView;
        public static LoadImageView LoadImageView
        {
            get
            {
                if (_loadImageView == null || _loadImageView.IsClosed)
                {
                    _loadImageView = new LoadImageView();
                }
                return _loadImageView;
            }
        }

        #endregion

    }
}
