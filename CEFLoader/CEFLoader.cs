﻿using CefSharp;
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
            var session = Log.SessionStart("CEFLoader()", true);
            try
            {
                Log.Add(session, "Start initialization...");
                Log.Add(session, string.Format("Current directory for initialization: '{0}'", Helpers.Log.CurrentPath));

                string resources = Path.Combine(Helpers.Log.CurrentPath, "cache");
                Log.Add(session, string.Format("Cache directory is '{0}'. Directory exists: '{1}'", resources, Directory.Exists(resources)));
                if (Directory.Exists(resources))
                    try
                    {
                        Directory.Delete(resources, true);
                        Directory.CreateDirectory(resources);
                        Log.Add(session, "Cache directory cleared");
                    }
                    catch(Exception ex)
                    {
                        Log.Add(session, "Can't clear cache directory");
                        Log.Add(session, ex);
                    }

                string logFile = Path.Combine(Helpers.Log.CurrentPath, "chromelog.txt");
                Log.Add(session, string.Format("Log file is '{0}'. Log file exists: '{1}'", logFile, File.Exists(logFile)));
                if (File.Exists(logFile))
                    try
                    {
                        File.Delete(logFile);
                    }
                    catch(Exception ex)
                    {
                        Log.Add(session, "Can't remove log file");
                        Log.Add(session, ex);
                        logFile = Path.Combine(Helpers.Log.CurrentPath, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_chromelog.txt");
                        Log.Add(session, string.Format("New log file is '{0}'. Log file exists: '{1}'", logFile, File.Exists(logFile)));
                    }

                string subprocessPath = Path.Combine(Helpers.Log.CurrentPath, "CefSharp.BrowserSubprocess.exe");
                Log.Add(session, string.Format("Subprocess path is '{0}'. Sub process file exists: '{1}'", subprocessPath, File.Exists(subprocessPath)));

                if (!File.Exists(subprocessPath))
                    throw new Exception("Subprocess file not exists!");

                CefSharp.CefSettings settings = new CefSharp.CefSettings()
                {
                    CachePath = resources.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty),
                    LogFile = logFile.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty),
                    BrowserSubprocessPath = subprocessPath.Replace(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar, string.Empty),
                    RemoteDebuggingPort = 8088,
                    LogSeverity = Debugger.IsAttached ? LogSeverity.Verbose : LogSeverity.Warning
                };
                Cef.OnContextInitialized = delegate
                {
                    Cef.GetGlobalCookieManager().SetStoragePath("cookies", true);
                };
                Log.Add(session, "Start Cef.Initialize()");
                Cef.Initialize(settings, shutdownOnProcessExit: true, performDependencyCheck: !Debugger.IsAttached);
                Log.Add(session, "Cef.Initialize() done");
            }
            catch(Exception ex)
            {
                Log.Add(session, ex);
                throw;
            }
            finally
            {
                Log.SessionEnd(session);
            }
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
