using ExelConverter.Core.DataAccess;
using ExelConverterLite.ViewModel;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Helpers;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace ExelConverterLite.View
{
    public class LoginClass : ExelConverter.Core.Settings.ILogIn
    {
        private bool authenticated = false;
        bool ExelConverter.Core.Settings.ILogIn.IsLogined
        {
            get
            {
                return authenticated;
            }
        }
        bool ExelConverter.Core.Settings.ILogIn.LogIn()
        {
            if (authenticated)
                return authenticated;

            if (this.IsDesignMode())
            {
                authenticated = true;
            } else
            {
                LogInView lv = new LogInView();
                var res = lv.ShowDialog();
                authenticated = (res == null ? false : res.Value);
                if (authenticated)
                {
                    lv.Client.SetAsDefault();
                    alphaEntities.ProviderConnectionString = lv.Client.ConnectionStringMain;
                    exelconverterEntities2.ProviderConnectionString = lv.Client.ConnectionStringApp;
                }
            }
            return authenticated;
        }
    }

    /// <summary>
    /// Interaction logic for LogInView.xaml
    /// </summary>
    public partial class LogInView : Window, INotifyPropertyChanged
    {
        public class NameValueItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public readonly HttpDataClient Client = new HttpDataClient();

        public LogInView()
        {
            Servers = new ObservableCollection<NameValueItem>();
            string serverItems = ExelConverterLite.Properties.Settings.Default.HttpServersName;
            foreach (var namevalueitem in serverItems.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Servers.Add(
                        new NameValueItem()
                        {
                            Name = namevalueitem.Substring(0, namevalueitem.IndexOf(':')),
                            Value = namevalueitem.Substring(namevalueitem.IndexOf(':') + 1)
                        }
                    );
            }
            InitializeComponent();
            serverComboBox.ItemsSource = Servers;
            var selItem = Servers.FirstOrDefault(i => i.Name == ExelConverterLite.Properties.Settings.Default.LastConnected);
            if (selItem == null)
                selItem = Servers.FirstOrDefault();
            serverComboBox.SelectedItem = selItem;
        }

        private bool isLoginButtonEnabled = true;
        public bool IsLoginButtonEnabled
        {
            get
            {
                return isLoginButtonEnabled;
            }
            set
            {
                if (isLoginButtonEnabled == value)
                    return;
                isLoginButtonEnabled = value;
                RaisePropertyChanged("IsLoginButtonEnabled");
            }
        }

        public string LoginStateText 
        {
            get
            {
                return stateTextBlock.Text;
            }
            set
            {
                if (stateTextBlock.Text != value)
                    stateTextBlock.Text = value;
            }
        }

        private ObservableCollection<NameValueItem> Servers { get; set; }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

            bw.DoWork += (s, e2) =>
                {
                    var bgrndW = ((BackgroundWorker)s);

                    dynamic param = e2.Argument;
                    var client = ((HttpDataClient)param.Client);

                    bgrndW.ReportProgress(0, "Проверка связки логин/пароль...");
                    client.Login(param.Server, param.UserLogin, param.UserPassword);

                    if (!client.HasConnections)
                        throw new Exception("В ответе сервера отсутсвуют необходимые пераметры подключения.");
                    
                    bgrndW.ReportProgress(33, "Проверка подключения к основной БД...");
                    using (var conn0 = MySql.Data.MySqlClient.MySqlClientFactory.Instance.CreateConnection())
                    {
                        conn0.ConnectionString = client.ConnectionStringMain;
                        conn0.Open();
                    }

                    bgrndW.ReportProgress(66, "Проверка подключения к БД приложения...");
                    using (var conn1 = MySql.Data.MySqlClient.MySqlClientFactory.Instance.CreateConnection())
                    {
                        conn1.ConnectionString = client.ConnectionStringApp;
                        conn1.Open();
                    }

                    bgrndW.ReportProgress(100, "Все проверки завершены...");

                    Thread.Sleep(1000);

                    e2.Result = true;
                };

            bw.RunWorkerCompleted += (s, e2) =>
                {
                    try
                    {
                        if (e2.Error != null)
                            throw e2.Error;
                        this.DialogResult = true;
                        ExelConverterLite.Properties.Settings.Default.LastConnected = ((NameValueItem)serverComboBox.SelectedItem).Name;
                        ExelConverterLite.Properties.Settings.Default.Save();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show(string.Format("Ошибка входа в систему:{0}{1}", Environment.NewLine, ex.GetExceptionText()), "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        LoginStateText = string.Empty;
                        IsLoginButtonEnabled = true;
                    }
                };
            bw.ProgressChanged += (s, e2) =>
                {
                    LoginStateText = string.Format("Идёт проверка ({0}%): {1}", e2.ProgressPercentage, e2.UserState.ToString());
                };

            IsLoginButtonEnabled = false;
            bw.RunWorkerAsync(
                new 
                { 
                    Client = this.Client,
                    Server = ((NameValueItem)serverComboBox.SelectedItem).Value,
                    UserLogin = usernameBox.Text,
                    UserPassword = passwordBox.Password
                });
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
