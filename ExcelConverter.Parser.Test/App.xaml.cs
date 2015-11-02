using Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ExcelConverter.Parser.Test
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            //System.Windows.Forms.Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            Log.LogFileName = "log.txt";
            Log.Clear();
            Log.Add("Application is starting up...");

            //App.Current.MainWindow = new MainWindow();

            //App.Current.MainWindow.Show();

            App.Current.Exit += (s,e2) => { Process.GetCurrentProcess().Kill(); };

            base.OnStartup(e);
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = "App.OnDispatcherUnhandledException() :: An unhandled " + e.Exception.GetExceptionText();
            Helpers.Old.Log.Add(errorMessage);
            e.Handled = true;

            if (MessageBox.Show(errorMessage + string.Format("{0}{0}Do you want to continue?", Environment.NewLine), "Exception", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.No)
                Shutdown();
        }
    }
}
