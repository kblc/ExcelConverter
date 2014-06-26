using ExelConverter.Core.DataAccess;
using ExelConverter.Core.Settings;
using ExelConverterLite.Utilities;
using ExelConverterLite.View;
using ExelConverterLite.ViewModel;
using GalaSoft.MvvmLight.Threading;
using Helpers;
using System;
using System.Threading;
using System.Windows;

namespace ExelConverterLite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            if (HttpDataClient.Default != null && HttpDataClient.Default.IsWebLogined)
                HttpDataClient.Default.WebLogout();
            base.OnExit(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            ExelConverter.Core.Settings.SettingsProvider.Login = new LoginClass();
            bool logined = ExelConverter.Core.Settings.SettingsProvider.Login.LogIn();

            if (!logined)
            {
                Shutdown();
            }
            else
            {
                SettingsProvider.Initialize(new DataAccess());
                SettingsProvider.IniializeSettings();
                MainWindow mv = new MainWindow();

                Locator.Import.DatabaseName = ExelConverterLite.Properties.Settings.Default.LastConnected;

                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                Current.MainWindow = mv;
                mv.Show();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            Log.LogFileName = ExelConverterLite.Properties.Settings.Default.LogFilename;
            Log.Clear();   
            Log.Add("Application is starting up...");

            base.OnStartup(e);
        }

        static App()
        {
            DispatcherHelper.Initialize();
        }

        public static ViewModelLocator Locator
        {
            get
            {
                return (ViewModelLocator)App.Current.FindResource("Locator");
            }
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled thread {0}", e.Exception.GetExceptionText());
            Log.Add(errorMessage);
            errorMessage += Environment.NewLine + "Вы хотите продолжить выполнение программы?";
            if (MessageBox.Show(errorMessage, "Exception", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                Shutdown();
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled {0}", e.Exception.GetExceptionText());
            Log.Add(errorMessage);
            e.Handled = true;
            errorMessage += Environment.NewLine + "Вы хотите продолжить выполнение программы?";
            if (MessageBox.Show(errorMessage, "Exception", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                Shutdown();
        }
    }
}
