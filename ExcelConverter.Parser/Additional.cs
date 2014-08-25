using Aspose.Cells;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using mshtml;
using System.Reflection;
using Helpers;

namespace ExcelConverter.Parser
{
    public static class InitLogFileStatic
    {
        static InitLogFileStatic()
        {
            if (string.IsNullOrEmpty(Helpers.Log.LogFileName))
                Helpers.Log.LogFileName = "ExcelConverter.Parser.Log.txt";
        }
    }

    public class ParseImageResult
    {
        public readonly HtmlNode node = null;
        public HtmlNode Node
        {
            get
            {
                return node;
            }
        }

        private readonly System.Drawing.Image image = null;
        public System.Drawing.Image Image
        {
            get
            {
                return image;
            }
        }

        private System.Drawing.Size imageSize = new System.Drawing.Size() { Width = 0, Height = 0 };
        public System.Drawing.Size ImageSize
        {
            get
            {
                return Image == null ? imageSize : new System.Drawing.Size() { Width = Image.Width, Height = image.Height };
            }
        }

        private readonly Uri url = null;
        public Uri Url
        {
            get
            {
                return url;
            }
        }

        public bool IsSelected { get; set; }

        public ParseImageResult() { }
        public ParseImageResult(HtmlNode node, System.Drawing.Image image, Uri url)
        {
            this.node = node;
            this.image = image;
            this.url = url;
        }
        public ParseImageResult(HtmlNode node, System.Drawing.Image image, System.Drawing.Size imageSize, Uri url) : this(node, image, url)
        {
            this.imageSize = imageSize;
        }
    }

    public class HtmlNodeWithUrl : INotifyPropertyChanged
    {
        private Uri url = null;
        public Uri Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
                RaisePropertyChanged("Url");
            }
        }

        private HtmlNode node = null;
        public HtmlNode Node
        {
            get
            {
                return node;
            }
            set
            {
                node = value;
                RaisePropertyChanged("Node");
            }
        }

        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public DelegateCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }

    public class URL: INotifyPropertyChanged
    {
        private string url = string.Empty;
        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                url = (new Uri(value)).ToString();
                RaisePropertyChanged(url);
            }
        }

        public URL() { }

        public URL(string url) { this.url = url; }

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public static class CSVReader
    {
        private static string ClearField(string field)
        {
            string result = field;
            if (!string.IsNullOrEmpty(field))
            {
                if (result[0] == '\"')
                    result = result.Remove(0, 1);
                if (result.Length > 0 && result[result.Length - 1] == '\"')
                    result = result.Remove(result.Length - 1, 1);
            }
            return result;
        }

        private static string[] GetCsvFields(string line, string delimiter)
        {
            List<string> result = new List<string>();

            while (true)
            {
                if (line.Length > 0 && line[0] == '"')
                {
                    int ind = -1;
                    if (line.Length > 1)
                        for (int i = 1; i < line.Length; i++)
                            if (line[i] == '"'
                                  && (i == line.Length - 1 || line[i + 1] == delimiter[0])
                                  && (i == 1 || line[i - 1] != '"' || (i > 2 && line[i - 1] == '"' && line[i - 2] == '"'))
                                  && (i == line.Length - 1 || line[i + 1] != '"')
                                )
                            {
                                ind = i;
                                break;
                            }

                    if (ind > 0)
                    {
                        var res = line.Substring(1, ind - 1).Replace("\"\"", "\"");
                        line = line.Remove(0, ind + 1);
                        line = (line == string.Empty) ? null : line.Remove(0, 1);
                        result.Add(res);
                    }
                    else
                    {

                    }
                }
                else
                {
                    var res = ReadUnquotedField(ref line, delimiter);
                    if (res != null)
                        result.Add(res);
                }

                if (line == null)
                    break;
            }

            return result.ToArray();
        }

        private static string ReadUnquotedField(ref string line, string delimiter)
        {
            string result = null;
            if (line != null)
            {
                var ind = line.IndexOf(delimiter);
                if (ind > 0)
                {
                    result = line.Substring(0, ind);
                    line = line.Remove(0, ind + 1);
                }
                else if (ind == 0)
                {
                    line = line.Remove(0, ind + 1);
                    result = string.Empty;
                }
                else
                {
                    result = line;
                    line = null;
                }

                //if (ind < 0 && line == string.Empty && result == string.Empty)
                //    result = null;
            }
            return result;
        }

        public class GetDataTableResult
        {
            public GetDataTableResult() { }
            public int ImportedRowCount = 0;
            public int TotalRowCount = 0;
        }

        public static DataTable Read(string filePath, out GetDataTableResult importResult, string[] mustExistsConstraints, bool hasColumns = true, string name = "CSVFile", string delimiter = ";")
        {
            importResult = new GetDataTableResult();
            importResult.ImportedRowCount = 0;
            importResult.TotalRowCount = 0;
            delimiter = string.IsNullOrEmpty(delimiter) ? ";" : delimiter;
            DataTable result = new DataTable(name);

            string[] Lines = File.ReadAllLines(filePath, Encoding.Default);
            DataRow Row = null;
            string[] Columns = null;
            string[] Fields = null;

            try
            {
                foreach (string line in Lines)
                {
                    Fields = GetCsvFields(line, delimiter);
                    if (Columns == null && hasColumns)
                    {
                        int i = 0;
                        Columns = Fields;
                        foreach (string col in Columns)
                        {
                            string col_name = ClearField(col).ToLower();
                            if (string.IsNullOrEmpty(col_name))
                                col_name = string.Format("неизвестно_{0}", i);
                            result.Columns.Add(col_name, typeof(string));
                            i++;
                        }

                        if (mustExistsConstraints != null)
                            foreach (string sys_column_name in mustExistsConstraints)
                                if (result.Columns.IndexOf(sys_column_name) < 0)
                                    throw new Exception(string.Format("В импортируемом файле отсутствует обязательная колонка \"{0}\"", sys_column_name));
                    }
                    else
                    {
                        if (Columns == null)
                        {
                            Columns = Fields;
                            for (int i = 0; i < Columns.Length; i++)
                                result.Columns.Add(string.Format("column_{0}", i), typeof(string));
                        }

                        importResult.TotalRowCount++;
                        Row = result.NewRow();
                        for (int i = 0; i < result.Columns.Count; i++)
                            if (i < Fields.Length)
                                Row[result.Columns[i]] = ClearField(Fields[i]);

                        bool all_constraints_ok = true;
                        if (hasColumns && mustExistsConstraints != null)
                        {
                            var badColumns = mustExistsConstraints.Where(c => Row[c] == DBNull.Value || string.IsNullOrEmpty(Row[c].ToString()));
                            all_constraints_ok = badColumns.Count() == 0;
                        }
                        if (all_constraints_ok)
                        {
                            importResult.ImportedRowCount++;
                            result.Rows.Add(Row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
    }

    public static class CSVWriter
    {
        public static void Write(DataTable table, string filePath, Encoding encoding, bool hasColumns = true, string[] excludeColumns = null, string delimiter = ";")
        {
            int totalRowOuted = 0;
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
                try
                {
                    WritePreamble(fs, encoding);
                    string line = string.Empty;

                    if (hasColumns)
                    {
                        for (int i = 0; i < table.Columns.Count; i++)
                            if (excludeColumns == null || !excludeColumns.Contains(table.Columns[i].ColumnName))
                            {
                                string column_name = table.Columns[i].ColumnName;
                                line += (string.IsNullOrEmpty(line) ? string.Empty : delimiter) + WrapInQuotesIfContains(column_name, new string[] { delimiter, "\"" });
                            }
                        AddLineToStream(fs, encoding, ref line);
                    }

                    foreach (DataRow row in table.Rows)
                    {
                        for (int i = 0; i < table.Columns.Count; i++)
                            if (excludeColumns == null || !excludeColumns.Contains(table.Columns[i].ColumnName))
                                line += (string.IsNullOrEmpty(line) ? string.Empty : delimiter) + WrapInQuotesIfContains(row[i].ToString(), new string[] { delimiter, "\"" });
                        AddLineToStream(fs, encoding, ref line);
                        totalRowOuted++;
                    }
                }
                finally
                {
                    fs.Flush();
                }
        }

        private static void WritePreamble(Stream stream, Encoding encoding)
        {
            byte[] preamble = encoding.GetPreamble();
            if (preamble.Length > 0)
                stream.Write(preamble, 0, preamble.Length);
        }
        private static void AddLineToStream(Stream stream, Encoding encoding, ref string line)
        {
            byte[] string_arr = encoding.GetBytes(line + Environment.NewLine);
            stream.Write(string_arr, 0, string_arr.Length);
            line = string.Empty;
        }
        private static string WrapInQuotesIfContains(string inString, string[] inSearchString)
        {
            return (inSearchString.Any(s => inString.Contains(s)) ? "\"" + inString.Replace("\"", "\"\"") + "\"" : inString);
        }
    }

    public class SiteManagerCompletedEventArgs : EventArgs
    {
        public string Content { get; set; }
        public Uri ResponseUri { get; set; }
    }

    internal class SiteManagerParams
    {
        public Uri Url { get; set; }
        public SiteManagerCompletedEventArgs Result { get; set; }
    }

    public class SiteManager
    {
        protected class SiteManagerIE : SiteManager
        {
            public SiteManagerCompletedEventArgs Navigate(Uri url, int wait = 0)
            {
                SiteManagerCompletedEventArgs result = new SiteManagerCompletedEventArgs() { ResponseUri = url };
                bool done = false;
                System.Windows.Controls.WebBrowser wb = null;

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        PutControl(wb = new System.Windows.Controls.WebBrowser());

                        wb.Navigated += (s, e) =>
                        {
                            result.ResponseUri = e.Uri;
                            HideScriptErrors(s as System.Windows.Controls.WebBrowser, true);
                        };

                        wb.LoadCompleted += (s, e) =>
                        {
                            result.ResponseUri = e.Uri;
                            done = true;
                        };

                        wb.Navigate(url);
                    }));

                #region Wait (done and pause) or 30 sec
                DateTime endTime = DateTime.Now.AddSeconds(30);
                while (!done && (DateTime.Now < endTime))
                    Thread.Sleep(100);

                if (done)
                    Wait(wait);
                #endregion

                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        try
                        {
                            mshtml.HTMLDocument doc = wb.Document as mshtml.HTMLDocument;
                            if (doc != null && doc.documentElement != null)
                                result.Content = doc.documentElement.innerHTML;
                        }
                        catch (Exception ex)
                        {
                            Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, "SiteManagerIE.Navigate().GetHTML()"));
                        }
                        finally
                        {
                            RemoveControl(wb);
                            wb = null;
                            GC.Collect();
                        }
                    }));

                return result;
            }
            private void HideScriptErrors(System.Windows.Controls.WebBrowser wb, bool Hide)
            {

                FieldInfo fiComWebBrowser = typeof(System.Windows.Controls.WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fiComWebBrowser == null) return;
                object objComWebBrowser = fiComWebBrowser.GetValue(wb);
                if (objComWebBrowser == null) return;

                objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
            }
        }

        protected class SiteManagerCHR : SiteManager
        {
            private static bool inited = false;
            public static void Init()
            {
                if (!inited)
                    try
                    {
                        string resources = Path.Combine(Directory.GetCurrentDirectory(), "cache");

                        if (Directory.Exists(resources))
                            Directory.Delete(resources, true);

                        Directory.CreateDirectory(resources);

                        string logFile = Path.Combine(Directory.GetCurrentDirectory(), "chromelog.txt");
                        if (File.Exists(logFile))
                            File.Delete(logFile);

                        CefSharp.Settings settings = new CefSharp.Settings() 
                        {
                            CachePath = resources,
                            Locale = "ru",
                            LogFile = logFile
                        };
                        inited = CefSharp.CEF.Initialize(settings);
                    }
                    catch (Exception ex)
                    {
                        Log.Add(string.Format("SiteManagerCHR.Init() :: exception '{0}'", ex.Message));
                    }
            }

            public SiteManagerCompletedEventArgs Navigate(Uri url, int wait = 0)
            {
                if (!inited)
                    throw new Exception("SiteManagerCHR not initialized");

                SiteManagerCompletedEventArgs result = new SiteManagerCompletedEventArgs() { ResponseUri = url };
                bool done = false;
                CefSharp.Wpf.WebView wv = null;

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        try
                        {
                            PutControl(wv = new CefSharp.Wpf.WebView()); //string.Empty, new CefSharp.BrowserSettings() { EncodingDetectorEnabled = true }
                            wv.LoadCompleted += (s, e) =>
                            {
                                CefSharp.Wpf.WebView s1 = s as CefSharp.Wpf.WebView;
                                if (s1.Address == e.Url)
                                {
                                    result.ResponseUri = new Uri(e.Url);
                                    object contentScriptResult = s1.EvaluateScript(@"document.getElementsByTagName ('html')[0].innerHTML");
                                    if (contentScriptResult != null)
                                        result.Content = contentScriptResult.ToString();
                                    done = true;
                                }
                            };
                            //wv.Load(url.AbsoluteUri);
                            wv.Address = url.AbsoluteUri;
                        }
                        catch(Exception ex)
                        {
                            Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, "SiteManagerCHR.Navigate().PutControl()"));
                        }
                    }));

                #region Wait (done and pause) or 30 sec
                DateTime endTime = DateTime.Now.AddSeconds(30);
                while (!done && (DateTime.Now < endTime))
                    Thread.Sleep(100);

                if (done)
                    Wait(wait);
                #endregion
                #region GetHTML
                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        if (wv != null)
                        try
                        {
                            if (wv.IsInitialized)
                            { 
                                object contentScriptResult = wv.EvaluateScript(@"document.getElementsByTagName ('html')[0].innerHTML");
                                if (contentScriptResult != null)
                                    result.Content = contentScriptResult.ToString();
                            }
                        }
                        catch(Exception ex)
                        {
                            Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, "SiteManagerCHR.Navigate().GetHTML()"));
                        }
                        finally
                        {
                            wv.Delete();
                            RemoveControl(wv);
                            wv = null;
                            GC.Collect();
                        }
                    }));
                #endregion

                return result;
            }
        }

        private static System.Windows.Controls.Panel ParentControl = null;
        protected static System.Windows.Threading.Dispatcher Dispatcher = null;

        protected static bool IsInited { get { return ParentControl != null && Dispatcher != null; } }

        public static void Init(System.Windows.Controls.Panel parentControl, System.Windows.Threading.Dispatcher dispatcher)
        {
            if (!IsInited)
                SiteManagerCHR.Init();
            
            ParentControl = parentControl;
            Dispatcher = dispatcher;
        }
        public static HtmlAgilityPack.HtmlDocument GetContent(string url, ParseRuleConnectionType type, out string urlResponse)
        {
            HtmlAgilityPack.HtmlDocument document = null;
            urlResponse = url;

            if (IsInited)
            {
                if (type == ParseRuleConnectionType.Direct)
                {
                    HtmlWeb htmlWeb = new HtmlWeb() { AutoDetectEncoding = true, UserAgent = "Other" };
                    document = htmlWeb.Load(url);
                    if (document.StreamEncoding != document.Encoding)
                    {
                        htmlWeb.AutoDetectEncoding = false;
                        htmlWeb.OverrideEncoding = document.Encoding;
                        document = htmlWeb.Load(url);
                    }
                    urlResponse = htmlWeb.ResponseUri.AbsoluteUri;
                }
                else if (new ParseRuleConnectionType[] { ParseRuleConnectionType.IE_00_sec, ParseRuleConnectionType.IE_05_sec, ParseRuleConnectionType.IE_10_sec }.Contains(type))
                {
                    string waitSeconds = type.GetType().GetEnumName(type);
                    waitSeconds = waitSeconds.Substring(0, waitSeconds.LastIndexOf("_"));
                    waitSeconds = waitSeconds.Substring(waitSeconds.IndexOf("_") + 1);
                    int wait;
                    if (int.TryParse(waitSeconds, out wait))
                    {
                        SiteManagerIE mgr = new SiteManagerIE();
                        var res = mgr.Navigate(new Uri(url), wait);
                        document = new HtmlDocument();
                        document.LoadHtml(res.Content);
                        urlResponse = res.ResponseUri.AbsoluteUri;
                    }
                }
                else if (new ParseRuleConnectionType[] { ParseRuleConnectionType.CHR_00_sec, ParseRuleConnectionType.CHR_05_sec, ParseRuleConnectionType.CHR_10_sec }.Contains(type))
                {
                    string waitSeconds = type.GetType().GetEnumName(type);
                    waitSeconds = waitSeconds.Substring(0, waitSeconds.LastIndexOf("_"));
                    waitSeconds = waitSeconds.Substring(waitSeconds.IndexOf("_") + 1);
                    int wait;
                    if (int.TryParse(waitSeconds, out wait))
                    {
                        SiteManagerCHR mgr = new SiteManagerCHR();
                        var res = mgr.Navigate(new Uri(url), wait);
                        document = new HtmlDocument();
                        document.LoadHtml(res.Content);
                        urlResponse = res.ResponseUri.AbsoluteUri;
                    }
                }
            }
            else
                throw new Exception("SiteManager not inited. Use SiteManager.Init() to initialize components");

            return document;
        }
        protected void PutControl(System.Windows.FrameworkElement control)
        {
            if (ParentControl != null)
            {
                control.Visibility = System.Windows.Visibility.Hidden;
                //control.Opacity = 0;
                control.Width = 1024;
                control.Height = 1024;
                ParentControl.Children.Add(control);
            }
        }
        protected void RemoveControl(System.Windows.FrameworkElement control)
        {
            if (control != null)
            {
                if (ParentControl != null && ParentControl.Children.Contains(control))
                    ParentControl.Children.Remove(control);

                IDisposable disp = control as IDisposable;
                if (disp != null)
                    disp.Dispose();
            }
        }
        protected void Wait(int waitSeconds)
        {
            DateTime endTime = DateTime.Now.AddSeconds(Math.Abs(waitSeconds));
            while (DateTime.Now < endTime)
                Thread.Sleep(100);
        }
        internal static void GetFile(Uri fileUrl, string tempFileName)
        {
            GetFile(ParseRuleConnectionType.Direct, fileUrl, tempFileName);
        }
        internal static void GetFile(ParseRuleConnectionType type, Uri fileUrl, string tempFileName)
        {
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent: Other");
                wc.DownloadFile(fileUrl, tempFileName);
            }
        }
    }


    public static class Helper
    {
        public static void DoEvents()
        {
            System.Windows.Threading.DispatcherFrame frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(ExitFrame), frame);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        private static object ExitFrame(object f)
        {
            ((System.Windows.Threading.DispatcherFrame)f).Continue = false;
            return null;
        }

        public static string LongestCommonSubstringLength(string str1, string str2)
        {
            if (String.IsNullOrEmpty(str1) || String.IsNullOrEmpty(str2))
                return string.Empty;

            List<int[]> num = new List<int[]>();
            int maxlen = 0;
            for (int i = 0; i < str1.Length; i++)
            {
                num.Add(new int[str2.Length]);
                for (int j = 0; j < str2.Length; j++)
                {
                    if (str1[i] != str2[j])
                        num[i][j] = 0;
                    else
                    {
                        if ((i == 0) || (j == 0))
                            num[i][j] = 1;
                        else
                            num[i][j] = 1 + num[i - 1][j - 1];
                        if (num[i][j] > maxlen)
                            maxlen = num[i][j];
                    }
                    if (i >= 2)
                        num[i - 2] = null;
                }
            }
            return str1.Substring(0, maxlen);
        }

        public static Uri GetFullSourceLink(string sourceLink, HtmlDocument doc, string stdBase)
        {
            Uri result = null;
            if (Helper.IsWellFormedUriString(sourceLink, UriKind.Relative))
            {
                Uri baseUri = 
                    new Uri(
                        doc.DocumentNode
                        .Descendants("base")
                        .Where(n => n.HasAttributes && n.Attributes.Contains("href"))
                        .Select(n => n.Attributes["href"].Value)
                        .FirstOrDefault() ?? stdBase, UriKind.Absolute);

                if (baseUri.AbsoluteUri.LastIndexOf(sourceLink) == baseUri.AbsoluteUri.Length - sourceLink.Length)
                    result = baseUri; else
                    result = new Uri(baseUri, sourceLink);
            }
            else if (Helper.IsWellFormedUriString(sourceLink, UriKind.Absolute))
            {
                result = new Uri(sourceLink);
            }
            return result;
        }

        public static Uri GetFullSourceLink(Uri sourceLink, HtmlDocument doc, Uri stdBase)
        {
            if (sourceLink.IsAbsoluteUri)
                return sourceLink;
            else
            {
                Uri baseUri = new Uri(doc.DocumentNode.Descendants("base").Where(n => n.HasAttributes && n.Attributes.Contains("href")).Select(n => n.Attributes["href"].Value).FirstOrDefault() 
                    ?? stdBase.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
                return new Uri(baseUri, sourceLink);
            }
        }

        #region Images stuff

        private const int readBytesForDefault = 30;
        private const int readBytesForJpeg = 1024; //max size can be 65K*65K... so big :(

        private static byte[] pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static byte[] bmpSignature = new byte[] { 0x42, 0x4D };
        private static byte[] jpg1Signature = new byte[] { 0xff, 0xd8, 0xff, 0xe0 }; //jpeg JFIF
        private static byte[] jpg2Signature = new byte[] { 0xff, 0xd8, 0xff, 0xe1 }; //jpeg EXIF
        private static byte[] gif1Signature = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };
        private static byte[] gif2Signature = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };

        private static System.Drawing.Size GetImageSize(string directSourceLink, int maxDownloadBytes)
        {
            System.Drawing.Size result = new System.Drawing.Size(0, 0);
            try
            {
                HttpWebRequest wReq = (HttpWebRequest)WebRequest.Create(directSourceLink);

                byte[] buffer = new byte[maxDownloadBytes];
                wReq.UserAgent = "Other";
                wReq.AddRange(0, buffer.Length);

                using (WebResponse wRes = wReq.GetResponse())
                using (Stream stream = wRes.GetResponseStream())
                {
                    stream.Read(buffer, 0, buffer.Length);
                    #region PNG
                    if (buffer.Take(pngSignature.Length).SequenceEqual(pngSignature))
                    {
                        var idhr = buffer.Skip(12);
                        result.Width = BitConverter.ToInt32(idhr.Skip(4).Take(4).Reverse().ToArray(), 0);
                        result.Height = BitConverter.ToInt32(idhr.Skip(8).Take(4).Reverse().ToArray(), 0);
                    }
                    #endregion
                    #region BMP
                    else if (buffer.Take(bmpSignature.Length).SequenceEqual(bmpSignature))
                    {
                        var idhr = buffer.Skip(18);
                        result.Width = BitConverter.ToInt32(idhr.Skip(0).Take(4).Reverse().ToArray(), 0);
                        result.Height = BitConverter.ToInt32(idhr.Skip(4).Take(4).Reverse().ToArray(), 0);
                    }
                    #endregion
                    #region JPG JFIF
                    else if (buffer.Take(jpg1Signature.Length).SequenceEqual(jpg1Signature))
                    {
                        if (maxDownloadBytes < readBytesForJpeg)
                        {
                            wRes.Close();
                            result = GetImageSize(directSourceLink, readBytesForJpeg);
                        }
                        else
                        {
                            using (MemoryStream fs = new MemoryStream(buffer))
                            {
                                //skip signature
                                fs.Seek(4, SeekOrigin.Begin);
                                var buf = new byte[4];
                                long blockStart = fs.Position;
                                fs.Read(buf, 0, 2);
                                var blockLength = ((buf[0] << 8) + buf[1]);
                                fs.Read(buf, 0, 4);

                                if (Encoding.ASCII.GetString(buf, 0, 4) == "JFIF" && fs.ReadByte() == 0)
                                {
                                    blockStart += blockLength;
                                    while (blockStart < fs.Length)
                                    {
                                        fs.Position = blockStart;
                                        fs.Read(buf, 0, 4);
                                        blockLength = ((buf[2] << 8) + buf[3]);
                                        if (blockLength >= 7 && buf[0] == 0xff && buf[1] == 0xc0)
                                        {
                                            fs.Position += 1;
                                            fs.Read(buf, 0, 4);
                                            result.Height = (buf[0] << 8) + buf[1];
                                            result.Width = (buf[2] << 8) + buf[3];
                                            break;
                                        }
                                        blockStart += blockLength + 2;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    #region JPG EXIF
                    else if (buffer.Take(jpg2Signature.Length).SequenceEqual(jpg2Signature))
                    {
                        if (maxDownloadBytes < readBytesForJpeg)
                        {
                            wRes.Close();
                            result = GetImageSize(directSourceLink, readBytesForJpeg);
                        }
                        else
                        {
                            using (MemoryStream fs = new MemoryStream(buffer))
                            {
                                //skip signature
                                fs.Seek(4, SeekOrigin.Begin);
                                var buf = new byte[4];
                                long blockStart = fs.Position;
                                fs.Read(buf, 0, 2);
                                var blockLength = ((buf[0] << 8) + buf[1]);
                                fs.Read(buf, 0, 4);

                                if (maxDownloadBytes >= blockLength + 10)
                                {
                                    if (Encoding.ASCII.GetString(buf, 0, 4).ToUpper() == "EXIF" && fs.ReadByte() == 0)
                                    {
                                        using (MemoryStream ms = new MemoryStream(buffer))
                                        using (ExifLib.ExifReader reader = new ExifLib.ExifReader(ms))
                                        {
                                            System.UInt32 width;
                                            System.UInt32 height;
                                            reader.GetTagValue(ExifLib.ExifTags.PixelXDimension, out width);
                                            reader.GetTagValue(ExifLib.ExifTags.PixelYDimension, out height);
                                            result.Height = (int)height;
                                            result.Width = (int)width;
                                        }
                                    }
                                }
                                else
                                {
                                    wRes.Close();
                                    result = GetImageSize(directSourceLink, blockLength + 10);
                                }
                            }
                        }
                    }
                    #endregion
                    #region GIF
                    else if (buffer.Take(gif1Signature.Length).SequenceEqual(gif1Signature) || buffer.Take(gif2Signature.Length).SequenceEqual(gif2Signature))
                    {
                        var idhr = buffer.Skip(gif1Signature.Length);
                        result.Width = idhr.ElementAt(0) | idhr.ElementAt(1) << 8;
                        result.Height = idhr.ElementAt(2) | idhr.ElementAt(3) << 8;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.GetExceptionText(ex, string.Format("Helper.GetImageSize('{0}',maxDownloadBytes:'{1}')", directSourceLink, maxDownloadBytes)));
            }
            return result;
        }

        public static System.Drawing.Size GetImageSize(string directSourceLink)
        {
            return GetImageSize(directSourceLink, readBytesForDefault);
        }

        public static System.Drawing.Size GetImageSize(string directSourceLink, bool tryToDownloadImage = true)
        {
            var size = GetImageSize(directSourceLink);
            if (tryToDownloadImage && (size.Width * size.Height == 0))
            {
                string fileName = new Uri(directSourceLink).AbsolutePath.Split(new[] { '/' }).Last();
                string tempFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + System.IO.Path.GetExtension(fileName);
                try
                {
                    SiteManager.GetFile(new Uri(directSourceLink), tempFileName);
                    using (System.Drawing.Image image = System.Drawing.Image.FromFile(tempFileName))
                    {
                        size.Height = image.Height;
                        size.Width = image.Width;
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(Log.GetExceptionText(ex, string.Format("Helper.GetImageSize('{0}','{1}')",directSourceLink, tryToDownloadImage)));
                }
                finally
                {
                    try
                    { System.IO.File.Delete(tempFileName); }
                    catch { }
                }
            }

            return size;
        }

        public static bool CheckImageSize(System.Drawing.Size size, System.Drawing.Size minSize, bool allowNull = true)
        {
            return ((size.Width * size.Height == 0 && allowNull) || 
                (
                    size.Width >= (int)((float)minSize.Width * 0.9)
                    && size.Height >= (int)((float)minSize.Height * 0.9))
                    && size.Width * size.Height >= minSize.Height * minSize.Width
                );
        }

        public static bool CheckImageSize(string directSourceLink, System.Drawing.Size minSize, bool allowNull = true, bool tryToDownloadImage = true)
        {
            var size = GetImageSize(directSourceLink, tryToDownloadImage);
            return CheckImageSize(size, minSize, allowNull);
        }

        public static bool CheckImageSize(string directSourceLink, System.Drawing.Size minSize, out System.Drawing.Size size, bool allowNull = true, bool tryToDownloadImage = true)
        {
            size = GetImageSize(directSourceLink, tryToDownloadImage);
            return CheckImageSize(size, minSize, allowNull);
        }

        #endregion

        public static string[] GetLinksFromFile(string fileName, bool additional = false, bool distinct = true)
        {
            List<string> result = new List<string>();
            object lockObject = new Object();
            #region CSV
            if (Path.GetExtension(fileName).ToLower() == ".csv")
            {
                //is CSV file
                CSVReader.GetDataTableResult importResult;
                using (DataTable csvFile = CSVReader.Read(fileName, out importResult, new string[] { }))
                {
                    csvFile.Rows
                        .Cast<DataRow>()
                        .ToArray()
                        .AsParallel()
                        .Select(
                            (row) =>
                            {
                                List<Uri> uries = new List<Uri>();
                                uries.AddRange(
                                csvFile
                                    .Columns
                                    .Cast<DataColumn>()
                                    .Where(
                                        c =>
                                            row[c] != null
                                            && row[c] != DBNull.Value
                                            && !string.IsNullOrWhiteSpace(row[c].ToString())
                                            && Helper.IsWellFormedUriString(row[c].ToString(), UriKind.Absolute)
                                        )
                                    .Select(c => new Uri(row[c].ToString()))
                                    );
                                return uries.ToArray();
                            }
                        ).Cast<Uri[]>()
                        .ForAll
                        (
                            (uries) => 
                            {
                                if (uries != null && uries.Length > 0)
                                lock (lockObject)
                                {
                                    result.AddRange(uries.Select( u => u.AbsoluteUri ).ToArray());
                                }
                            }
                        );
                }
            }
            #endregion
            #region EXCEL
            else
            {
                //is XLS(X) file
                Workbook wb = new Workbook(fileName);
                wb.Worksheets.Cast<Worksheet>().AsParallel().ForAll
                    (
                        (sheet) =>
                            {
                                string[] urls = 
                                    sheet
                                    .Hyperlinks
                                    .Cast<Hyperlink>()
                                    .AsParallel()
                                    .Select(h => h.Address)
                                    .Where(l => Helper.IsWellFormedUriString(l, UriKind.RelativeOrAbsolute))
                                    .ToArray();
                                lock (lockObject)
                                {
                                    result.AddRange(urls);
                                }

                                if (additional)
                                {
                                    int colCount = sheet.Cells.Columns.Count;
                                    sheet.Cells.Rows.Cast<Row>().AsParallel().WithDegreeOfParallelism(1).ForAll(
                                        (row) =>
                                            {
                                                for (int i = 0; i < colCount; i++)
                                                    {
                                                        Cell cell = row.GetCellOrNull(i);
                                                        if (cell != null)
                                                        {
                                                            if (cell.IsFormula)
                                                            {
                                                                string formula = cell.Formula;
                                                                const string hLink = "hyperlink(";
                                                                if (formula.ToLower().Contains(hLink))
                                                                {
                                                                    formula = formula.Substring(formula.ToLower().IndexOf(hLink) + hLink.Length);
                                                                    if (!string.IsNullOrWhiteSpace(formula))
                                                                    {
                                                                        if (formula[0] == '\"')
                                                                        {
                                                                            formula = formula.Substring(1);
                                                                            if (formula.IndexOf("\"") - 1 > 0)
                                                                            { 
                                                                                formula = formula.Substring(0, formula.IndexOf("\""));
                                                                                if (Helper.IsWellFormedUriString(formula, UriKind.Absolute))
                                                                                    lock(lockObject)
                                                                                    {
                                                                                        result.Add(formula);
                                                                                    }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                            }
                                        );
                                }
                            }
                    );
            }
            #endregion
            string[] res = distinct ? result.Distinct().ToArray() : result.ToArray();
            return res;
        }

        public static string[] GetSomeUrlsForHost(string host, Controls.UrlCollection Urls, int count = 0)
        {
            List<string> result = new List<string>();
            try
            {
                List<string> fullResult = new List<string>();
                fullResult.AddRange(
                    Urls
                        .AsParallel()
                        .Where(url => Helper.IsWellFormedUriString(url.Value, UriKind.Absolute) && StringLikes((new Uri(url.Value)).Host, host))
                        .Select(i => i.Value).ToArray()
                        .OrderBy(i => i)
                        );
                if (count > 0 && count < fullResult.Count)
                {
                    count = Math.Min(count, fullResult.Count);
                    if (count > 0)
                        result.Add(fullResult.First());
                    if (count > 1)
                        result.Add(fullResult.Last());
                    if (count > 2)
                    {
                        int step = (int)((float)fullResult.Count / (float)(count - 1));
                        for (int i = 1; i < count - 1; i++)
                            result.Add(fullResult[step * i]);
                    }
                }
                else
                    result.AddRange(fullResult);
            }
            catch (Exception ex)
            {
                Log.Add(Log.GetExceptionText(ex, string.Format("Helper.GetSomeUrlsForHost(host:'{0}',count:'{1}')", host, count)));
            }
            return result.OrderBy(i => i).ToArray();
        }

        public static bool IsWellFormedUriString(string uriString, UriKind uriKind)
        {
            if ((new UriKind[] { UriKind.Absolute, UriKind.RelativeOrAbsolute }).Contains(uriKind) && string.IsNullOrWhiteSpace(uriString))
                return false;

            Uri uri;
            return Uri.TryCreate(uriString, uriKind, out uri); // && uri.IsWellFormedOriginalString()
        }

        internal class SomeNodeElement
        {
            public HtmlNode Node { get; set; }
            public Uri Url { get; set; }
        }
        internal static SomeNodeElement[] GetAllImagesUrlsFromUrl(HtmlAgilityPack.HtmlDocument document, Uri url, bool collectIMGTags, bool collectLINKTags, bool collectMETATags)
        {
            return GetAllImagesUrlsFromUrl(document, url.AbsoluteUri, collectIMGTags, collectLINKTags, collectMETATags);
        }
        internal static SomeNodeElement[] GetAllImagesUrlsFromUrl(HtmlAgilityPack.HtmlDocument document, string url, bool collectIMGTags, bool collectLINKTags, bool collectMETATags)
        {
            if (document == null)
                throw new ArgumentNullException("В функции GetAllImagesUrlsFromUrl() не может отсутствовать обязательный параметр document");

            bool wasException = false;
            var logSession = Helpers.Log.SessionStart("Additional.GetAllImagesUrlsFromUrl()", true);
            try
            {
                var allIMGLinks = 
                        !collectIMGTags ? new SomeNodeElement[] {} :
                        document
                        .DocumentNode
                        .Descendants("img")
                        .Where(n => 
                            n.Attributes.Contains("src") && 
                            Helper.IsWellFormedUriString(n.Attributes["src"].Value, UriKind.RelativeOrAbsolute))
                        .Select(n => new SomeNodeElement() { Node = n, Url = Helper.GetFullSourceLink(n.Attributes["src"].Value, document, url) })
                        .ToArray();
            
                //add some links (<a href="img_source"/>)
                var allLINKLinks =
                        !collectLINKTags ? new SomeNodeElement[] { } :
                        document
                        .DocumentNode
                        .Descendants("a")
                        .Where(n => n.Attributes.Contains("href"))
                        .Where(n =>
                        {
                            string href = n.Attributes["href"].Value;
                            string[] likes = new string[] { "*.jpeg","*.jpg",  "*.bmp", "*.gif" };
                            return likes.Any(i => Helper.StringLikes(href, i)) && Helper.IsWellFormedUriString(href, UriKind.RelativeOrAbsolute);
                        })
                        .Select(n => new SomeNodeElement()
                        { 
                            Node = n, 
                            Url = Helper.GetFullSourceLink(n.Attributes["href"].Value, document, url)
                        })
                        .ToArray();

                var allMETALinks = new SomeNodeElement[] { };

                if (collectMETATags)
                if (document.DocumentNode != null && document.DocumentNode.FirstChild != null && !string.IsNullOrWhiteSpace(document.DocumentNode.InnerHtml))
                {
                    string regExString = "(https?:)?//?[^\'\"<>]+?\\.(jpg|jpeg|gif|png|bmp)";// @"/((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)$";
                    foreach (Match m in Regex.Matches(document.DocumentNode.InnerHtml, regExString)) //"(\\S+?)\\.(jpg|png|gif|jpeg|bmp)"
                    {
                        string value = m.Value;
                        while (value.IndexOf("(") >= 0)
                            value = value.Substring(value.IndexOf("(")+1);

                        string http = "http:";
                        if (value.Like("*" + http + "*"))
                            value = value.Substring(value.IndexOf(http));

                        string https = "https:";
                        if (value.Like("*" + https + "*"))
                            value = value.Substring(value.IndexOf(https));

                        value = value.Replace(@"\/","/");

                        if (Helper.IsWellFormedUriString(value, UriKind.RelativeOrAbsolute))
                            try
                            {
                                Uri newUri = Helper.GetFullSourceLink(value, document, url);
                                    allMETALinks = allMETALinks.Union(new SomeNodeElement[] { new SomeNodeElement() { Node = document.DocumentNode, Url = newUri } }).ToArray();
                            }
                            catch(Exception)
                            {

                            }
                    }
                }

                var allLinks =
                        allIMGLinks
                        .Union(allLINKLinks)
                        .Union(allMETALinks)
                        //.Distinct()
                        .GroupBy(ne => ne.Url)
                        .Select(neg => neg.First())
                        .ToArray();

                if (allLinks.Length == 0 && string.IsNullOrWhiteSpace(document.DocumentNode.InnerText))
                    allLinks = new SomeNodeElement[] { new SomeNodeElement() { Node = document.DocumentNode, Url = new Uri(url) } };

                //List<SomeNodeElement> distinctedLinks = new List<SomeNodeElement>(allLinks);
                //for (int i = distinctedLinks.Count - 1; i >= 0; i--)
                //{
                //    int lnToDel = distinctedLinks.Count(i2 => i2.Url.AbsoluteUri == distinctedLinks[i].Url.AbsoluteUri && i2 != distinctedLinks[i]);
                //    if (lnToDel > 0)
                //        distinctedLinks.RemoveAt(i);
                //}
                //return distinctedLinks.ToArray();
                return allLinks;
            }
            catch(Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex);
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException);
            }
        }
        internal static SomeNodeElement[] GetAllImagesUrlsWithMinSize(SomeNodeElement[] items, System.Drawing.Size minSize)
        {
            return GetImageSizes(items).Where( n => Helper.CheckImageSize(n.Value, minSize, true)).Select( n => n.Key).ToArray();
        }
        internal static Dictionary<SomeNodeElement, System.Drawing.Size> GetImageSizes(SomeNodeElement[] items)
        {
            Dictionary<SomeNodeElement, System.Drawing.Size> result = new Dictionary<SomeNodeElement, Size>();
            foreach(var n in items)
            {
                System.Drawing.Size sz;

                int width, height;
                if (n.Node.HasAttributes
                    && n.Node.Attributes.Contains("Width")
                    && n.Node.Attributes.Contains("Height")
                    && int.TryParse(n.Node.Attributes["Width"].Value, out width)
                    && int.TryParse(n.Node.Attributes["Height"].Value, out height)
                    )
                    sz = new System.Drawing.Size() { Width = width, Height = height };
                else
                    sz = Helper.GetImageSize(n.Url.AbsoluteUri, true);

                result.Add(n, sz);
            }
            return result;
        }
        internal static void SetImageSize(HtmlNode node, System.Drawing.Size size)
        {
            if (node.HasAttributes && node.Attributes.Contains("Width"))
                node.Attributes["Width"].Value = size.Width.ToString();
            else
                node.Attributes.Add("Width", size.Width.ToString());

            if (node.HasAttributes && node.Attributes.Contains("Height"))
                node.Attributes["Height"].Value = size.Height.ToString();
            else
                node.Attributes.Add("Height", size.Height.ToString());
        }
        public static ParseImageResult[] GetAllImagesFromUrl(
            string url,
            System.Drawing.Size minSize, 
            bool collectIMGTags,
            bool collectLINKTags,
            bool collectMETATags,
            int threadCount = 6, 
            Helpers.PercentageProgress prgItem = null, 
            bool downloadImages = false, 
            ParseRuleConnectionType type = ParseRuleConnectionType.Direct)
        {
            List<ParseImageResult> result = new List<ParseImageResult>();
            try
            {
                Helpers.PercentageProgress prgItemPage = null;
                Helpers.PercentageProgress prgItemImg = null;
                if (prgItem != null)
                {
                    prgItemPage = prgItem.GetChild();
                    prgItemImg = prgItem.GetChild();
                }

                HtmlAgilityPack.HtmlDocument document = SiteManager.GetContent(url, type, out url);

                if (prgItemPage != null)
                    prgItemPage.Value = 100;

                //threadCount = 6;

                object lockAdd = new Object();

                var allLinks = GetAllImagesUrlsFromUrl(document, url, collectIMGTags, collectLINKTags, collectMETATags);
                int fullCnt = allLinks.Count();
                int currLoaded = 0;

                object currLoadedLock = new Object();

                allLinks
                    .AsParallel()
                    .WithDegreeOfParallelism(threadCount)
                    .ForAll(node =>
                    {
                        Uri fileUrl = node.Url;// Helper.GetFullSourceLink(node.Url, document, url);
                        try
                        {
                            System.Drawing.Size imageSize;
                            if (Helper.CheckImageSize(fileUrl.ToString(), minSize, out imageSize, true, !downloadImages))
                            {
                                if (!imageSize.IsEmpty)
                                    SetImageSize(node.Node, imageSize);

                                string fileName = fileUrl.AbsolutePath.Split(new[] { '/' }).Last();
                                string tempFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + System.IO.Path.GetExtension(fileName);

                                if (downloadImages)
                                    SiteManager.GetFile(type, fileUrl, tempFileName);

                                System.Drawing.Image image = downloadImages ? System.Drawing.Image.FromFile(tempFileName) : null;
                                try
                                {
                                    if (image != null)
                                    {
                                        imageSize = new System.Drawing.Size() { Height = image.Height, Width = image.Width };
                                        if (!imageSize.IsEmpty)
                                            SetImageSize(node.Node, imageSize);
                                    }
                                    if (!downloadImages || Helper.CheckImageSize(imageSize, minSize, false))
                                        result.Add(new ParseImageResult(node.Node, image, imageSize, fileUrl));
                                }
                                finally
                                {
                                    if (!result.Any(r => r.Url == fileUrl) && image != null)
                                        image.Dispose();

                                    if (downloadImages)
                                        try { System.IO.File.Delete(tempFileName); }
                                        catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, string.Format("Helper.GetAllImagesFromUrl(url:'{0}',..,type:'{1}').ForAllThread(fileUrl:'{2}',..)", url, type, fileUrl.AbsoluteUri)));
                        }
                        finally
                        {
                            if (prgItemImg != null)
                                lock (currLoadedLock)
                                {
                                    currLoaded++;
                                    prgItemImg.Value = ((float)currLoaded / (float)fullCnt) * (float)100;
                                }
                        }
                    }
                );
                if (prgItemImg != null)
                    prgItemImg.Value = 100;
            }
            catch (Exception ex)
            {
                Helpers.Log.Add(Helpers.Log.GetExceptionText(ex, string.Format("Helper.GetAllImagesFromUrl(url:'{0}',..,type:'{1}')", url, type)));
            }

            if (prgItem != null && prgItem.Value != 100)
                prgItem.Value = 100;

            result.RemoveAll(i => result.AsParallel().Count(r => r.Url.AbsoluteUri == i.Url.AbsoluteUri) > 1);
            return result.OrderBy(i => i.Url.ToString()).OrderByDescending(i => i.ImageSize.Width * i.ImageSize.Height).ToArray();
        }

        #region Masks and other strings stuff

        public static string GetDomainFromUrl(Uri Url, bool addStar)
        {
            if (Url == null) return null;
            return GetDomainFromString(Url.Host, addStar);
        }

        public static string GetDomainFromString(string host, bool addStar)
        {
            if (host == null) return null;

            var dotBits = host.Split('.');
            if (new int[] { 0, 1, 2 }.Contains(dotBits.Length)) return host;
            else
            {
                string[] items = host.Split('.').Reverse().Take(2).Reverse().ToArray();
                return (addStar ? "*" : string.Empty) + items[0] + "." + items[1];
            }
        }

        public static bool StringLikes(string source, string arguments)
        {
            try
            {
                string str = "^" + Regex.Escape(arguments);
                str = str.Replace("\\*", ".*").Replace("\\?", ".") + "$";

                bool result = (Regex.IsMatch(source, str, RegexOptions.IgnoreCase));
                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string LongestMaskedStringBetween(string first, string second)
        {
            string[] swap = new string[] { first.ToLower(), second.ToLower() };
            var swapOrdered = swap.OrderBy(i => i.Length);

            string s = swapOrdered.First();
            string l = swapOrdered.Last();

            int step = 0;
            try
            {
                for (int i = 0; i < s.Length; i++)
                {
                    step = i;

                    if (l.Length < s.Length)
                    {
                        //for (int n = 0; n < s.Length - l.Length; n++)
                        //    l += "*";
                        l = s.Substring(0,i) + l.Remove(0, i);

                        swap[0] = s;
                        swap[1] = l;

                        s = swap[1];
                        l = swap[0];
                    }


                    int indexOfSmallLetter = -1;
                    int curPosLength = 0;
                    for (int posLen = 1; posLen <= s.Length - i; posLen++)
                    {
                        int z1 = l.Substring(i).IndexOf(s.Substring(i, posLen));
                        if (z1 >= 0)
                        {
                            curPosLength = posLen;
                            indexOfSmallLetter = z1;
                        }
                        else
                            break;
                    }

                    if (l[i] != s[i])
                    {
                        if (indexOfSmallLetter > 0)
                        {
                            for (int n = 0; n < indexOfSmallLetter; n++)
                                s = s.Insert(i, "*");
                            //i += curPosLength - 1;
                            i += indexOfSmallLetter - 1;
                            continue;
                        }
                        else if (s[i] != '*')
                        {
                            s = s.Remove(i, 1);
                            s = s.Insert(i, "*");
                        }
                    }
                    else
                    {
                        if (indexOfSmallLetter != 0)
                        {
                            for (int n = 0; n < indexOfSmallLetter - i; n++)
                            {
                                s = s.Insert(i, "*");
                                i++;
                            }
                            //i = indexOfSmallLetter - 1;
                            continue;
                        }
                        else
                        {
                            if (curPosLength > 0)
                                i += curPosLength - 1;
                        }
                    }
                }

                if (l.Length > s.Length)
                    s += "*";

                while (s.IndexOf("**") >= 0)
                    s = s.Replace("**", "*");
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("LongestMaskedStringBetween() exception at step {0}: {1}{2}", step, Environment.NewLine, ex.Message));
            }

            return string.IsNullOrWhiteSpace(s) ? "*" : s;
        }

        public static string LongestMaskedStringBetween(string[] str)
        {
            string result = string.Empty;
            if (str.Count() > 1)
            {
                string fStr = str.First();
                result = fStr;
                foreach (string path in str.Where(p => p != fStr).ToArray())
                    result = LongestMaskedStringBetween(result, path);
            }
            return result;
        }

        public static string LongestMaskedPathBetween(string first, string second, char stringSeparator = '/')
        {
            string result = string.Empty;

            bool needFirstSeparator = first.FirstOrDefault() == stringSeparator && second.FirstOrDefault() == stringSeparator;

            string[] firstSplited = first.Split(new char[] { stringSeparator }, StringSplitOptions.RemoveEmptyEntries);
            string[] secondSplited = second.Split(new char[] { stringSeparator }, StringSplitOptions.RemoveEmptyEntries);

            List<string> sPath = new List<string>(firstSplited.Length > secondSplited.Length ? secondSplited : firstSplited);
            List<string> lPath = new List<string>(firstSplited.Length > secondSplited.Length ? firstSplited : secondSplited);

            for (int i = 0; i < sPath.Count; i++)
            {
                int indexOfItem = lPath.IndexOf(sPath[i]);
                if (indexOfItem > i && indexOfItem != i)
                {
                    for (int z = 0; z < indexOfItem - i; z++)
                        sPath.Insert(i, "*");
                    i = indexOfItem;
                }
            }

            List<string> path = new List<string>();

            for (int i = 0; i < Math.Min(sPath.Count, lPath.Count); i++)
            {
                path.Insert(0, LongestMaskedStringBetween(sPath[sPath.Count - 1 - i], lPath[lPath.Count - 1 - i]));
            }

            for (int i = 0; i < path.Count; i++)
                result += (result.Length > 0 || needFirstSeparator ? stringSeparator.ToString() : "") + path[i];

            string toChange = stringSeparator.ToString() + "*" + stringSeparator.ToString() + "*";
            //while (result.Contains(toChange))
            //    result = result.Replace(toChange, toChange.Substring(0, toChange.Length / 2));

            return result;
        }

        public static string LongestMaskedPathBetween(string[] paths, char stringSeparator = '/')
        {
            string result = string.Empty;
            if (paths.Count() > 1)
            {
                string fPath = paths.First();
                result = fPath;
                foreach (string path in paths.Where( p => p != fPath).ToArray())
                    result = LongestMaskedPathBetween(result, path, stringSeparator);
            }
            return result;
        }

        public static string LongestMaskedUriBetween(Uri first, Uri second)
        {
            string result = "*";

            if (first.IsAbsoluteUri == second.IsAbsoluteUri)
            {
                string hostPart = first.IsAbsoluteUri ? LongestMaskedStringBetween(first.Host, second.Host) : string.Empty;
                result = "*" + hostPart + "*" + LongestMaskedPathBetween(first.PathAndQuery, second.PathAndQuery);
            }
            else
            {
                result = "*" + LongestMaskedUriBetween(new Uri(first.PathAndQuery, UriKind.Relative), new Uri(second.PathAndQuery, UriKind.Relative));
            }

            while (result.IndexOf("**") >= 0)
                result = result.Replace("**", "*");

            return result;
        }

        public static string LongestMaskedUriBetween(Uri[] uries)
        {
            string result = "*";

            if (uries.All(u => !u.IsAbsoluteUri))
            {
                string pathPart = string.Empty;

                string[] paths = uries.Select(u => u.OriginalString).Distinct().ToArray();
                string fPart = paths.FirstOrDefault();
                if (fPart != null)
                {
                    pathPart = fPart;
                    foreach (string path in paths.Where(h => h != fPart))
                        pathPart = LongestMaskedPathBetween(path, pathPart);
                }

                result = pathPart;
            }
            else
            {
                string hostPart = string.Empty;

                string[] hosts = uries.Where(u => u.IsAbsoluteUri).Select( u => u.Host ).Distinct().ToArray();
                string fHost = hosts.FirstOrDefault();
                if (fHost != null)
                {
                    hostPart = fHost;
                    foreach (string host in hosts.Where(h => h != fHost))
                        hostPart = LongestMaskedStringBetween(host, hostPart);
                }

                result = "*" + hostPart + "*" + LongestMaskedUriBetween(uries.Select(u => new Uri(u.PathAndQuery, UriKind.Relative)).ToArray());
            }

            while (result.IndexOf("**") >= 0)
                result = result.Replace("**", "*");

            return string.IsNullOrWhiteSpace(result) ? "*" : result;
        }

        #endregion

        public static ParseRule GetRule(HtmlNodeWithUrl[] nodes, string label, System.Drawing.Size minSize, bool collectIMGTags, bool collectLINKTags, bool collectMETATags)
        {
            ParseRule result =
                   GetRuleByLink(nodes, label, collectIMGTags, collectLINKTags, collectMETATags, null)
                ?? GetRuleByXPath(nodes, label, collectIMGTags, collectLINKTags, collectMETATags, null)
                ?? GetRuleByLink(nodes, label, collectIMGTags, collectLINKTags, collectMETATags, minSize)
                ?? GetRuleByXPath(nodes, label, collectIMGTags, collectLINKTags, collectMETATags, minSize)
                ?? new ParseRule();
            return result;
        }

        private static ParseRule GetRuleByLink(HtmlNodeWithUrl[] nodesarr, string label, bool collectIMGTags, bool collectLINKTags, bool collectMETATags, System.Drawing.Size? minSize = null)
        {
            ParseRule result = null;

            if (nodesarr != null && nodesarr.Length > 0)
            {
                #region ByLink
                string mask = string.Empty;
                mask = LongestMaskedUriBetween(nodesarr.Select(n => n.Url).ToArray());

                int badNodesWithThisMask =
                        nodesarr
                            .Where(n => !string.IsNullOrWhiteSpace(n.Node.OuterHtml) )
                            .Select(n => new { Node = n.Node.OwnerDocument.DocumentNode, Url = n.Url })
                            .Select(nodeItem => 
                                {
                                    var links = Helper
                                                .GetAllImagesUrlsFromUrl(nodeItem.Node.OwnerDocument, nodeItem.Url.AbsoluteUri, collectIMGTags, collectLINKTags, collectMETATags)
                                                .Where(n => Helper.StringLikes(n.Url.AbsoluteUri, mask));
                                    var results =
                                        minSize == null
                                        ? links.Count()
                                        : (links = Helper.GetAllImagesUrlsWithMinSize(links.ToArray(), minSize.Value)).Count();

                                    return results;
                                }
                            )
                            .Where(c => c != 1)
                            .Count();

                if (badNodesWithThisMask == 0)
                    result = new ParseRule()
                    {
                        Label = label,
                        Condition = ParseFindRuleCondition.ByLink,
                        Parameter = mask
                    };

                #endregion
                #region ByLinkAndIndex
                if (result == null)
                {
                    int index =
                        nodesarr
                            .Select(n => new { Doc = n.Node.OwnerDocument, Node = n.Node.OwnerDocument.DocumentNode, Url = n.Url })
                            .Select(n =>
                            {
                                var links = Helper
                                            .GetAllImagesUrlsFromUrl(n.Node.OwnerDocument, n.Url.AbsoluteUri, collectIMGTags, collectLINKTags, collectMETATags)
                                            .Where(i => Helper.StringLikes(i.Url.AbsoluteUri, mask)).ToArray();

                                string[] images =
                                    (minSize == null 
                                        ? links
                                        : (links = Helper.GetAllImagesUrlsWithMinSize(links, minSize.Value))
                                    )
                                    .Select( i => i.Url.AbsoluteUri)
                                    .ToArray();

                                for (int i = 0; i < images.Length; i++)
                                    if (images[i].ToLower() == n.Url.AbsoluteUri.ToLower())
                                        return i;
                                return -1;
                            }
                            ).Distinct().OrderBy(i => i).FirstOrDefault();
                    if (index != -1)
                        result = new ParseRule()
                        {
                            Label = label,
                            Condition = ParseFindRuleCondition.ByLinkAndIndex,
                            Parameter = mask + ";" + index.ToString()
                        };
                }
                #endregion
            }

            if (result != null)
            {
                result.CheckImageSize = minSize != null ? true : false;
                if (minSize != null)
                    result.MinImageSize = minSize.Value;
                result.CollectIMGTags = collectIMGTags;
                result.CollectLINKTags = collectLINKTags;
                result.CollectMETATags = collectMETATags;
            }

            if (minSize != null && result == null)
            {
                System.Drawing.Size minCalcedSize = new System.Drawing.Size();

                foreach(var sz in Helper.GetImageSizes(nodesarr.Select(n => new SomeNodeElement() { Node = n.Node, Url = n.Url }).ToArray()).Select( n => n.Value ))
                {
                    if (minCalcedSize.Width > sz.Width || minCalcedSize.Width == 0)
                        minCalcedSize.Width = sz.Width;
                    if (minCalcedSize.Height > sz.Height || minCalcedSize.Height == 0)
                        minCalcedSize.Height = sz.Height;
                }
                
                if (minSize.Value.Height < minCalcedSize.Height || minSize.Value.Width < minCalcedSize.Width)
                    result = GetRuleByLink(nodesarr, label, collectIMGTags, collectLINKTags, collectMETATags, minCalcedSize);
            }

            return result;
        }

        private static ParseRule GetRuleByXPath(HtmlNodeWithUrl[] nodesarr, string label, bool collectIMGTags, bool collectLINKTags, bool collectMETATags, System.Drawing.Size? minSize = null)
        {
            ParseRule result = null;

            if (nodesarr != null && nodesarr.Length > 0)
            {
                #region ByXPath
                string mask1 = string.Empty;
                string mask2 = string.Empty;

                string[] xpaths = nodesarr.Select(n => n.Node.XPath).ToArray();
                mask1 = LongestMaskedPathBetween(xpaths);
                mask2 = LongestMaskedStringBetween(xpaths);
  
                if (
                        nodesarr
                            .Select(n => new { Node = n.Node.OwnerDocument.DocumentNode, Url = n.Url })
                            .Select(nodeItem =>
                                {
                                    var links = Helper
                                                .GetAllImagesUrlsFromUrl(nodeItem.Node.OwnerDocument, nodeItem.Url.AbsoluteUri, collectIMGTags, collectLINKTags, collectMETATags)
                                                .Where(n => Helper.StringLikes(n.Url.AbsoluteUri, mask1));
                                    return
                                        minSize == null
                                        ? links.Count()
                                        : Helper.GetAllImagesUrlsWithMinSize(links.ToArray(), minSize.Value).Count();
                                }
                            )
                            .Where(c => c != 1)
                            .Count() == 0)
                    result = new ParseRule()
                    {
                        Label = label,
                        Condition = ParseFindRuleCondition.ByXPath,
                        Parameter = mask1
                    };

                if (
                        nodesarr
                            .Select(n => new { Node = n.Node.OwnerDocument.DocumentNode, Url = n.Url })
                            .Select(nodeItem =>
                                {
                                    var links = Helper
                                                .GetAllImagesUrlsFromUrl(nodeItem.Node.OwnerDocument, nodeItem.Url.AbsoluteUri, collectIMGTags, collectLINKTags, collectMETATags)
                                                .Where(n => Helper.StringLikes(n.Url.AbsoluteUri, mask2));
                                    return
                                        minSize == null
                                        ? links.Count()
                                        : Helper.GetAllImagesUrlsWithMinSize(links.ToArray(), minSize.Value).Count();
                                }
                            )
                            .Where(c => c != 1)
                            .Count() == 0)
                    result = new ParseRule()
                    {
                        Label = label,
                        Condition = ParseFindRuleCondition.ByXPath,
                        Parameter = mask2
                    };

                #endregion
                #region ByXPathAndIndex
                if (result == null)
                {
                    string betterMask = mask2;
                    int index = 
                        nodesarr
                            .Select(n => new { Doc = n.Node.OwnerDocument, Node = n.Node.OwnerDocument.DocumentNode, Url = n.Url })
                            .Select(n => 
                                {
                                    var links = Helper
                                            .GetAllImagesUrlsFromUrl(n.Node.OwnerDocument, n.Url.AbsoluteUri, collectIMGTags, collectLINKTags, collectMETATags)
                                            .Where(i => Helper.StringLikes(i.Url.AbsoluteUri, betterMask));

                                    string[] images =
                                        (minSize == null
                                            ? links.ToArray()
                                            : Helper.GetAllImagesUrlsWithMinSize(links.ToArray(), minSize.Value)
                                        )
                                        .Select(i => i.Url.AbsoluteUri)
                                        .ToArray();

                                    for (int i = 0; i < images.Length; i++)
                                        if (images[i].ToLower() == n.Url.AbsoluteUri.ToLower())
                                            return i;
                                    return -1;
                                }
                            ).Distinct().OrderBy( i => i).FirstOrDefault();
                    if (index != -1)
                        result = new ParseRule()
                        {
                            Label = label,
                            Condition = ParseFindRuleCondition.ByXPathAndIndex,
                            Parameter = betterMask + ";" + index.ToString()
                        };
                }
                #endregion
            }

            if (result != null)
            {
                result.CheckImageSize = minSize != null ? true : false;
                if (minSize != null)
                    result.MinImageSize = minSize.Value;
                result.CollectIMGTags = collectIMGTags;
                result.CollectLINKTags = collectLINKTags;
                result.CollectMETATags = collectMETATags;
            }

            if (minSize != null && result == null)
            {
                System.Drawing.Size minCalcedSize = new System.Drawing.Size();

                foreach (var sz in Helper.GetImageSizes(nodesarr.Select(n => new SomeNodeElement() { Node = n.Node, Url = n.Url }).ToArray()).Select(n => n.Value))
                {
                    if (minCalcedSize.Width > sz.Width)
                        minCalcedSize.Width = sz.Width;
                    if (minCalcedSize.Height > sz.Height)
                        minCalcedSize.Height = sz.Height;
                }

                if (minSize.Value.Height < minCalcedSize.Height || minSize.Value.Width < minCalcedSize.Width)
                    result = GetRuleByLink(nodesarr, label, collectIMGTags, collectLINKTags, collectMETATags, minCalcedSize);
            }

            return result;
        }
    }
}
