using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Helpers
{
    public static class Log
    {
        internal class SessionInfo
        {
            public DateTime SessionStart = DateTime.Now;
            public string SessionName = string.Empty;
            public SessionInfo()
            {
            }
            public SessionInfo(string sessionName)
                : this()
            {
                SessionName = sessionName;
            }
            public SessionInfo(string sessionName, DateTime sessionStart)
                : this(sessionName)
            {
                SessionStart = sessionStart;
            }

            public bool IsBlockInfo { get; set; }

            private List<string> log = null;
            public List<string> Log
            {
                get
                {
                    return log ?? (log = new List<string>());
                }
            }
        }

        private static object sessionsLock = new Object();
        private static Dictionary<Guid, SessionInfo> Sessions = new Dictionary<Guid, SessionInfo>();

        private const string WhereCatchedFormat = "{0} :: {1}";

        public static string LogFileName = string.Empty;
        private static object fileLogLock = new Object();

        public static Guid SessionStart(string sessionName, bool isBlockInfo = false)
        {
            Guid result = Guid.NewGuid();
            lock (sessionsLock)
                Sessions.Add(result, new SessionInfo(sessionName) { IsBlockInfo = isBlockInfo });
            return result;
        }

        public static void SessionEnd(Guid session, bool writeThisBlock = true)
        {
            Add(session, string.Format("elapsed time: {0} ms.", (DateTime.Now - Sessions[session].SessionStart).TotalMilliseconds));
            lock (sessionsLock)
            {
                if (Sessions[session].IsBlockInfo)
                {
                    if (writeThisBlock && !string.IsNullOrWhiteSpace(LogFileName))
                        lock (fileLogLock)
                            using (StreamWriter w = File.AppendText(Environment.CurrentDirectory + Path.DirectorySeparatorChar + LogFileName))
                            {
                                w.WriteLine("##########################################################");
                                w.WriteLine(string.Format("### {0}", Sessions[session].SessionName));
                                foreach(string line in Sessions[session].Log)
                                    w.WriteLine(line);
                                w.WriteLine("##########################################################");
                            }
                    Sessions[session].Log.Clear();
                }
                Sessions.Remove(session);
            }
        }

        public static void Add(string logMessage)
        {
            Add(Guid.Empty, logMessage);
        }
        public static void AddWithCatcher(string whereCathched, string logMessage)
        {
            Add(Guid.Empty, string.Format(WhereCatchedFormat, whereCathched, logMessage));
        }

        private static string GetFullLogMessage(Guid session, string logMessage, out bool isBlock)
        {
            string message = string.Format("[{0}] ", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

            if (session != Guid.Empty)
                lock (sessionsLock)
                {
                    isBlock = Sessions[session].IsBlockInfo;
                    message +=
                        isBlock
                        ? logMessage
                        : string.Format(WhereCatchedFormat, Sessions[session].SessionName, logMessage);
                }
            else
            {
                isBlock = false;
                message += logMessage;
            }

            return message;
        }

        public static void Add(Guid session, string logMessage)
        {
            bool isBlock;
            logMessage = GetFullLogMessage(session, logMessage, out isBlock);

            if (isBlock && session != null)
                lock(sessionsLock)
                {
                    Sessions[session].Log.Add(logMessage);
                }
            else
            { 
#if DEBUG
                Trace.WriteLine(logMessage);
#else
                Console.WriteLine(logMessage);
#endif
                //start /B /wait ????.exe > out.txt & type out.txt
                if (!string.IsNullOrWhiteSpace(LogFileName))
                    lock (fileLogLock)
                        using (StreamWriter w = File.AppendText(Environment.CurrentDirectory + Path.DirectorySeparatorChar + LogFileName))
                        {
                            w.WriteLine(logMessage);
                        }
            }
        }

        public static void Add(Guid session, Exception ex)
        {
            Add(session, ex.GetExceptionText());
        }

        public static void Add(Exception ex)
        {
            Add(ex.GetExceptionText());
        }

        public static void Clear()
        {
            if (!string.IsNullOrEmpty(LogFileName) && File.Exists(Environment.CurrentDirectory + Path.DirectorySeparatorChar + LogFileName))
                lock (fileLogLock)
                    File.Delete(Environment.CurrentDirectory + Path.DirectorySeparatorChar + LogFileName);
        }

        public static string GetExceptionText(this Exception ex, string whereCathched = null)
        {
            if (ex == null)
                return string.Empty;

            string result = string.Empty;
            Exception innerEx = ex;
            while (innerEx != null)
            {
                result += 
                    (string.IsNullOrWhiteSpace(result))
                    ? string.Format("exception '{1}' occured; Source: '{2}';", Environment.NewLine, innerEx.Message, innerEx.Source)
                    : string.Format("{0}inner exception '{1}' occured; Source: '{2}';", Environment.NewLine, innerEx.Message, innerEx.Source);

                innerEx = innerEx.InnerException;
            }
            result += string.Format("{0}{1}{0}", Environment.NewLine, ex.StackTrace);

            if (whereCathched != null)
                result = string.Format(WhereCatchedFormat, whereCathched, result);

            return result;
        }
    }

    public class PercentageProgress
    {
        public class PercentageProgressEventArgs : EventArgs
        {
            public PercentageProgressEventArgs() { }
            public PercentageProgressEventArgs(float value)
            {
                Value = value;
            }

            public readonly float Value = 0;
        }

        public PercentageProgress() { }

        private object childLocks = new Object();
        private List<PercentageProgress> childs = new List<PercentageProgress>();

        private float value = 0;
        public float Value
        {
            get
            {
                lock (childLocks)
                    return (childs.Count == 0) ? value : (childs.Sum( i => i.Value ) / childs.Count);
            }
            set
            {
                bool needRaise = false;

                lock (childLocks)
                    if (childs.Count == 0)
                    {
                        if (value > 100 || value < 0)
                            throw new ArgumentException("Значение должно быть в диапазоне от 0 до 100");

                        this.value = value;
                        needRaise = true;
                    }
                    else
                    {
                        foreach (var c in childs)
                            c.value = value;
                    }

                if (needRaise)
                    RaiseChange();
                //    throw new ArgumentException("Нельзя задать значения для элемента, у которого есть наследники");
            }
        }
        public bool HasChilds
        {
            get { lock(childLocks) return childs.Count > 0; }
        }

        public PercentageProgress GetChild(float value = 0)
        {
            PercentageProgress result = new PercentageProgress() { Value = value };
            result.Change += child_Change;
            lock (childLocks)
            { 
                childs.Add(result);
            }
            RaiseChange();
            return result;
        }

        public void RemoveChild(PercentageProgress child)
        {
            lock (childLocks)
            if (childs.Contains(child))
            {
                child.Change -= child_Change;
                childs.Remove(child);
            }
            RaiseChange();
        }

        private void child_Change(object sender, PercentageProgressEventArgs e)
        {
            RaiseChange();
        }

        private void RaiseChange()
        {
            if (Change != null)
                Change(this, new PercentageProgressEventArgs(Value));
        }

        public event EventHandler<PercentageProgressEventArgs> Change;
    }

    public class PropertyChangedBase : System.ComponentModel.INotifyPropertyChanged
    {
        private class RaiseItem
        {
            public string PropertyName;
            public string[] AfterPropertyNames;
        }

        #region Property changed

        private List<RaiseItem> afterItems = new List<RaiseItem>();
        private List<RaiseItem> beforeItems = new List<RaiseItem>();

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                foreach (var item in beforeItems)
                    if (item.AfterPropertyNames.Contains(propertyName))
                        RaisePropertyChange(item.PropertyName);
                    

                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

                foreach (var item in afterItems)
                    if (item.AfterPropertyNames.Contains(propertyName))
                        RaisePropertyChange(item.PropertyName);
            }
        }
        protected void RaisePropertyAfterChange(string[] afterPropertyNames, string propertyName)
        {
            afterItems.Add(new RaiseItem() { PropertyName = propertyName, AfterPropertyNames = afterPropertyNames });
        }
        protected void RaisePropertyBeforeChange(string[] beforePropertyNames, string propertyName)
        {
            beforeItems.Add(new RaiseItem() { PropertyName = propertyName, AfterPropertyNames = beforePropertyNames });
        }

        #endregion
    }

    public static class Extensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            //HashSet<TKey> knownKeys = new HashSet<TKey>();
            //foreach (TSource element in source)
            //{
            //    if (knownKeys.Add(keySelector(element)))
            //    {
            //        yield return element;
            //    }
            //}

            return source.GroupBy(keySelector).Select(grp => grp.First());
        }

        public static bool Like(this string strVal, string mask, bool ignoreCase = true)
        {
            return StringLikes(strVal, mask, ignoreCase);
        }

        public static bool StringLikes(string source, string arguments, bool ignoreCase = true)
        {
            try
            {
                string str = "^" + Regex.Escape(arguments);
                str = str.Replace("\\*", ".*").Replace("\\?", ".") + "$";

                bool result = (Regex.IsMatch(source, str, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None));
                return result;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool IsDesignMode(this object obj)
        {
            return System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
        }

        public static void CopyObject<fromType, toType>(fromType from, toType to)
        {
            if (from == null || to == null)
                return;

            var piToItems = typeof(toType).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Where(pi => pi.CanWrite).ToArray();
            var piFromItems = typeof(fromType).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).ToArray();

            foreach(var piTo in piToItems)
            {
                var piFrom = piFromItems.FirstOrDefault(p => p.Name == piTo.Name);
                if (piFrom != null)
                {
                    object value = piFrom.GetValue(from, null);
                    if (value == null)
                        piTo.SetValue(to, value, null); else
                        piTo.SetValue(to, System.Convert.ChangeType(value, piTo.PropertyType), null); 
                }
            }
        }
    }
}
