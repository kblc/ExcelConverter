using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;

namespace ExcelConverter.Parser
{
    //public class CEFLoader1
    //{
    //    static CEFLoader1()
    //    {
    //        return;
    //        DebugOutput("1. Initialize CEF");

    //        // Initializes the CEF and its settings
    //        string resources = Path.Combine(Directory.GetCurrentDirectory(), "cache");
    //        if (Directory.Exists(resources))
    //            try
    //            {
    //                Directory.Delete(resources, true);
    //                Directory.CreateDirectory(resources);
    //            }
    //            catch { }

    //        string logFile = Path.Combine(Directory.GetCurrentDirectory(), "chromelog.txt");
    //        if (File.Exists(logFile))
    //            try
    //            {
    //                File.Delete(logFile);
    //            }
    //            catch
    //            {
    //                logFile = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_chromelog.txt");
    //            }

    //        CefSharp.CefSettings settings = new CefSharp.CefSettings()
    //        {
    //            CachePath = resources,
    //            Locale = "ru",
    //            LogFile = logFile,
    //        };
    //        Cef.Initialize(settings);
    //    }

    //    // Offscreen browser instance
    //    private readonly ChromiumWebBrowser OffScreenBrowser;

    //    // Event for syncing between the worker threads and the main thread.
    //    private readonly ManualResetEvent LoadedEvent = new ManualResetEvent(false);

    //    private readonly ManualResetEvent InitializedEvent = new ManualResetEvent(false);

    //    public CEFLoader1(bool waitInitialized)
    //    {
    //        DebugOutput("2. Create Offscreen CEF");

    //        // Create browser and hook status events
    //        this.OffScreenBrowser = new ChromiumWebBrowser("about:blank");
    //        this.OffScreenBrowser.LoadError += this.OffScreenBrowser_LoadError;
    //        this.OffScreenBrowser.FrameLoadEnd += this.OffScreenBrowser_FrameLoadEnd;
    //        this.OffScreenBrowser.BrowserInitialized += OffScreenBrowser_BrowserInitialized;

    //        if (waitInitialized)
    //        {
    //            DebugOutput("Waiting for initialized");
    //            this.InitializedEvent.WaitOne();
    //        }
    //    }

    //    void OffScreenBrowser_BrowserInitialized(object sender, EventArgs e)
    //    {
    //        DebugOutput("Initialized");
    //        this.InitializedEvent.Set();
    //    }

    //    // Loads an HTML document and waits for the load to finish.
    //    public void ShowHTML(Stream readStream)
    //    {
    //        string htmlData;
    //        using (StreamReader reader = new StreamReader(readStream, System.Text.Encoding.UTF8))
    //        {
    //            htmlData = reader.ReadToEnd();
    //        }

    //        DebugOutput("3. Load HTML");

    //        this.OffScreenBrowser.LoadHtml(htmlData, "myscheme://web/__Memory__Document__.html");

    //        DebugOutput("4. Wait for load complete");

    //        // Wait for the load to complete! THIS WAITS FOREVER!!!
    //        this.LoadedEvent.WaitOne();
    //    }

    //    public void ShowHTML(string url)
    //    {
    //        DebugOutput("3. Load HTML");

    //        this.OffScreenBrowser.Load(url);

    //        DebugOutput("4. Wait for load complete");

    //        // Wait for the load to complete! THIS WAITS FOREVER!!!
    //        this.LoadedEvent.WaitOne();
    //    }

    //    private void OffScreenBrowser_LoadError(object sender, LoadErrorEventArgs e)
    //    {
    //        DebugOutput("5a. Load Error: {0} ({1}) in {2}",
    //            e.ErrorCode, e.ErrorText, e.FailedUrl);
    //    }

    //    private void OffScreenBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
    //    {
    //        DebugOutput("5b. Load Completed");

    //        // Load ended ... tell the main thread to continue ... SOMETIMES THIS NEVER HAPPENS!
    //        this.LoadedEvent.Set();
    //    }

    //    public object EvaluateScript(string javascript)
    //    {
    //        DebugOutput("6. Evaluate Javascript");

    //        Task<JavascriptResponse> task = this.OffScreenBrowser.EvaluateScriptAsync(javascript, null);

    //        DebugOutput("7. Wait for Javascript");

    //        task.Wait();

    //        DebugOutput("8. Javascript done. Faulted: {0}", task.IsFaulted);

    //        if (task.IsFaulted)
    //            throw new Exception("Javascript failed", task.Exception);
    //        return task.Result.Result;
    //    }

    //    [DllImport("kernel32.dll")]
    //    private static extern void OutputDebugString(string lpOutputString);

    //    private static void DebugOutput(string format, params object[] args)
    //    {
    //        string txt = String.Format("~{0}: {1}",
    //            Thread.CurrentThread.ManagedThreadId,
    //            String.Format(format, args));
    //        CEFLoader.OutputDebugString(txt + "\r\n");
    //        Console.WriteLine(txt);
    //    }
    //}

    //public class CEFLoader2
    //{
    //    static CEFLoader2()
    //    {
    //        CefSharp.CefSettings settings = new CefSharp.CefSettings()
    //        {
    //            CachePath = "cache",
    //            Locale = "ru",
    //            LogFile = "chromelog.txt",
    //            BrowserSubprocessPath = "CefSharp.BrowserSubprocess.exe",
    //            RemoteDebuggingPort = 8088,
    //            LogSeverity = LogSeverity.Verbose
    //        };
    //        Cef.OnContextInitialized = delegate
    //        {
    //            Cef.GetGlobalCookieManager().SetStoragePath("cookies", true);
    //        };
    //        Cef.Initialize(settings, shutdownOnProcessExit: true, performDependencyCheck: !Debugger.IsAttached);
    //    }

    //    public string GetHTML(string url)
    //    { 
    //        return MainAsync(Path.Combine(Directory.GetCurrentDirectory(), "cache"), url);
    //    }

    //    private static string MainAsync(string cachePath, string url)
    //    {
    //        var browserSettings = new BrowserSettings();
    //        //var requestContextSettings = new RequestContextSettings { CachePath = cachePath };

    //        // RequestContext can be shared between browser instances and allows for custom settings
    //        // e.g. CachePath
    //        //using (var requestContext = new RequestContext(requestContextSettings))
    //        using (var browser = new ChromiumWebBrowser(url, browserSettings: browserSettings)) //, requestcontext: requestContext
    //        {
    //            browser.FrameLoadEnd += (s, e) =>
    //            {
    //                Thread.Sleep(100);
    //            };
    //            var tsk = LoadPageAsync(browser);
    //            tsk.Wait();

    //            var js = browser.EvaluateScriptAsync(@"document.getElementsByTagName ('html')[0].innerHTML");
    //            js.Wait();
    //            return js.Result.Result.ToString();
    //        }
    //    }

    //    public static Task LoadPageAsync(IWebBrowser browser, string address = null)
    //    {
    //        var tcs = new TaskCompletionSource<bool>();

    //        EventHandler<LoadingStateChangedEventArgs> handler = null;
    //        handler = (sender, args) =>
    //        {
    //            //Wait for while page to finish loading not just the first frame
    //            if (!args.IsLoading)
    //            {
    //                browser.LoadingStateChanged -= handler;
    //                tcs.TrySetResult(true);
    //            }
    //        };

    //        browser.LoadingStateChanged += handler;

    //        if (!string.IsNullOrEmpty(address))
    //        {
    //            browser.Load(address);
    //        }
    //        return tcs.Task;
    //    }
    //}
}
