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
            //string testErr = string.Empty;
            //try
            //{
            //    HttpDataClient test = new HttpDataClient(true);
            //    test.Login(@"http://alpha.outdoor-online.com.ua/", "ad", "ad");

            //    //test.WebLogin();

            //    test.UserPassword = test.UserPassword+"2";

            //    test.AddFillRectangle(new ExelConverter.Core.DataObjects.FillArea() { FKOperatorID = 1, Height = 100, ID = 0, Type = "test_type", Width = 100, X1 = 0, X2 = 100, Y1 = 0, Y2 = 100 });

            //    string map, pdf;
            //    test.GetResourcesList(
            //        72,
            //        new System.Collections.Generic.List<ExelConverter.Core.DataWriter.ReExportData>(new ExelConverter.Core.DataWriter.ReExportData[] { new ExelConverter.Core.DataWriter.ReExportData("123123") })
            //        , out map
            //        , out pdf);

            //    test.RemoveFillRectangle(100);

            //    test.UploadFileToQueue(new HttpDataAccessQueueParameters() { FilePath = @"D:\test.csv", Activate = false, CoordinatesApproved = false, OperatorID = 1, Timeout = 100, UseQueue = true });

            //    test.WebLogout();
            //}
            //catch (Exception ex)
            //{
            //    testErr = ex.GetExceptionText();
            //}

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
