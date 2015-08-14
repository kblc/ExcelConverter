using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CEFLoader
{
    public static class CEFLoader// : IDisposable
    {
        static CEFLoader()
        {
            string resources = Path.Combine(Directory.GetCurrentDirectory(), "cache");
            if (Directory.Exists(resources))
                try
                {
                    Directory.Delete(resources, true);
                    Directory.CreateDirectory(resources);
                }
                catch { }

            string logFile = Path.Combine(Directory.GetCurrentDirectory(), "chromelog.txt");
            if (File.Exists(logFile))
                try
                {
                    File.Delete(logFile);
                }
                catch
                {
                    logFile = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_chromelog.txt");
                }

            CefSharp.CefSettings settings = new CefSharp.CefSettings()
            {
                CachePath = resources,
                LogFile = logFile,
                BrowserSubprocessPath = Path.Combine(Directory.GetCurrentDirectory(), "CefSharp.BrowserSubprocess.exe"),
                RemoteDebuggingPort = 8088,
                LogSeverity = LogSeverity.Warning
            };
            Cef.OnContextInitialized = delegate
            {
                Cef.GetGlobalCookieManager().SetStoragePath("cookies", true);
            };
            Cef.Initialize(settings, shutdownOnProcessExit: true, performDependencyCheck: !Debugger.IsAttached);
        }

        public static void Shutdown(bool killCurrentProcess = false)
        {
            Cef.Shutdown();
            if (killCurrentProcess)
                Process.GetCurrentProcess().Kill();
        }

        public static string GetHTML(ref string url, TimeSpan timeout)
        {
            return MainAsync(ref url, timeout);
        }

        public static string GetHTML(ref string url, int timeoutInMilliseconds = 0)
        {
            return GetHTML(ref url, TimeSpan.FromMilliseconds(timeoutInMilliseconds));
        }
        
        private static string MainAsync(ref string url, TimeSpan timeout)
        {
            var browserSettings = new BrowserSettings() { };
            using (var browser = new ChromiumWebBrowser(url, browserSettings: browserSettings)) //, requestcontext: requestContext
                return MainAsync(browser, ref url, timeout);
        }

        private static string MainAsync(IWebBrowser browser, ref string url, TimeSpan timeout)
        {
            var tsk = LoadPageAsync(browser);
            tsk.Wait(new TimeSpan(0, 0, 30));
            if (tsk.IsCompleted)
            {
                url = tsk.Result;
                if (timeout.TotalMilliseconds > 0)
                { 
                    var waitDone = new AutoResetEvent(false);
                    var th = new Thread(new ParameterizedThreadStart((p) => 
                    {
                        var are = p as AutoResetEvent;
                        if (are != null)
                        {
                            Thread.Sleep((int)timeout.TotalMilliseconds);
                            are.Set();
                        }
                    }));
                    th.Start(waitDone);
                    //Task.Factory.StartNew(() => { Thread.Sleep((int)timeout.TotalMilliseconds); waitDone.Set(); });
                    waitDone.WaitOne();
                }
                var js = browser.EvaluateScriptAsync(@"document.getElementsByTagName('html')[0].innerHTML");
                js.Wait();
                return js.Result.Result.ToString();
            }
            else
                throw new Exception("Timeout");
        }

        private static Task<string> LoadPageAsync(IWebBrowser browser, string address = null)
        {
            var tcs = new TaskCompletionSource<string>();

            EventHandler<LoadingStateChangedEventArgs> handler = null;
            handler = (sender, args) =>
            {
                //Wait for while page to finish loading not just the first frame
                if (!args.IsLoading)
                {
                    browser.LoadingStateChanged -= handler;
                    tcs.TrySetResult(browser.Address);
                }
            };

            browser.LoadingStateChanged += handler;

            if (!string.IsNullOrEmpty(address))
            {
                browser.Load(address);
            }
            return tcs.Task;
        }

        //public void Dispose()
        //{
        //    if (browser != null)
        //        browser.Dispose();
        //}

        //public CEFLoader(string url = null)
        //{
        //    browserSettings = new BrowserSettings();
        //    browser = new ChromiumWebBrowser(url ?? "about:blank", browserSettings: browserSettings);
        //}

        //public string GetHTML(ref string url, TimeSpan timeout)
        //{
        //    return MainAsync(browser, ref url, timeout);
        //}

        //private ChromiumWebBrowser browser { get; set; }
        //private BrowserSettings browserSettings { get; set; }
    }
}
