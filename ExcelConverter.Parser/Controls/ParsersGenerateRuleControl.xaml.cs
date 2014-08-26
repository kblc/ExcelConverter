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
using Helpers;
using Helpers.WPF;

namespace ExcelConverter.Parser.Controls
{
    public class RuleRoutedEventArgs : RoutedEventArgs
    {
        public RuleRoutedEventArgs() { }
        public RuleRoutedEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }
        public RuleRoutedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }
        public ParseRule Rule { get; set; }
        public Parser Parser { get; set; }
    }

    public delegate void RuleRoutedEventHandler(object sender, RuleRoutedEventArgs e);

    public partial class ParsersGenerateRuleControl : UserControl, INotifyPropertyChanged
    {
        public event RuleRoutedEventHandler Cancel
        {
            add { AddHandler(CancelEvent, value); }
            remove { RemoveHandler(DoneEvent, value); }
        }
        public event RuleRoutedEventHandler Done
        {
            add { AddHandler(DoneEvent, value); }
            remove { RemoveHandler(DoneEvent, value); }
        }

        private bool isBusy = false;
        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
            private set
            {
                isBusy = value;
                RaisePropertyChanged("IsBusy");
            }
        }

        public static readonly DependencyProperty ParserProperty = DependencyProperty.Register("Parser", typeof(Parser), typeof(ParsersGenerateRuleControl), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly RoutedEvent CancelEvent = EventManager.RegisterRoutedEvent("Cancel", RoutingStrategy.Bubble, typeof(RuleRoutedEventHandler), typeof(ParsersGenerateRuleControl));
        public static readonly RoutedEvent DoneEvent = EventManager.RegisterRoutedEvent("Done", RoutingStrategy.Bubble, typeof(RuleRoutedEventHandler), typeof(ParsersGenerateRuleControl));
        public static readonly DependencyProperty NewParseRuleProperty = DependencyProperty.Register("NewParseRule", typeof(ParseRule),     typeof(ParsersGenerateRuleControl), new FrameworkPropertyMetadata(new ParseRule()));
        public static readonly DependencyProperty UrlsProperty =         DependencyProperty.Register("Urls",         typeof(UrlCollection), typeof(ParsersGenerateRuleControl), new FrameworkPropertyMetadata(new UrlCollection(), new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty CanCacelProperty = DependencyProperty.Register("CanCancel", typeof(bool), typeof(ParsersGenerateRuleControl), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty CanEditUrlProperty = DependencyProperty.Register("CanEditUrl", typeof(bool), typeof(ParsersGenerateRuleControl), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        public ParsersGenerateRuleControl()
        {
            InitializeComponent();
        }

        private void Urls_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            foreach (var i in e.NewItems.Cast<StringWrapper>())
                if (!UrlsToAddList.Any( u => u.Value == i.Value))
                    UrlsToAddList.Add(new UrlResultWrapper() { Value = i.Value });

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            foreach (var i in e.OldItems.Cast<StringWrapper>())
                foreach (var item in UrlsToAddList.Where(u => u.Value == i.Value).ToArray())
                    UrlsToAddList.Remove(item); 
        }

        private static void MyCustom_PropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            ParsersGenerateRuleControl p = dobj as ParsersGenerateRuleControl;
            if (p != null)
            {
                if (e.Property == ParserProperty)
                {
                    p.UpdateStep(0, false, false);
                }

                if (e.Property == UrlsProperty)
                {
                    p.UrlsToAddList.Clear();
                    if (e.NewValue != null)
                    {
                        foreach(var i in ((UrlCollection)e.NewValue))
                            p.UrlsToAddList.Add(new UrlResultWrapper() { Value = i.Value });
                        ((UrlCollection)e.NewValue).CollectionChanged += p.Urls_CollectionChanged;
                    }
                    if (e.OldValue != null)
                        ((UrlCollection)e.OldValue).CollectionChanged -= p.Urls_CollectionChanged;
                }

                if (e.Property == ParserProperty)
                {
                    p.RaisePropertyChanged("ThreadCount");
                }
            }
        }

        public UrlCollection Urls
        {
            get { return this.GetValue(UrlsProperty) as UrlCollection; }
            set { this.SetValue(UrlsProperty, value); }
        }

        private readonly ObservableCollection<UrlResultWrapper> urlsToAddList = new ObservableCollection<UrlResultWrapper>();
        public ObservableCollection<UrlResultWrapper> UrlsToAddList
        {
            get
            {
                return urlsToAddList;
            }
        }

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

        public bool CanCancel
        {
            get
            {
                return (bool)this.GetValue(CanCacelProperty);
            }
            set
            {
                this.SetValue(CanCacelProperty, value);
            }
        }

        public bool CanEditUrl
        {
            get
            {
                return (bool)this.GetValue(CanEditUrlProperty);
            }
            set
            {
                this.SetValue(CanEditUrlProperty, value);
            }
        }

        private DelegateCommand deleteUrlWithImagesCommand = null;
        public ICommand DeleteUrlWithImagesCommand
        {
            get
            {
                return deleteUrlWithImagesCommand ?? (deleteUrlWithImagesCommand = new DelegateCommand((o) =>
                {
                    string url = o as string;
                    foreach (var item in UrlsToAddList.Where(u => u.Value == url).ToArray())
                        UrlsToAddList.Remove(item);
                }));
            }
        }

        private DelegateCommand cancelCommand = null;
        public ICommand CancelCommand
        {
            get
            {
                return cancelCommand ?? (cancelCommand = new DelegateCommand((o) =>
                {
                    UpdateStep(0, false, true);
                    RaiseEvent(new RuleRoutedEventArgs(CancelEvent));
                }));
            }
        }

        private DelegateCommand doneCommand = null;
        public ICommand DoneCommand
        {
            get
            {
                return doneCommand ?? (doneCommand = new DelegateCommand((o) =>
                {
                    Parser savedParser = Parser;
                    ParseRule savedRule = (o as ParseRule);
                    if (savedRule == null)
                    {
                        savedRule = new ParseRule();
                        NewParseRule.CopyObject(savedRule);
                    }
                    UpdateStep(0, false, true);
                    RaiseEvent(new RuleRoutedEventArgs(DoneEvent) { Rule = savedRule, Parser = savedParser });
                }));
            }
        }

        private DelegateCommand addNewRuleCommand = null;
        public ICommand AddNewRuleCommand
        {
            get
            {
                return addNewRuleCommand ?? (addNewRuleCommand = new DelegateCommand((o) =>
                {
                    ParseRule savedRule = new ParseRule();
                    NewParseRule.CopyObject(savedRule);

                    foreach (var item in Parser.Rules.Where(i => i.Label == savedRule.Label).ToArray())
                        Parser.Rules.Remove(item);
                    Parser.Rules.Add(savedRule);

                    if (DoneCommand != null)
                        DoneCommand.Execute(savedRule);
                }));
            }
        }

        private DelegateCommand nextStepCommand = null;
        public ICommand NextStepCommand
        {
            get
            {
                return nextStepCommand ?? (nextStepCommand = new DelegateCommand((o) =>
                {
                    TabControl tc = o as TabControl;
                    if (tc != null && tc.SelectedIndex < tc.Items.Count - 1)
                    {
                        UpdateStep(tc.SelectedIndex + 1, true);
                    }
                }));
            }
        }

        private DelegateCommand prevStepCommand = null;
        public ICommand PrevStepCommand
        {
            get
            {
                return prevStepCommand ?? (prevStepCommand = new DelegateCommand((o) =>
                {
                    TabControl tc = o as TabControl;
                    if (tc != null && tc.SelectedIndex > 0)
                    {
                        UpdateStep(tc.SelectedIndex - 1, false);
                    }
                }));
            }
        }

        private byte threadCount = ExcelConverter.Parser.Properties.Settings.Default.ThreadCount;
        public byte ThreadCount
        {
            get { return Parser == null ? threadCount : Parser.ThreadCount; }
            set
            {
                if (Parser != null)
                    Parser.ThreadCount = value;
                threadCount = value;
                RaisePropertyChanged("ThreadCount");
            }
        }

        private void UpdateStep(int step, bool action, bool clear = false, byte insideThreadCount = 3)
        {
            if (clear)
                UrlsToAddList.Clear();

            #region step 1
            if (step == 1 && action)
            {
                object lockAdd = new Object();
                UrlsToAddList.Clear();

                insideThreadCount = ThreadCount;

                ParseRuleConnectionType type = NewParseRule.Connection;

                int minWidth = NewParseRule.MinImageWidth;
                int minHeight = NewParseRule.MinImageHeight;
                bool collectIMGTags = NewParseRule.CollectIMGTags;
                bool collectLINKTags = NewParseRule.CollectLINKTags;
                bool collectMETATags = NewParseRule.CollectMETATags;
                byte threadCount = this.ThreadCount;

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (s, e) =>
                    {
                        Helpers.PercentageProgress progress = new Helpers.PercentageProgress();
                        progress.Change += (sP, eP) =>
                            {
                                bw.ReportProgress((int)eP.Value);
                            };

                        List<UrlResultWrapper> urlResultWrapper = new List<UrlResultWrapper>();
                        var urls = e.Argument as StringUrlWithResultWrapper[];

                        if (urls != null)
                            urls
                                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Value) && Helper.IsWellFormedUriString(item.Value, UriKind.Absolute))
                                .Select(sw => 
                                    new 
                                        { 
                                            item = sw,
                                            prgItem = progress.GetChild() 
                                        })
                                .ToArray()
                                .AsParallel()
                                .WithDegreeOfParallelism(insideThreadCount)
                                .ForAll(
                                    (sw) =>
                                    {
                                        var item = new UrlResultWrapper() { Value = sw.item.Value };

                                        System.Drawing.Size minSize = new System.Drawing.Size() { Width = minWidth, Height = minHeight };

                                        var result = Helper.GetAllImagesFromUrl(item.Value, minSize, collectIMGTags, collectLINKTags, collectMETATags, threadCount, sw.prgItem, true, type);

                                        foreach (ParseImageResult res in result)
                                            item.ParseResult.Add(res);

                                        if (item.ParseResult.Count > 0)
                                            lock (lockAdd)
                                            {
                                                urlResultWrapper.Add(item);
                                            }
                                    });

                        e.Result = urlResultWrapper;
                    };
                bw.RunWorkerCompleted += (s, e) =>
                    {
                        if (e.Error != null)
                            throw e.Error;

                        try
                        {
                            List<UrlResultWrapper> urlResultWrapper = e.Result as List<UrlResultWrapper>;
                            foreach (var item in urlResultWrapper)
                                UrlsToAddList.Add(item);
                        }
                        finally
                        {
                            bw.Dispose();
                            IsBusy = false;
                        }
                    };
                bw.WorkerReportsProgress = true;
                bw.ProgressChanged += (s, e) =>
                    {
                        LoadedPercent = e.ProgressPercentage;
                    };
                IsBusy = true;
                bw.RunWorkerAsync(Urls.ToArray());
                while (bw.IsBusy)
                    Helper.DoEvents();
                bw = null;
            }
            #endregion
            #region step 2
            else if (step == 2 && action)
            {
                HtmlNodeWithUrl[] nodes =
                    UrlsToAddList
                    .Where(n => !string.IsNullOrWhiteSpace(n.Value))
                    .Select(i => 
                        {
                            ParseImageResult res = i.ParseResult.Where(i2 => i2.IsSelected).FirstOrDefault();
                            return
                                new HtmlNodeWithUrl()
                                {
                                    Node = res == null ? null : res.Node,
                                    Url = res == null ? new Uri(i.Value, UriKind.RelativeOrAbsolute) : res.Url
                                };
                        }
                    )
                    .Where(i3 => i3 != null && i3.Node != null)
                    .ToArray();

                ParseRule newRule = Helper.GetRule(nodes, AddRuleLabelComboBox.Text, NewParseRule.MinImageSize, NewParseRule.CollectIMGTags, NewParseRule.CollectLINKTags, NewParseRule.CollectMETATags);
                newRule.CopyObject(NewParseRule, new string[] { "Connection" });
            }
            #endregion

            for (int i = UrlsToAddTabControl.Items.Count - 1; i >= 0; i--)
                (UrlsToAddTabControl.Items[i] as TabItem).Visibility = (i == step) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            UrlsToAddTabControl.SelectedIndex = step;
        }

        public ParseRule NewParseRule
        {
            get
            {
                return this.GetValue(NewParseRuleProperty) as ParseRule;
            }
            private set
            {
                this.SetValue(NewParseRuleProperty, value);
            }
        }

        private System.Drawing.Image lastSelected = null;
        public System.Drawing.Image LastSelected
        {
            get
            {
                return lastSelected;
            }
            private set
            {
                lastSelected = value;
                RaisePropertyChanged("LastSelected");
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (ParseImageResult i in e.RemovedItems.Cast<ParseImageResult>())
                i.IsSelected = false;
            foreach (ParseImageResult i in e.AddedItems.Cast<ParseImageResult>())
            {
                i.IsSelected = true;
                LastSelected = i.Image;
            }
        }

        private void AddNewRuleSelectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ParseImageResult)
                foreach (ParseImageResult i in e.AddedItems.Cast<ParseImageResult>())
                {
                    i.IsSelected = true;
                    LastSelected = i.Image;
                }
            else if (e.AddedItems.Count > 0 && e.AddedItems[0] is UrlResultWrapper)
            {
                UrlResultWrapper wrapper = e.AddedItems.Cast<UrlResultWrapper>().LastOrDefault();
                if (wrapper != null)
                    LastSelected = wrapper.ParseResult.Where(i => i.IsSelected).Select(i => i.Image).FirstOrDefault();
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

        private int loadedPercent = 0;
        public int LoadedPercent 
        {
            get
            {
                return loadedPercent;
            }
            set
            {
                loadedPercent = value;
                RaisePropertyChanged("LoadedPercent");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Urls.CollectionChanged += Urls_CollectionChanged;
            UpdateStep(0, false);

            //SiteManager.Init(GridMain, Dispatcher);
        }
    }
}
