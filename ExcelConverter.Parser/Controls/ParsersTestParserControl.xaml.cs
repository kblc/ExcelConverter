using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExcelConverter.Parser.Controls
{
    /// <summary>
    /// Логика взаимодействия для ParsersTestParserControl.xaml
    /// </summary>
    public partial class ParsersTestParserControl : UserControl, INotifyPropertyChanged
    {
        static ParsersTestParserControl()
        {
            ParsersProperty = ParsersListControl.ParsersProperty.AddOwner(typeof(ParsersTestParserControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        }

        public static readonly DependencyProperty ParserProperty = DependencyProperty.Register("Parser", typeof(Parser), typeof(ParsersTestParserControl), new FrameworkPropertyMetadata(new PropertyChangedCallback(MyCustom_PropertyChanged)));
        //public static readonly DependencyProperty ParsersProperty = DependencyProperty.Register("Parsers", typeof(ParserCollection), typeof(ParsersTestParserControl));
        public static readonly DependencyProperty ParsersProperty;
        public static readonly DependencyProperty IsSingleParserProperty = DependencyProperty.Register("IsSingleParser", typeof(bool), typeof(ParsersTestParserControl));

        public Parser Parser
        {
            get
            {
                return this.GetValue(ParserProperty) as Parser;
            }
            set
            {
                this.SetValue(ParserProperty, value);
            }
        }

        private static void MyCustom_PropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ParserProperty)
            {
                ParsersTestParserControl p = dobj as ParsersTestParserControl;
                if (p != null)
                {
                    p.RaisePropertyChanged("IsSingleParser");
                    p.RaisePropertyChanged("IsSingleParserCheckBoxVisible");
                }
            }
        }

        public bool IsSingleParserCheckBoxVisible
        {
            get
            {
                return Parser != null;
            }
        }

        public ParserCollection Parsers
        {
            get
            {
                return this.GetValue(ParsersProperty) as ParserCollection;
            }
            set
            {
                this.SetValue(ParsersProperty, value);
            }
        }

        public bool IsSingleParser
        {
            get
            {
                return ((bool)(this.GetValue(IsSingleParserProperty) ?? false) && Parser != null);
            }
            set
            {
                this.SetValue(IsSingleParserProperty, value);
            }
        }

        private string testResult = string.Empty;
        public string TestResult
        {
            get
            {
                return testResult;
            }
            private set
            {
                testResult = value;
                RaisePropertyChanged("TestResult");
            }
        }

        private DelegateCommand parserTestUrl = null;
        public ICommand ParserTestUrl
        {
            get
            {
                return parserTestUrl ?? (parserTestUrl = new DelegateCommand((o) =>
                {
                    string urlToParse = o as string;

                    bwTesting = new BackgroundWorker();
                    bwTesting.DoWork += (s, e) =>
                    {
                        TestingParams param = e.Argument as TestingParams;

                        string[] urls = param.Url.Split(new char[] { ';' });
                        ParseResult[] res = param.IsSingleParser ? param.Parser.Parse(urls) : param.Parsers.Parse(urls);
                        e.Result = res;
                        if (res.Length != urls.Length)
                            throw new Exception(string.Format("Количество входных URL ({0}) не равно количеству результатов ({1})", urls.Length, res.Length));
                    };
                    bwTesting.RunWorkerCompleted += (s, e) =>
                    {
                        TestResult = e.Error == null ? "[" + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss") + "] Тест завершен успешно." : e.Error.Message;
                        IsEnableTesting = true;
                        ParseResults.Clear();
                        try
                        {
                            ParseResult[] res = e.Result as ParseResult[];
                            if (res != null)
                            {
                                if (res != null)
                                    foreach (var resItem in res)
                                        foreach (var item in resItem.Data)
                                            ParseResults.Add(item);
                                RaisePropertyChanged("ParseResults");
                            }
                        }
                        catch { }
                        bwTesting.Dispose();
                    };
                    TestResult = "[" + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss") + "] Парсинг начат...";
                    IsEnableTesting = false;
                    bwTesting.RunWorkerAsync(new TestingParams() { Url = urlToParse, Parser = this.Parser, IsSingleParser = this.IsSingleParser, Parsers = this.Parsers });
                }));
            }
        }

        internal class TestingParams
        {
            public Parser Parser { get; set; }
            public ParserCollection Parsers { get; set; }
            public bool IsSingleParser { get; set; }
            public string Url { get; set; }
        }
        private BackgroundWorker bwTesting = null;
        private bool isEnableTesting = true;
        public bool IsEnableTesting
        {
            get
            {
                return isEnableTesting;
            }
            private set
            {
                isEnableTesting = value;
                RaisePropertyChanged("IsEnableTesting");
            }
        }

        private ObservableCollection<KeyValuePair<string, string>> parseResults = new ObservableCollection<KeyValuePair<string, string>>();
        public ObservableCollection<KeyValuePair<string, string>> ParseResults
        {
            get
            {
                return parseResults;
            }
        }

        public ParsersTestParserControl()
        {
            InitializeComponent();
        }

        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion;

    }
}
