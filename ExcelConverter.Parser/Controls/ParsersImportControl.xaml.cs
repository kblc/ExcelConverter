using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
    public class StringWrapper : INotifyPropertyChanged
    {
        public StringWrapper() { }
        public StringWrapper(string value) { Value = value; }

        private string value = string.Empty;
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                RaisePropertyChanged("Value");
            }
        }

        private bool isItemEnabled = true;
        internal bool IsItemEnabled
        {
            get { return isItemEnabled; }
            set
            {
                isItemEnabled = value;
                RaisePropertyChanged("IsItemEnabled");
            }
        }

        #region INotifyPropertyChanged

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion;
    }

    public class StringUrlWithResultWrapper : StringWrapper
    {
        public StringUrlWithResultWrapper() : base() { }
        public StringUrlWithResultWrapper(string value) : base(value) { }

        public bool IsFinished
        {
            get { return FinishResult > 0; }
        }

        private byte finishResult = 0;
        public byte FinishResult
        {
            get { return finishResult; }
            set { finishResult = value; RaisePropertyChanged("FinishResult"); RaisePropertyChanged("IsFinished"); }
        }

        private ParseResult result = new ParseResult();
        public ParseResult Result
        {
            get
            {
                return result;
            }
            set
            {
                result.Url = value == null ? string.Empty : value.Url;
                if (value != null)
                    result.Data = value.Data;
                else
                    result.Data.Clear();
                RaisePropertyChanged("Result");
            }
        }
    }

    public class StringParserWrapper : StringWrapper
    {
        private Parser parser = null;
        public Parser Parser
        {
            get
            {
                return parser;
            }
            set
            {
                if (parser != null)
                    parser.PropertyChanged -= parser_PropertyChanged;

                parser = value;
                RaisePropertyChanged("Parser");

                if (parser != null)
                    parser.PropertyChanged += parser_PropertyChanged;

                ReCalcParser();
            }
        }

        public ParserCollection Parsers { get; set; }

        private void parser_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Url")
            {
                ReCalcParser();
            }
        }

        private void ReCalcParser()
        {
            Labels.Clear();
            if (Parser != null && Parsers != null)
            {
                SimilarParser = Parsers.Parsers.Where(p => Helper.StringLikes(p.Url, parser.Url) || Helper.StringLikes(parser.Url, p.Url)).FirstOrDefault();
            }
        }

        private ObservableCollection<string> labels = null;
        public ObservableCollection<string> Labels
        {
            get 
            {
                return labels ?? (labels = new ObservableCollection<string>());
            }
        }

        private Parser similarParser = null;
        public Parser SimilarParser 
        {
            get 
            {
                return similarParser;
            } 
            private set 
            {
                similarParser = value;
                RaisePropertyChanged("SimilarParser"); 
                RaisePropertyChanged("SimilarParserExists");
                RaisePropertyChanged("SimilarParserRules");
            }
        }

        public bool SimilarParserExists { get { return SimilarParser != null; } }

        public string SimilarParserRules
        {
            get
            {
                string result = string.Empty;
                if (SimilarParser != null)
                    foreach (var r in SimilarParser.Rules)
                        result += (string.IsNullOrWhiteSpace(result) ? string.Empty : ",") + "\'" + r.Label + "\'";
                return result;
            }
        }

        private UrlCollection urls = new UrlCollection();
        public UrlCollection Urls
        {
            get { return urls; }
            set
            {
                urls.Clear();
                if (value != null)
                    foreach (var i in value)
                        urls.Add(i);
            }
        }
    }

    public class UrlCollection : ObservableCollection<StringUrlWithResultWrapper>
    {
        public UrlCollection() { }
        public UrlCollection(IEnumerable<string> items)
        {
            if (items != null)
                foreach (string item in items)
                    Add(new StringUrlWithResultWrapper() { Value = item });
        }
    }

    public class HostsCollection : ObservableCollection<StringParserWrapper> { }

    public partial class ParsersImportControl : UserControl, INotifyPropertyChanged
    {
        public ParsersImportControl()
        {
            Hosts = new HostsCollection();
            Urls = new UrlCollection();
            InitializeComponent();
        }

        #region Properties

        internal const bool AllowFileImportDefaultValue = true;
        internal const bool AllowFileExportDefaultValue = true;
        internal const bool AllowClearUrlsAfterImportDefaultValue = true;
        internal const byte ThreadCountDefaultValue = byte.MinValue;

        public static readonly DependencyProperty ParsersProperty = DependencyProperty.Register("Parsers", typeof(ParserCollection), typeof(ParsersImportControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty UrlsProperty = DependencyProperty.Register("Urls", typeof(UrlCollection), typeof(ParsersImportControl), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty HostsProperty = DependencyProperty.Register("Hosts", typeof(HostsCollection), typeof(ParsersImportControl), new FrameworkPropertyMetadata(new HostsCollection(), new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowFileImportProperty = DependencyProperty.Register("AllowFileImport", typeof(bool), typeof(ParsersImportControl), new FrameworkPropertyMetadata(AllowFileImportDefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowFileExportProperty = DependencyProperty.Register("AllowFileExport", typeof(bool), typeof(ParsersImportControl), new FrameworkPropertyMetadata(AllowFileExportDefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty ThreadCountProperty = DependencyProperty.Register("ThreadCount", typeof(byte), typeof(ParsersImportControl), new FrameworkPropertyMetadata(ThreadCountDefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportAllProperty = DependencyProperty.Register("AllowImportAll", typeof(bool), typeof(ParsersImportControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportPhotoProperty = DependencyProperty.Register("AllowImportPhoto", typeof(bool), typeof(ParsersImportControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportSchemaProperty = DependencyProperty.Register("AllowImportSchema", typeof(bool), typeof(ParsersImportControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowClearUrlsAfterImportProperty = DependencyProperty.Register("AllowClearUrlsAfterImport", typeof(bool), typeof(ParsersImportControl), new FrameworkPropertyMetadata(AllowClearUrlsAfterImportDefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        private static void MyCustom_PropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            ParsersImportControl p = dobj as ParsersImportControl;
            if (p != null)
            {
                p.RaisePropertyChanged(e.Property.Name);

                if (e.Property == UrlsProperty && e.NewValue != null)
                {
                    ((UrlCollection)e.NewValue).CollectionChanged -= p.Urls_CollectionChanged;
                    ((UrlCollection)e.NewValue).CollectionChanged += p.Urls_CollectionChanged;
                    p.ReCalcHosts();
                }
                if (e.Property == UrlsProperty && e.OldValue != null)
                {
                    ((UrlCollection)e.OldValue).CollectionChanged -= p.Urls_CollectionChanged;
                }
                if (e.Property == ParsersProperty)
                {
                    p.ReCalcHosts();
                }

                if (e.Property == UrlsProperty && e.NewValue == null)
                {
                    p.Urls = new UrlCollection();
                }
            }
        }
        public ParserCollection Parsers
        {
            get { return this.GetValue(ParsersProperty) as ParserCollection; }
            set { this.SetValue(ParsersProperty, value); }
        }
        public UrlCollection Urls
        {
            get { return this.GetValue(UrlsProperty) as UrlCollection; }
            set { this.SetValue(UrlsProperty, value); }
        }
        public HostsCollection Hosts
        {
            get { return this.GetValue(HostsProperty) as HostsCollection; }
            set { this.SetValue(HostsProperty, value); }
        }
        public bool AllowFileImport
        {
            get { return (bool)this.GetValue(AllowFileImportProperty); }
            set { this.SetValue(AllowFileImportProperty, value); }
        }
        public bool AllowFileExport
        {
            get { return (bool)this.GetValue(AllowFileExportProperty); }
            set { this.SetValue(AllowFileExportProperty, value); }
        }
        public byte ThreadCount
        {
            get { return (byte)this.GetValue(ThreadCountProperty); }
            set { this.SetValue(ThreadCountProperty, value); }
        }
        public bool AllowImportAll
        {
            get { return (bool)this.GetValue(AllowImportAllProperty); }
            set { this.SetValue(AllowImportAllProperty, value); }
        }
        public bool AllowImportPhoto
        {
            get { return (bool)this.GetValue(AllowImportPhotoProperty); }
            set { this.SetValue(AllowImportPhotoProperty, value); }
        }
        public bool AllowImportSchema
        {
            get { return (bool)this.GetValue(AllowImportSchemaProperty); }
            set { this.SetValue(AllowImportSchemaProperty, value); }
        }
        public bool AllowClearUrlsAfterImport
        {
            get { return (bool)this.GetValue(AllowClearUrlsAfterImportProperty); }
            set { this.SetValue(AllowClearUrlsAfterImportProperty, value); }
        }
        #endregion

        private string progressText = string.Empty;
        public string ProgressText
        {
            get { return progressText; }
            set { progressText = value; RaisePropertyChanged("ProgressText"); }
        }

        private bool isBusy = false;
        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
            set
            {
                isBusy = value;
                ReCalcHosts();
                RaisePropertyChanged("IsBusy");
            }
        }

        private string importFileName = string.Empty;
        public string ImportFileName
        {
            get
            {
                return importFileName;
            }
            set
            {
                importFileName = value;
                RaisePropertyChanged("ImportFileName");
            }
        }

        private void ReCalcHosts()
        {
            Hosts.Clear();
            if (!IsBusy && Parsers != null && Urls != null)
            {
                var hostDistinct = 
                    Urls
                    .Where( u => Helper.IsWellFormedUriString(u.Value, UriKind.Absolute) )
                    .Select(u => new Uri(u.Value).Host)
                    .Distinct()
                    .ToArray();
                //var hostToExclude = hostDistinct.Where(h1 => hostDistinct.Where(h2 => Helper.StringLikes(h1, Helper.GetDomainFromString(h2, true))).Count() > 1).ToArray();
                //var hostToAdd = hostToExclude.Select(h => ("*"+Helper.GetDomainFromString(h, true)).Replace("**","*") ).Distinct().ToArray();

                foreach (var sw in 
                    hostDistinct
                    //.Except(hostToExclude)
                    //.Concat(hostToAdd)
                    //.Distinct()
                    .OrderBy(i => i)
                    .Select(
                            host => new StringParserWrapper()
                                {
                                    Value = host,
                                    IsItemEnabled = true,
                                    Parsers = this.Parsers,
                                    Parser = new Parser() { Url = host, Labels = Parsers.Labels },
                                    Urls = new UrlCollection(Helper.GetSomeUrlsForHost(host, Urls, 10))
                                })
                            )
                    Hosts.Add(sw);
                


                //foreach (var sw in
                //        Urls
                //        .Select(
                //                u =>
                //                    Helper.GetDomainFromUrl(new Uri(u.Value), true)
                //    //new Uri(u.Value).GetLeftPart(UriPartial.Authority)
                //            )
                //            .Distinct()
                //            .Select(host => new StringParserWrapper()
                //                {
                //                    Value = host,
                //                    IsItemEnabled = true,//!Parsers.Parsers.Any(i2 => i2.Url == host),
                //                    Parser = new Parser() { Url = host, Labels = Parsers.Labels },
                //                    Urls = new UrlCollection(Helper.GetSomeUrlsForHost(host, Urls, 10))
                //                }))
                //    Hosts.Add(sw);
            }
        }

        public void Parse(string label = null)
        {
            ParseUrlsCommand.Execute(label);
        }

        private DelegateCommand selectFileCommand = null;
        public ICommand SelectFileCommand
        {
            get
            {
                return selectFileCommand ?? (selectFileCommand = new DelegateCommand(
                    (o) =>
                        {
                            OpenFileDialog ofd = new OpenFileDialog() 
                            {
                                Filter = "Tables files (*.xls,*.xlsx,*.csv)|*.xls;*.xlsx;*.csv|All files (*.*)|*.*",
                                RestoreDirectory = true
                            };
                            if (ofd.ShowDialog() ?? false)
                                ImportFileName = ofd.FileName;
                        }
                ));
            }
        }

        private DelegateCommand importFileCommand = null;
        public ICommand ImportFileCommand
        {
            get
            {
                return importFileCommand ?? (importFileCommand = new DelegateCommand(
                    (o) =>
                    {
                        ImportFile(ImportFileName);
                    }
                ));
            }
        }
        private DelegateCommand importFileAdditionalCommand = null;
        public ICommand ImportFileAdditionalCommand
        {
            get
            {
                return importFileAdditionalCommand ?? (importFileAdditionalCommand = new DelegateCommand(
                    (o) =>
                    {
                        ImportFile(ImportFileName, true);
                    }
                ));
            }
        }

        private BackgroundWorker bwImportFile = null;
        private void ImportFile(string fileName, bool additional = false)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                Urls.Clear();

                bwImportFile = new BackgroundWorker();
                bwImportFile.DoWork += (s, e) =>
                {
                    string cfileName = e.Argument as string;
                    string[] res =  Helper.GetLinksFromFile(cfileName, additional, true).OrderBy(i => i).ToArray();
                    e.Result = res;
                };
                bwImportFile.RunWorkerCompleted += (s, e) =>
                {
                    try
                    {
                        if (e.Error != null)
                            throw e.Error;

                        string[] res = (string[])e.Result;
                        foreach (var sw in res.Select(i => new StringUrlWithResultWrapper() { Value = i }))
                            Urls.Add(sw);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Ошибка при импорте файла:{0}{1}", Environment.NewLine, ex.Message), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsBusy = false;
                        bwImportFile.Dispose();
                        bwImportFile = null;
                    }
                };
                IsBusy = true;
                bwImportFile.RunWorkerAsync(fileName);
            }
        }

        private DelegateCommand deleteUrlCommand = null;
        public ICommand DeleteUrlCommand
        {
            get
            {
                return deleteUrlCommand ?? (deleteUrlCommand = new DelegateCommand(
                    (o) =>
                    {
                        string url = o as string;
                            foreach (var item in Urls.Where(u => u.Value == url).ToArray())
                                Urls.Remove(item);
                    }
                ));
            }
        }

        internal class ParseLinksParams
        {
            public string[] Urls { get; set; }
            public ParserParse Parse { get; set; }
            public byte ThreadCount { get; set; }
        }

        private BackgroundWorker bwParseLinks = null;
        private DelegateCommand parseUrlsCommand = null;
        public ICommand ParseUrlsCommand
        {
            get
            {
                return parseUrlsCommand ?? (parseUrlsCommand = new DelegateCommand(
                    (o) =>
                    {
                        string label = o as string;

                        Urls.AsParallel().ForAll((u) => { u.FinishResult = 0; });

                        bwParseLinks = new BackgroundWorker();
                        bwParseLinks.DoWork += (s, e) =>
                            {
                                ParseLinksParams param = e.Argument as ParseLinksParams;
                                if (param != null)
                                {
                                    string[] urls = param.Urls;
                                    BackgroundWorker bw = s as BackgroundWorker;
                                    float cnt = urls.Length;
                                    float i = 0;
                                    object lockObject = new Object();
                                    ParseResult[] res = param.Parse(
                                            urls
                                            , param.ThreadCount
                                            , label == null ? null : new string[] { label }
                                            , new Action<ParseResult>((ps) =>
                                            {
                                                lock (lockObject)
                                                {
                                                    i++;
                                                    bw.ReportProgress((int)((i / cnt) * 100));
                                                }
                                            })
                                        );
                                    e.Result = res;
                                }
                            };
                        bwParseLinks.RunWorkerCompleted += (s, e) =>
                            {
                                try
                                {
                                    if (e.Error != null)
                                        throw e.Error;

                                    ParseResult[] res = (ParseResult[])e.Result;

                                    foreach (var item in res)
                                        Urls
                                            .AsParallel()
                                            .Where(u => u.Value == item.Url)
                                            .ForAll(
                                                (u) =>
                                                {
                                                    int filled = item.Data.Where(i => !string.IsNullOrWhiteSpace(i.Value)).Count();
                                                    int mustBeFilled = item.Parser.Rules.Count;

                                                    if (filled == mustBeFilled && filled > 0)
                                                        u.FinishResult = 1;
                                                    else if (filled < mustBeFilled && filled > 0)
                                                        u.FinishResult = 2;
                                                    else
                                                        u.FinishResult = 0;

                                                    u.Result = item;
                                                }
                                            );
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(string.Format("Ошибка при парсинге:{0}{1}", Environment.NewLine, ex.Message),"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                finally
                                {
                                    bwParseLinks.Dispose();
                                    bwParseLinks = null;
                                    IsBusy = false;
                                }
                            };
                        bwParseLinks.ProgressChanged += (s, e) =>
                            {
                                ProgressText = e.ProgressPercentage > 0 && e.ProgressPercentage < 100 ? string.Format("Завершено: {0}%", e.ProgressPercentage) : string.Empty;
                            };
                        bwParseLinks.WorkerReportsProgress = true;
                        IsBusy = true;
                        bwParseLinks.RunWorkerAsync(new ParseLinksParams() { Urls = Urls.Select(u => u.Value).ToArray(), Parse = Parsers.Parse, ThreadCount = this.ThreadCount });
                    }
                ));
            }
        }

        internal class ExportLinksParams
        {
            public StringUrlWithResultWrapper[] Urls { get; set; }
            public string FileName { get; set; }
        }

        private BackgroundWorker bwExportLinks = null;
        private DelegateCommand exportUrlsCommand = null;
        public ICommand ExportUrlsCommand
        {
            get
            {
                return exportUrlsCommand ?? (exportUrlsCommand = new DelegateCommand(
                    (o) =>
                    {
                        SaveFileDialog sfd = new SaveFileDialog()
                        {
                            Filter = "Tables files (*.csv)|*.csv|All files (*.*)|*.*",
                            OverwritePrompt = true,
                            RestoreDirectory = true
                        };
                        if (sfd.ShowDialog() ?? false)
                        {
                            bwExportLinks = new BackgroundWorker();
                            bwExportLinks.DoWork += (s, e) =>
                                {
                                    ExportLinksParams param = e.Argument as ExportLinksParams;
                                    if (param != null)
                                    {
                                        List<string> labels = new List<string>();
                                        object lockObject = new Object();
                                        param.Urls.AsParallel().ForAll((u) =>
                                        {
                                            foreach(var key in u.Result.Data.Keys)
                                                if (!labels.Contains(key))
                                                    lock (lockObject)
                                                    {
                                                        if (string.IsNullOrWhiteSpace(key))
                                                            throw new Exception("Обнаружена пустая метка. Её тут быть не должно!");
                                                        labels.Add(key);
                                                    }
                                        });

                                        var distinctLabels = labels.Distinct();

                                        DataTable dt = new DataTable(System.IO.Path.GetFileNameWithoutExtension(param.FileName));
                                        dt.Columns.Add("Ссылка", typeof(string));
                                        foreach (string c in distinctLabels)
                                            dt.Columns.Add(c, typeof(string));

                                        foreach (var u in param.Urls)
                                        {
                                            DataRow nRow = dt.NewRow();
                                            nRow[0] = u.Value;
                                            foreach (string c in distinctLabels)
                                                nRow[c] = u.Result.Data.ContainsKey(c) ? u.Result.Data[c] : string.Empty;
                                            dt.Rows.Add(nRow);
                                        }

                                        dt.AcceptChanges();

                                        CSVWriter.Write(dt, param.FileName, Encoding.UTF8);
                                    }
                                };
                            bwExportLinks.RunWorkerCompleted += (s, e) =>
                                {
                                    try
                                    {
                                        if (e.Error != null)
                                            throw e.Error;
                                    }
                                    catch(Exception ex)
                                    {
                                        MessageBox.Show(string.Format("Ошибка при экспорте файла: {0}{1}", Environment.NewLine, ex.Message), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                    finally
                                    {
                                        IsBusy = false;
                                        bwExportLinks.Dispose();
                                        bwExportLinks = null;
                                    }
                                };
                            IsBusy = true;
                            bwExportLinks.RunWorkerAsync(new ExportLinksParams() { FileName = sfd.FileName, Urls = this.Urls.ToArray() });
                        }
                    }
                ));
            }
        }

        private DelegateCommand navigateCommand = null;
        public ICommand NavigateCommand
        {
            get
            {
                return navigateCommand ?? (navigateCommand = new DelegateCommand(
                    (o) =>
                    {
                        string url = o as string;
                        if (!string.IsNullOrEmpty(url))
                            Process.Start(url);
                    }
                ));
            }
        }

        private void ParsersGenerateRuleControl_Done(object sender, RuleRoutedEventArgs e)
        {
            try
            {
                IsBusy = true;

                var parserExisted = (Parsers.Parsers.Where(p => p.Url == e.Parser.Url).FirstOrDefault());

                if (parserExisted == null)
                {
                    parserExisted = new Parser() { Url = e.Parser.Url };
                    Parsers.Parsers.Add(parserExisted);
                }

                foreach (var r in parserExisted.Rules.Where(r => r.Label == e.Rule.Label).ToArray())
                    parserExisted.Rules.Remove(r);

                parserExisted.Rules.Add(
                    new ParseRule()
                    {
                        Label = e.Rule.Label,
                        Parameter = e.Rule.Parameter,
                        Condition = e.Rule.Condition,
                        Connection = e.Rule.Connection,
                        MinImageSize = e.Rule.MinImageSize,
                        CheckImageSize = e.Rule.CheckImageSize
                    });

                if (AllowClearUrlsAfterImport)
                    foreach (var url in Urls.Where(i => Helper.IsWellFormedUriString(i.Value, UriKind.Absolute) && Helper.StringLikes(new Uri(i.Value).Host, e.Parser.Url)).ToArray())
                        Urls.Remove(url);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion;

        #region Additional

        public static readonly DependencyProperty ExpandRowProperty = DependencyProperty.RegisterAttached(
        "ExpandRow", typeof(bool), typeof(ParsersImportControl),
        new FrameworkPropertyMetadata(new PropertyChangedCallback(ExpandRowChanged)));

        public static void ExpandRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (Grid)LogicalTreeHelper.GetParent(d);
            var row = (int)d.GetValue(Grid.RowProperty);

            if ((bool)e.NewValue)
            {
                grid.RowDefinitions[row].Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                grid.RowDefinitions[row].Height = new GridLength(0, GridUnitType.Auto);
            }
        }

        public static void SetExpandRow(DependencyObject d, bool value)
        {
            d.SetValue(ParsersImportControl.ExpandRowProperty, value);
        }

        public static bool GetExpandRow(DependencyObject d)
        {
            return (bool)d.GetValue(ParsersImportControl.ExpandRowProperty);
        }

        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Urls != null)
            {
                Urls.CollectionChanged -= Urls_CollectionChanged;
                Urls.CollectionChanged += Urls_CollectionChanged;
            }

            bool designTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());
            if (!designTime)
                SiteManager.Init(GridMain, Dispatcher);
        }

        private void Urls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ReCalcHosts();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //(sender as Button).ContextMenu.IsEnabled = true;
            (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
            (sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            (sender as Button).ContextMenu.IsOpen = true;
        }
    }
}
