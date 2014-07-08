using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

using Helpers;
using Helpers.WPF;
using Helpers.Serialization;

namespace ExcelConverter.Parser
{
    [Serializable]
    public enum ParseFindRuleCondition
    {
        None = 0,
        ByLink,
        ByLinkAndIndex,
        //ByAttributes,
        //ByClass,
        //ByParent,
        ByXPath,
        ByXPathAndIndex
    }

    [Serializable]
    public enum ParseRuleConnectionType
    {
        Direct = 0,
        IE_00_sec,
        IE_05_sec,
        IE_10_sec,
        CHR_00_sec,
        CHR_05_sec,
        CHR_10_sec
    }

    public sealed class ParseFindRuleConditionList : List<ParseFindRuleCondition>
    {
        public ParseFindRuleConditionList()
        {
            foreach (var item in typeof(ParseFindRuleCondition).GetEnumValues().Cast<int>().Select( i=> (ParseFindRuleCondition)i))
            {
                Add(item);
            }
        }
    }

    public sealed class ParseRuleConnectionTypeList : List<ParseRuleConnectionType>
    {
        public ParseRuleConnectionTypeList()
        {
            foreach (var item in typeof(ParseRuleConnectionType).GetEnumValues().Cast<int>().Select(i => (ParseRuleConnectionType)i))
            {
                Add(item);
            }
        }
    }

    [Serializable]
    public class ParseRule : INotifyPropertyChanged
    {
        private string label = string.Empty;
        public string Label
        {
            get
            {
                return label;
            }
            set
            {
                label = value;
                RaisePropertyChanged("Label");
            }
        }

        private ParseFindRuleCondition condition = ParseFindRuleCondition.None;
        public ParseFindRuleCondition Condition
        {
            get
            {
                return condition;
            }
            set
            {
                condition = value;
                RaisePropertyChanged("Condition");
            }
        }

        private ParseRuleConnectionType connection = ParseRuleConnectionType.Direct;
        public ParseRuleConnectionType Connection
        {
            get
            {
                return connection;
            }
            set
            {
                connection = value;
                RaisePropertyChanged("Connection");
            }
        }

        private int minImageWidth = ExcelConverter.Parser.Properties.Settings.Default.MinImageWidth;
        public int MinImageWidth
        {
            get { return minImageWidth; }
            set 
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("Значение должно быть больше 0");
                minImageWidth = value; 
                RaisePropertyChanged("MinImageWidth");
                RaisePropertyChanged("MinImageSize");
            }
        }

        private int minImageHeight = ExcelConverter.Parser.Properties.Settings.Default.MinImageHeight;
        public int MinImageHeight
        {
            get { return minImageHeight; }
            set 
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("Значение должно быть больше 0");
                minImageHeight = value;
                RaisePropertyChanged("MinImageHeight");
                RaisePropertyChanged("MinImageSize");
            }
        }

        private bool checkImageSize = false;
        public bool CheckImageSize
        {
            get { return checkImageSize; }
            set { checkImageSize = value; RaisePropertyChanged("CheckImageSize"); }
        }

        private bool collectIMGTags = true;
        public bool CollectIMGTags
        {
            get { return collectIMGTags; }
            set { collectIMGTags = value; RaisePropertyChanged("CollectIMGTags"); }
        }

        private bool collectLINKTags = true;
        public bool CollectLINKTags
        {
            get { return collectLINKTags; }
            set { collectLINKTags = value; RaisePropertyChanged("CollectLINKTags"); }
        }

        private bool collectMETATags = true;
        public bool CollectMETATags
        {
            get { return collectMETATags; }
            set { collectMETATags = value; RaisePropertyChanged("CollectMETATags"); }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Drawing.Size MinImageSize
        {
            get
            {
                return new System.Drawing.Size() { Height = minImageHeight, Width = minImageWidth };
            }
            set
            {
                MinImageWidth = value.Width;
                MinImageHeight = value.Height;
            }
        }

        private string parameter = string.Empty;
        public string Parameter
        {
            get
            {
                return parameter;
            }
            set
            {
                parameter = value;
                RaisePropertyChanged("Parameter");
            }
        }

        public ParseRule() { }

        public string Parse(HtmlDocument doc, string baseUrl, string urlToParse)
        {
            string result = string.Empty;

            if (Condition == ParseFindRuleCondition.ByLink || Condition == ParseFindRuleCondition.ByXPath)
            {
                string mask = Parameter;
                if (string.IsNullOrWhiteSpace(doc.DocumentNode.InnerText))
                {
                    result = urlToParse;
                }
                else
                {
                    var links = Helper.GetAllImagesUrlsFromUrl(doc, baseUrl, collectIMGTags, collectLINKTags, collectMETATags)
                                       .Where(n => Helper.StringLikes(
                                           Condition == ParseFindRuleCondition.ByLink ? n.Url.AbsoluteUri : n.Node.XPath
                                           , mask))
                                       .ToArray();

                    if (CheckImageSize)
                        links = Helper.GetAllImagesUrlsWithMinSize(links, MinImageSize);

                    var results = links.Select(n => n.Url.AbsoluteUri).ToArray();

                    result = results.FirstOrDefault();
                }
            }
            else if (Condition == ParseFindRuleCondition.ByLinkAndIndex || Condition == ParseFindRuleCondition.ByXPathAndIndex)
            {
                var lInd = parameter.LastIndexOf(";");
                if (lInd >= 0 && parameter.Length > lInd)
                {
                    int index;
                    if (int.TryParse(parameter.Substring(lInd + 1), out index))
                    {
                        string mask = parameter.Substring(0, lInd);
                        if (string.IsNullOrWhiteSpace(doc.DocumentNode.InnerText))
                        {
                            result = urlToParse;
                        }
                        else
                        {
                            var links = Helper.GetAllImagesUrlsFromUrl(doc, baseUrl, collectIMGTags, collectLINKTags, collectMETATags)
                                        .Where(n => Helper.StringLikes(
                                            Condition == ParseFindRuleCondition.ByLinkAndIndex ? n.Url.AbsoluteUri : n.Node.XPath
                                            , mask))
                                        .ToArray();
                            
                            if (CheckImageSize)
                                links = Helper.GetAllImagesUrlsWithMinSize(links, MinImageSize);

                            var results = links.Select(n => n.Url.AbsoluteUri).ToArray();

                            if (results.Length >= index + 1)
                                result = results[index];
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(result))
                result = Helper.GetFullSourceLink(result, doc, baseUrl).AbsoluteUri;

            return result ?? string.Empty;
        }

        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public class ParseResult: INotifyPropertyChanged
    {
        private string url = string.Empty;
        public string Url
        {
            get { return url; }
            set { url = value; RaisePropertyChanged("Url"); }
        }

        private Dictionary<string, string> data = new Dictionary<string,string>();
        public Dictionary<string, string> Data
        {
            get { return data; }
            set { Init(value); RaisePropertyChanged("Data"); }
        }

        public ParseResult() { }
        public ParseResult(string url, Dictionary<string,string> labelsWithData)
        {
            this.Url = url;
            Init(labelsWithData);
        }

        private Parser parser = null;
        public Parser Parser
        {
            get { return parser; }
            set { parser = value; RaisePropertyChanged("Parser"); }
        }

        private double timeToLoadContent = 0;
        public double TimeToLoadContent
        {
            get { return timeToLoadContent; }
            set { timeToLoadContent = value; RaisePropertyChanged("TimeToLoadContent"); }
        }

        private double timeToParse = 0;
        public double TimeToParse
        {
            get { return timeToParse; }
            set { timeToParse = value; RaisePropertyChanged("TimeToParse"); }
        }

        public void Init(Dictionary<string, string> labelsWithData)
        {
            data.Clear();
            if (labelsWithData != null)
                foreach (var i in labelsWithData)
                    data.Add(i.Key, i.Value);
        }

        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public delegate ParseResult[] ParserParse(string[] urls, byte threadCount, string[] labels, Action<ParseResult> resultAdded = null, Func<bool> isCanceled = null);

    [Serializable]
    [System.Xml.Serialization.XmlRoot("ParserCollection")]
    [System.Xml.Serialization.XmlInclude(typeof(ParserCollection))]
    [System.Xml.Serialization.XmlInclude(typeof(DBParserCollection))]
    public abstract class DBParserCollection : ParserCollection
    {
        public DBParserCollection() : base()
        {
            this.Parsers.CollectionChanged += Parsers_CollectionChanged;
        }

        protected virtual void DBParserUpdate(Parser p)
        {
            if (StoreDirect)
            {
                //TODO: try to update
                p.IsChanged = false;
                throw new NotImplementedException();
            }
        }

        protected virtual void DBParserRemove(Parser p)
        {
            if (StoreDirect)
            {
                //TODO: try to delete
                p.Id = Guid.Empty;
                p.IsChanged = false;
                throw new NotImplementedException();
            }
            else
                p.IsDeleted = true;
        }

        protected virtual void DBParserAdd(Parser p)
        {
            if (StoreDirect)
            {
                //TODO: try to insert
                p.Id = Guid.NewGuid();
                p.IsChanged = false;
                throw new NotImplementedException();
            }
        }

        protected virtual void DBParserLoad()
        {
            //TODO: try to load parsers from database
            Parser[] parsersLoaded = new Parser[] { };

            foreach (var p in parsersLoaded)
                Parsers.Add(p);

            throw new NotImplementedException();
        }

        protected virtual void DBParserSave()
        {
            Parser[] parsersToUpdate = Parsers.Where(p => p.IsChanged).ToArray();
            //TODO: try to update parsers
            throw new NotImplementedException();
        }

        #region Some propertyes

        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        private bool storeLocal = true;
        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        public bool StoreLocal
        {
            get { return storeLocal; }
            set { storeLocal = value; RaisePropertyChanged("StoreLocal"); }
        }

        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        private bool storeDirect = true;
        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        public bool StoreDirect
        {
            get { return storeDirect; }
            set { storeDirect = value; RaisePropertyChanged("StoreDirect"); }
        }

        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        private bool isBusy = true;
        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        public bool IsBusy { get { return isBusy; } private set { isBusy = value; RaisePropertyChanged("IsBusy"); } }

        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        private string fullParsersPath = null;
        [field: NonSerialized]
        [System.Xml.Serialization.XmlIgnore]
        public string FullParsersPath
        {
            get
            {
                return fullParsersPath ??
                    (
                        fullParsersPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "parsers.xml")
                    );
            }
        }

        private void Parsers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var p in e.NewItems.Cast<Parser>())
                {
                    p.PropertyChanged += Parser_PropertyChanged;
                    if (!StoreLocal && !IsBusy)
                        DBParserAdd(p);
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var p in e.OldItems.Cast<Parser>())
                {
                    p.PropertyChanged -= Parser_PropertyChanged;
                    if (!StoreLocal && !IsBusy)
                        DBParserRemove(p);
                }
            }
        }

        private void Parser_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!StoreLocal && !IsBusy)
            {
                Parser p = sender as Parser;
                if (p != null)
                {
                    if (p.IsDeleted)
                        DBParserRemove(p);
                    else if (p.Id != Guid.Empty)
                        DBParserUpdate(p);
                    else
                        DBParserAdd(p);
                }
            }
        }

        #endregion
        #region Load/Save
        public void Load()
        {
            IsBusy = true;
            try
            {
                if (StoreLocal)
                    try
                    {
                        ExcelConverter.Parser.Controls.ParsersControl.LoadParsers(this, FullParsersPath);
                    }
                    catch(Exception ex)
                    {
                        Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, string.Format("DBParserCollection.Load(file: {0})", FullParsersPath)));
                    }
                else
                    DBParserLoad();

                foreach (var p in Parsers)
                    p.IsChanged = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Save()
        {
            IsBusy = true;
            try
            {
                if (StoreLocal)
                    try
                    {
                        ExcelConverter.Parser.Controls.ParsersControl.SaveParsers(this, FullParsersPath);
                    }
                    catch (Exception ex)
                    {
                        Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, string.Format("DBParserCollection.Save(file: {0})", FullParsersPath)));
                    }

                else
                    DBParserSave();
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion
    }

    [Serializable]
    [System.Xml.Serialization.XmlRoot("ParserCollection")]
    [System.Xml.Serialization.XmlInclude(typeof(ParserCollection))]
    public class ParserCollection : INotifyPropertyChanged, Helpers.Serialization.ISerializable
    {
        private const string LabelPhoto = "Фото";
        private const string LabelSchema = "Схема";

        public ParserCollection()
        {
            if (!labels.Contains(LabelPhoto))
                labels.Add(LabelPhoto);
            if (!labels.Contains(LabelSchema))
                labels.Add(LabelSchema);
            
            Labels.CollectionChanged += (s, e) => { RaisePropertyChanged("Labels"); };
            Parsers.CollectionChanged += (s, e) => 
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    //remove already existed with this name (on import)
                    //var urls = e.NewItems.Cast<Parser>().Select( p => p.Url).ToArray();
                    //var alreadExistedWithThisUrls = Parsers.Where(p => urls.Contains(p.Url) && !e.NewItems.Cast<Parser>().Contains(p)).ToArray();
                    //foreach(var p in alreadExistedWithThisUrls)
                    //    Parsers.Remove(p);

                    foreach (Parser parser in e.NewItems)
                        parser.ParentCollection = this;
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    foreach (Parser parser in e.OldItems)
                        parser.ParentCollection = null;

                RaisePropertyChanged("Parsers");
            };
        }

        private readonly ObservableCollection<string> labels = new ObservableCollection<string>(); //new string[] { LabelPhoto, LabelSchema }
        [field: NonSerializedAttribute()]
        public ObservableCollection<string> Labels
        {
            get
            {
                return labels;
            }
        }

        private readonly ObservableCollection<Parser> parsers = new ObservableCollection<Parser>();
        [field: NonSerializedAttribute()]
        public ObservableCollection<Parser> Parsers
        {
            get
            {
                return parsers;
            }
        }

        public ParseResult[] Parse(string[] urls, byte threadCount, string[] labels = null, Action<ParseResult> resultAdded = null, Func<bool> isCanceled = null)
        {
            List<ParseResult> result = new List<ParseResult>();
            object lockObject = new Object();
            urls.AsParallel()
                .Where(u => Helper.IsWellFormedUriString(u, UriKind.Absolute))
                .Select(u => new Uri(u).Host)
                .Distinct()
                .Select(h => Parsers.FirstOrDefault(p => Helper.StringLikes(h, p.Url)))
                .Where(p => p != null)
                .Distinct()
                .ForAll(
                (parser) =>
                {
                    if (isCanceled != null && isCanceled())
                        return;

                    string[] subUrls = urls.Where(u =>
                        Helper.IsWellFormedUriString(u, UriKind.Absolute) &&
                        Helper.StringLikes(new Uri(u).Host, parser.Url)).ToArray();
                    ParseResult[] res = threadCount == byte.MinValue ? parser.Parse(subUrls, labels, resultAdded, isCanceled) : parser.Parse(subUrls, threadCount, labels, resultAdded, isCanceled);
                    lock (lockObject)
                    {
                        result.AddRange(res);
                    }
                }
                );
            return result.OrderBy(i => i.Url).ToArray();
        }
        public ParseResult[] Parse(string[] urls, string[] labels = null, Action<ParseResult> resultAdded = null)
        {
            return Parse(urls, byte.MinValue, labels, resultAdded);
        }

        #region Serialization

        public string Serialize()
        {
            return this.SerializeToBase64(true);
        }

        public string SerializeXML()
        {
            return Helpers.Serialization.Extensions.SerializeToXML(this, false);
        }

        public static ParserCollection Deserilize(string obj)
        {
            ParserCollection result;
            typeof(ParserCollection).DeserializeFromBase64(obj, true, out result);
            return result;
        }

        public static ParserCollection DeserilizeXML(string obj)
        {
            ParserCollection result;
            typeof(ParserCollection).DeserializeFromXML(obj, out result);
            return result;
        }


        #endregion
        #region INotifyPropertyChanged

        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void OnDeserialized()
        {
            foreach (var p in Parsers)
                p.Init();
        }
    }

    [Serializable]
    //[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class Parser : INotifyPropertyChanged, Helpers.Serialization.ISerializable
    {
        [NonSerializedAttribute]
        [System.Xml.Serialization.XmlIgnore]
        private Guid id = Guid.Empty;

        [System.Xml.Serialization.XmlIgnore]
        [field: NonSerializedAttribute()]
        public Guid Id
        {
            get { return id; }
            set
            {
                id = value;
                RaisePropertyChanged("Id");
            }
        }

        [field: NonSerializedAttribute()]
        [System.Xml.Serialization.XmlIgnore]
        private bool isChanged = false;

        [field: NonSerializedAttribute()]
        [System.Xml.Serialization.XmlIgnore]
        public bool IsChanged
        {
            get { return isChanged; }
            set
            {
                if (isChanged == value)
                    return;
                isChanged = value;
                RaisePropertyChanged("IsChanged");
            }
        }

        [NonSerializedAttribute]
        [System.Xml.Serialization.XmlIgnore]
        private bool isDeleted = false;

        [System.Xml.Serialization.XmlIgnore]
        [field: NonSerializedAttribute()]
        public bool IsDeleted
        {
            get { return isDeleted; }
            set
            {
                if (isDeleted == value)
                    return;
                isDeleted = value;
                RaisePropertyChanged("IsDeleted");
            }
        }

        [NonSerialized]
        private ParserCollection parentCollection = null;
        [field: NonSerializedAttribute()]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public ParserCollection ParentCollection
        {
            get
            {
                return parentCollection;
            }
            internal set
            {
                if (parentCollection == value)
                    return;

                if (parentCollection != null)
                    (parentCollection as INotifyPropertyChanged).PropertyChanged -= ParentCollection_PropertyChanged;

                parentCollection = value;

                if (parentCollection != null)
                    (parentCollection as INotifyPropertyChanged).PropertyChanged += ParentCollection_PropertyChanged;
            }
        }

        private byte threadCount = ExcelConverter.Parser.Properties.Settings.Default.ThreadCount;
        [field: NonSerializedAttribute()]
        public byte ThreadCount
        {
            get { return threadCount; }
            set
            {
                threadCount = value;
                RaisePropertyChanged("ThreadCount");
            }
        }

        [field: NonSerializedAttribute()]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        private ObservableCollection<string> labels = new ObservableCollection<string>();
        [field: NonSerializedAttribute()]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public ObservableCollection<string> Labels 
        { 
            get 
            {
                return ParentCollection != null ? ParentCollection.Labels : labels;
            }
            set
            {
                Labels.Clear();
                if (value != null)
                    foreach (string str in value)
                        Labels.Add(str);
            }
        }

        private string url = null;
        [field: NonSerializedAttribute()]
        public string Url
        {
            get
            {
                return url ?? string.Empty;
            }
            set
            {
                if (url == value)
                    return;
                
                url = value;
                RaisePropertyChanged("Url");
            }
        }
        [field: NonSerializedAttribute()]
        public string UrlForOrder
        {
            get
            {
                return url == null ? string.Empty : url.Replace("*","");
            }
        }
        
        public Parser()
        {
            Init();
        }

        private void ParentCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Labels")
            {
                RaisePropertyChanged(e.PropertyName);
                IsChanged = true;
            }
        }

        private void Rules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //IsChanged = true;
            //ParseRule rule = sender as ParseRule;
            //if (rule != null)
            //{ 
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var rule in e.NewItems.Cast<ParseRule>())
                    rule.PropertyChanged += Rule_PropertyChanged;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var rule in e.OldItems.Cast<ParseRule>())
                    rule.PropertyChanged -= Rule_PropertyChanged;
            }
            //RaisePropertyChanged("Rules");
            //}
        }

        private void Rule_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
        }

        public void Init()
        {
            if (parentCollection != null)
            { 
                (parentCollection as INotifyPropertyChanged).PropertyChanged -= ParentCollection_PropertyChanged;
                (parentCollection as INotifyPropertyChanged).PropertyChanged += ParentCollection_PropertyChanged;
            }

            Rules.CollectionChanged -= Rules_CollectionChanged;
            Rules.CollectionChanged += Rules_CollectionChanged;
            foreach(var r in Rules)
            {
                r.PropertyChanged -= Rule_PropertyChanged;
                r.PropertyChanged += Rule_PropertyChanged;
            }
        }

        private ObservableCollectionEx<ParseRule> rules = new ObservableCollectionEx<ParseRule>();
        [field: NonSerializedAttribute()]
        public ObservableCollectionEx<ParseRule> Rules
        {
            get
            {
                return rules;
            }
        }

        public ParseResult[] Parse(string[] urls, byte threadCount, string[] labels = null, Action<ParseResult> resultAdded = null, Func<bool> isCanceled = null)
        {
            //threadCount = 1;

            List<ParseResult> result = new List<ParseResult>();
            object lockObject = new Object();
            urls
                .AsParallel()
                .WithDegreeOfParallelism(threadCount == byte.MinValue ? ThreadCount : threadCount)
                .ForAll(
                    (url) =>
                    {
                        if (isCanceled != null && isCanceled())
                            return;

                        try
                        {
                            Dictionary<string, string> dic = new Dictionary<string, string>();

                            var rulesForParse = (labels == null ? Rules : Rules.Where(r => labels.Contains(r.Label))).ToArray();

                            TimeSpan timeToLoad = new TimeSpan(0);
                            TimeSpan timeToParse = new TimeSpan(0);

                            if (rulesForParse.Length > 0 && rulesForParse.Select(r => r.Connection).Distinct().Count() == 1)
                            {
                                string urlResponse;
                                DateTime startLoad = DateTime.Now;
                                HtmlAgilityPack.HtmlDocument document = SiteManager.GetContent(url, rulesForParse.Select(r => r.Connection).Distinct().FirstOrDefault(), out urlResponse);
                                timeToLoad = DateTime.Now - startLoad;

                                DateTime startParse = DateTime.Now;
                                foreach (var rule in rulesForParse)
                                {
                                    dic.Add(rule.Label, rule.Parse(document, new Uri(urlResponse).GetLeftPart(UriPartial.Authority), urlResponse));
                                }
                                timeToParse = DateTime.Now - startParse;
                            } else if (rulesForParse.Length > 0)
                            foreach (var rule in rulesForParse)
                            {
                                string urlResponse;
                                DateTime startLoad = DateTime.Now;
                                HtmlAgilityPack.HtmlDocument document = SiteManager.GetContent(url, rule.Connection, out urlResponse);
                                timeToLoad = DateTime.Now - startLoad;

                                DateTime startParse = DateTime.Now;
                                dic.Add(rule.Label, rule.Parse(document, new Uri(urlResponse).GetLeftPart(UriPartial.Authority), urlResponse));
                                timeToParse = DateTime.Now - startParse;
                            }

                            var res = new ParseResult(url, dic) { Parser = this, TimeToLoadContent = timeToLoad.TotalSeconds, TimeToParse = timeToParse.TotalSeconds };

                            lock (lockObject)
                            {
                                result.Add(res);
                            }

                            if (resultAdded != null)
                                resultAdded(res);
                        }
                        catch (Exception ex)
                        {
                            Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, "Parser.Parse()"));
                            throw new Exception(string.Format("Ошибка при обработке ссылки{0}{1}.{0}Смотрите внутреннее исключение.", Environment.NewLine, url), ex);
                        }
                    }
                );
            return result.ToArray();
        }
        public ParseResult[] Parse(string[] urls, string[] labels = null, Action<ParseResult> resultAdded = null, Func<bool> isCanceled = null)
        {
            return Parse(urls, ThreadCount, labels, resultAdded, isCanceled);
        }

        #region Serialization

        public string Serialize()
        {
            var stringRule = string.Empty;
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                var bytes = stream.ToArray();
                stringRule = System.Convert.ToBase64String(bytes);
            }
            return stringRule;
        }

        public string SerializeXML()
        {
            return this.SerializeToXML();
        }

        public static object Deserialize(string obj, Type type)
        {
            object result = null;
            using (var stream = new MemoryStream(System.Convert.FromBase64String(obj)))
            {
                var formatter = new BinaryFormatter();
                result = formatter.Deserialize(stream);

                if (result is Parser)
                    (result as Parser).Init();
            }
            return result;
        }

        public static Parser DeserializeXML(string obj, Type type)
        {
            Parser result;
            typeof(Parser).DeserializeFromXML(obj, out result);
            return result;
        }

        #endregion
        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (propertyName != "IsChanged")
                IsChanged = true;
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void OnDeserialized()
        {
            Init();
        }
    }
}
