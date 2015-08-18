using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;

namespace CEFLoader.Test
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            { 
                var items =
                    Enumerable.Range(0, 5)
                        .AsParallel()
                        .WithDegreeOfParallelism(6)
                        .Select(i =>
                        {
                            var url = "http://google.com/?q=" + i.ToString();
                            try
                            { 
                                var tsk = Task.Factory.StartNew<string>(() =>
                                {
                                    var res = CEFLoader.GetHTML(ref url, new TimeSpan(0,0,10));
                                    return url;
                                });
                                tsk.Wait();
                                if (tsk.IsCompleted)
                                    return tsk.Result;
                                if (tsk.Exception != null)
                                    throw tsk.Exception;
                                return "EMPTY RESULT";
                            }
                            catch (Exception ex)
                            {
                                Log.Add(ex);
                                return "ERROR: " + ex.Message;
                            }
                        }
                    ).ToArray();

                foreach(var url in items)
                    Console.WriteLine((string.IsNullOrEmpty(url) ? "[+]" : "[-]") + " NEW URL IS: " + url);

                Console.ReadKey();
                CEFLoader.Shutdown(killCurrentProcess : true);
            }
            catch(Exception ex)
            {
                Log.Add(ex);
            }
        }
    }
}
