using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CEFLoader.Test
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var items =
                Enumerable.Range(0, 20)
                    .AsParallel()
                    .WithDegreeOfParallelism(6)
                    .Select(i =>
                    {
                        var tsk = Task.Factory.StartNew<string>(() =>
                        {
                            var url = "http://google.com/?q=" + i.ToString();
                            var res = CEFLoader.GetHTML(ref url, new TimeSpan(0,0,10));
                            return url;
                        });
                        tsk.Wait();
                        return tsk.Result;
                    }
                ).ToArray();

            foreach(var url in items)
                Console.WriteLine((string.IsNullOrEmpty(url) ? "[+]" : "[-]") + " NEW URL IS: " + url);

            Console.ReadKey();
            CEFLoader.Shutdown(killCurrentProcess : true);
        }
    }
}
