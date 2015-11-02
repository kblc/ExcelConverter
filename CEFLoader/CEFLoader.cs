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
using Helpers;

namespace CEFLoader
{
    public static class CEFLoader// : IDisposable
    {
        static CEFLoader()
        {
            var session = Helpers.Old.Log.SessionStart("CEFLoader()", true);
            try
            {
                Helpers.Old.Log.Add(session, "Start dependency checker...");
                Helpers.Old.Log.Add(session, string.Format("Current directory for initialization: '{0}'", Helpers.Log.CurrentPath));

                string cache = Path.Combine(Helpers.Log.CurrentPath, "cache");
                Helpers.Old.Log.Add(session, string.Format("Cache directory is '{0}'. Directory exists: '{1}'", cache, Directory.Exists(cache)));
                if (Directory.Exists(cache))
                    try
                    {
                        Directory.Delete(cache, true);
                        Directory.CreateDirectory(cache);
                        Helpers.Old.Log.Add(session, "Cache directory cleared");
                    }
                    catch (Exception ex)
                    {
                        Helpers.Old.Log.Add(session, "Can't clear cache directory");
                        Helpers.Old.Log.Add(session, ex);
                    }

                string logFile = Path.Combine(Helpers.Log.CurrentPath, "chromelog.txt");
                Helpers.Old.Log.Add(session, string.Format("Log file is '{0}'. Log file exists: '{1}'", logFile, File.Exists(logFile)));
                if (File.Exists(logFile))
                    try
                    {
                        File.Delete(logFile);
                    }
                    catch (Exception ex)
                    {
                        Helpers.Old.Log.Add(session, "Can't remove log file");
                        Helpers.Old.Log.Add(session, ex);
                        logFile = Path.Combine(Helpers.Log.CurrentPath, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_chromelog.txt");
                        Helpers.Old.Log.Add(session, string.Format("New log file is '{0}'. Log file exists: '{1}'", logFile, File.Exists(logFile)));
                    }

                string subprocessPath = Path.Combine(Helpers.Log.CurrentPath, "CefSharp.BrowserSubprocess.exe");
                Helpers.Old.Log.Add(session, string.Format("Subprocess path is '{0}'. Sub process file exists: '{1}'", subprocessPath, File.Exists(subprocessPath)));

                if (!File.Exists(subprocessPath))
                    throw new Exception("Subprocess file not exists!");

                CefSharp.DependencyChecker.AssertAllDependenciesPresent("ru", "locales", "", false, subprocessPath);

                Helpers.Old.Log.Add(session, "Check done");
            }
            catch (Exception ex)
            {
                Helpers.Old.Log.Add(session, ex);
                throw;
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(session);
            }
        }

        private static bool CEFInited = false;
        private static bool CEFLoaderInit()
        {
            var session = Helpers.Old.Log.SessionStart("CEFLoaderInit()", true);
            try
            {
                Helpers.Old.Log.Add(session, "Start initialization...");

                if (Cef.IsInitialized)
                {
                    Helpers.Old.Log.Add(session, "CEF already initialized. Exit.");
                    return true;
                }

                Helpers.Old.Log.Add(session, string.Format("Current directory for initialization: '{0}'", Helpers.Log.CurrentPath));

                string cache = Path.Combine(Helpers.Log.CurrentPath, "cache");
                Helpers.Old.Log.Add(session, string.Format("Cache directory is '{0}'. Directory exists: '{1}'", cache, Directory.Exists(cache)));
                if (Directory.Exists(cache))
                    try
                    {
                        Directory.Delete(cache, true);
                        Directory.CreateDirectory(cache);
                        Helpers.Old.Log.Add(session, "Cache directory cleared");
                    }
                    catch(Exception ex)
                    {
                        Helpers.Old.Log.Add(session, "Can't clear cache directory");
                        Helpers.Old.Log.Add(session, ex);
                    }

                string logFile = Path.Combine(Helpers.Log.CurrentPath, "chromelog.txt");
                Helpers.Old.Log.Add(session, string.Format("Log file is '{0}'. Log file exists: '{1}'", logFile, File.Exists(logFile)));
                if (File.Exists(logFile))
                    try
                    {
                        File.Delete(logFile);
                    }
                    catch(Exception ex)
                    {
                        Helpers.Old.Log.Add(session, "Can't remove log file");
                        Helpers.Old.Log.Add(session, ex);
                        logFile = Path.Combine(Helpers.Log.CurrentPath, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_chromelog.txt");
                        Helpers.Old.Log.Add(session, string.Format("New log file is '{0}'. Log file exists: '{1}'", logFile, File.Exists(logFile)));
                    }

                string subprocessPath = Path.Combine(Helpers.Log.CurrentPath, "CefSharp.BrowserSubprocess.exe");
                Helpers.Old.Log.Add(session, string.Format("Subprocess path is '{0}'. Sub process file exists: '{1}'", subprocessPath, File.Exists(subprocessPath)));

                if (!File.Exists(subprocessPath))
                    throw new Exception("Subprocess file not exists!");

                CefSharp.CefSettings settings = new CefSharp.CefSettings()
                {
                    CachePath = cache.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty),
                    LogFile = logFile.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty),
                    BrowserSubprocessPath = subprocessPath.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty),
                    RemoteDebuggingPort = 8088,
                    LogSeverity = Debugger.IsAttached ? LogSeverity.Verbose : LogSeverity.Warning
                };
                Cef.OnContextInitialized = delegate
                {
                    Cef.GetGlobalCookieManager().SetStoragePath("cookies", true);
                };
                Helpers.Old.Log.Add(session, "Start Cef.Initialize()");
                var res = Cef.Initialize(settings, shutdownOnProcessExit: true, performDependencyCheck: !Debugger.IsAttached);
                Helpers.Old.Log.Add(session, "Cef.Initialize() done");
                return res;
            }
            catch(Exception ex)
            {
                Helpers.Old.Log.Add(session, ex);
                throw;
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(session);
            }
        }

        public static void Shutdown(bool killCurrentProcess = false)
        {
            if (CEFInited)
                Cef.Shutdown();
            if (killCurrentProcess)
                Process.GetCurrentProcess().Kill();
        }

        private static object initLock = new object();
        public static string GetHTML(ref string url, TimeSpan timeout)
        {
            lock(initLock)
                if (!CEFInited)
                    CEFInited = CEFLoaderInit();
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
