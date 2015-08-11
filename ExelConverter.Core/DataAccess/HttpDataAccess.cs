using ExelConverter.Core.DataObjects;
using Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using HtmlAgilityPack;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace ExelConverter.Core.DataAccess
{
    public class HttpDataAccessQueueParameters
    {
        public HttpDataAccessQueueParameters()
        {
            OperatorID = -1;
            FilePath = string.Empty;
            Activate = true;
            CoordinatesApproved = false;
            UseQueue = true;
            Timeout = 5;
        }

        public long OperatorID { get; set; }
        public string FilePath { get; set; }
        public bool Activate { get; set; }
        public bool CoordinatesApproved { get; set; }
        public bool UseQueue { get; set; }
        public int Timeout { get; set; }
    }

    public class HttpDataClient
    {
        private static HttpDataClient _default = null;
        public static HttpDataClient Default { get { return _default; } }
        #region Properties

#if DEBUG
        public HttpDataClient(bool userDataRewritable = false) : this()
        {
            IsUserDataRewritable = userDataRewritable;
        }
#endif

        private static string PathLogin = @"/converter/get-connection-config/";
        private static string PathGetResourceList = @"converter/get-resources-urls/";
        private static string PathAddFillRectangle = @"ajax/save-company-import-rectangle/";
        private static string PathRemoveFillRectangle = @"ajax/delete-company-import-rectangle/";
        private static string PathUploadToQueue = @"resources/import-queue/";
        private static string PathWebLogin = @"auth/login";
        private static string PathWebLogout = @"logout";
        private const bool DefaultHashPassword = false;

        private static bool? isWebDebug = null;
        private static bool IsWebDebug
        {
            get
            {
                if (isWebDebug == null)
                {
                    isWebDebug = Environment.GetCommandLineArgs().Any(i => i.Like("_webDebug"));
                }
                return isWebDebug.Value;
            }
        }

        private bool IsUserDataRewritable = false;

        public WebProxy Proxy { get; private set; }
        public CookieContainer Cookies { get; private set; }
        public string Server { get; private set; }

        private string userLogin = string.Empty;
        public string UserLogin { get { return userLogin; } set { if (!IsUserDataRewritable) throw new Exception("This class is not rewritable"); userLogin = value; } }

        private string userPassword = string.Empty;
        public string UserPassword { get { return userPassword; } set { if (!IsUserDataRewritable) throw new Exception("This class is not rewritable"); userPassword = value; } }

        public string UserPasswordHash { get { return Md5Hash(UserPassword); } }

        public string ConnectionStringMain { get; private set; }
        public string ConnectionStringApp { get; private set; }

        public bool IsLogined { get { return !string.IsNullOrWhiteSpace(UserLogin); } }
        public bool IsWebLogined { get; private set; }
        public bool HasConnections { get { return !string.IsNullOrWhiteSpace(ConnectionStringMain) && !string.IsNullOrWhiteSpace(ConnectionStringApp); } }

        public HttpDataClient()
        {
            Proxy = null;
            Cookies = null;
            Server = string.Empty;
            userLogin = string.Empty;
            userPassword = string.Empty;
            ConnectionStringMain = string.Empty;
            ConnectionStringApp = string.Empty;
            IsWebLogined = false;
            IsUserDataRewritable = false;
        }

        #endregion
        #region Http methods

        public void SetAsDefault()
        {
            _default = this;
        }

        public void Login(string server, string userLogin, string userPassword, WebProxy proxy = null)
        {
            bool wasException = false;
            var logSession = Helpers.Log.SessionStart(string.Format("HttpDataClient.Login(server:'{0}',userLogin:'{1}')", server, userLogin), true);
            try
            {
                CookieContainer cookies = new CookieContainer();
                var strRequest = GetLoginPasswordCouple(userLogin, userPassword);

                Helpers.Log.Add(logSession, "Send request to server...");

                string result = Post(GetUrl(server, PathLogin), strRequest, cookies, proxy);

                if (string.IsNullOrWhiteSpace(result))
                    throw new Exception("Сервер вернул пустую строку.");

                using(MemoryStream ms = new MemoryStream())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(result);
                    ms.Write(bytes, 0, bytes.Length);
                    ms.Seek(0, SeekOrigin.Begin);

                    var conf = XDocument.Load(ms);
                    var error = conf.Document.Descendants("error").FirstOrDefault();
                    if (error != null)
                        throw new Exception(error.Value);

                    Server = server;
                    this.userLogin = userLogin;
                    this.userPassword = userPassword;
                    Cookies = cookies;
                    Proxy = proxy;

                    var connectionStrings = conf.Document.Descendants("connection");
                    XElement mainConnectionString = connectionStrings.Where(i => i.Attribute("type").Value == "main").FirstOrDefault();
                    XElement appConnectionString = connectionStrings.Where(i => i.Attribute("type").Value == "app").FirstOrDefault();

                    if (mainConnectionString != null)
                        ConnectionStringMain = GetConnectionString(mainConnectionString);

                    if (appConnectionString != null)
                        ConnectionStringApp = GetConnectionString(appConnectionString);
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Helpers.Log.Add(logSession, ex.GetExceptionText());
                throw new Exception(string.Format("При подлючении к серверу '{0}' (логин: '{1}') произошла ошибка: '{2}';", server, userLogin, ex.Message), ex);
            }
            finally
            {
                Helpers.Log.SessionEnd(logSession, wasException || IsWebDebug);
            }
        }

        public void WebLogin()
        {
            WebLogin(Server, UserLogin, UserPassword, Cookies, Proxy);
            IsWebLogined = true;
        }

        public void WebLogout()
        {
            if (IsWebLogined)
            { 
                WebLogout(Server, Cookies, Proxy);
                IsWebLogined = false;
            }
        }

        public void AddFillRectangle(FillArea rect)
        {
            bool wasException = false;
            var logSession = Helpers.Log.SessionStart(string.Format("HttpDataClient.AddFillRectangle(operator:'{0}')", rect.FKOperatorID), true);
            try
            {
                var strRequest = GetLoginPasswordCouple() +
                                "&company[0]=" + rect.FKOperatorID +
                                "&data[0][height]=" + rect.Height +
                                "&data[0][type]=" + rect.Type +
                                "&data[0][width]=" + rect.Width +
                                "&data[0][x1]=" + rect.X1 +
                                "&data[0][x2]=" + rect.X2 +
                                "&data[0][y1]=" + rect.Y1 +
                                "&data[0][y2]=" + rect.Y2;
                Helpers.Log.Add(logSession, "Client query: " + strRequest.Replace(UserPassword, "*"));
                var result = Post(GetUrl(PathAddFillRectangle), Uri.EscapeUriString(strRequest), Cookies, Proxy);
                Helpers.Log.Add(logSession, "Server answer: " + result);
                if (result == "error")
                {
                    throw new InvalidDataException("Сервер сообщает об ошибке");
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException || IsWebDebug);
            }
        }

        public void RemoveFillRectangle(int id)
        {
            bool wasException = false;
            var logSession = Helpers.Log.SessionStart(string.Format("HttpDataClient.RemoveFillRectangle(operator:'{0}')", id), true);
            try
            {
                var strRequest = GetLoginPasswordCouple() + "&id=" + id;
                Helpers.Log.Add(logSession, "Client query: " + strRequest.Replace(UserPassword, "*"));
                var result = Post(GetUrl(PathRemoveFillRectangle), Uri.EscapeUriString(strRequest), Cookies, Proxy);
                Helpers.Log.Add(logSession, "Server answer: " + result);
                if (result == "error")
                {
                    throw new InvalidDataException("Сервер сообщает об ошибке");
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException || IsWebDebug);
            }
        }

        public void GetResourcesList(long companyId, IList<DataWriter.ReExportData> idsToGet, out string map, out string pdf)
        {
            bool wasException = false;
            var logSession = Helpers.Log.SessionStart(string.Format("HttpDataClient.GetResourcesList(companyId:'{0}')", companyId), true);
            try
            {
                string items = string.Empty;
                foreach (string item in idsToGet.Select(i => i.Code).Distinct())
                    items += string.Format("&codes[]={0}", Uri.EscapeDataString(item));

                var strRequest = string.Format("{0}&companyId={1}{2}", GetLoginPasswordCouple(), companyId, items);
                Helpers.Log.Add(logSession, "Client query: " + strRequest.Replace(UserPassword, "*"));
                //var result = Post(GetUrl(PathGetResourceList), strRequest, Cookies, Proxy);
                var result = Post(GetUrl(PathGetResourceList), strRequest, Cookies, Proxy);
                Helpers.Log.Add(logSession, "Server answer: " + result);

                var serializer = new JavaScriptSerializer();
                serializer.RegisterConverters(new[] { new DynamicJsonConverter() });
                dynamic data = serializer.Deserialize(result, typeof(object));

                if (!string.IsNullOrWhiteSpace(data["error"]))
                {
                    throw new InvalidDataException(data["error"]);
                }

                if (data["resources"] != null)
                {
                    foreach (KeyValuePair<string,object> res in data["resources"].Dictionary)
                    {
                        dynamic dynItem = res.Value;
                        foreach(var item in idsToGet.Where(i => i.Code.ToLower() == res.Key.ToLower()))
                        {
                            item.LinkPhoto = dynItem["photo"];
                            item.LinkLocation = dynItem["location"];
                            item.LinkMap = dynItem["map"];
                        }
                    }

                    //foreach (var item in idsToGet)
                    //{
                    //    dynamic dynItem = data["resources"][item.Code];
                    //    if (dynItem != null)
                    //    {
                    //        item.LinkPhoto = dynItem["photo"];
                    //        item.LinkLocation = dynItem["location"];
                    //        item.LinkMap = dynItem["map"];
                    //    }
                    //}
                }
                map = data["map"];
                pdf = data["pdf"];
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException || IsWebDebug);
            }
        }

        public void UploadFileToQueue(HttpDataAccessQueueParameters param)
        {
            UploadFileToQueue(
                param.OperatorID,
                param.FilePath,
                param.Activate,
                param.CoordinatesApproved,
                param.UseQueue,
                param.Timeout
                );
        }

        public void UploadFileToQueue(long operatorId, string filePath, bool activate, bool coordinatesApproved, bool useQueue, int curlTimeout)
        {
            bool wasException = false;
            var logSession = Helpers.Log.SessionStart(string.Format("HttpDataClient.UploadFileToQueue(operatorId:'{0}',activate:'{1}',coordinatesApproved:'{2}',useQueue:'{3}',curlTimeout:'{4}')", operatorId, activate, coordinatesApproved, useQueue, curlTimeout), true);
            try
            {
                if (!IsWebLogined)
                    WebLogin();

                NameValueCollection nvc = new NameValueCollection() {
                    { "companyId", operatorId.ToString() },  //Оператор
                    { "params[activate]", activate ? "1" : "0" }, //автоматически активировать плоскости после импорта
                    { "params[coordinatesApproved]", coordinatesApproved ? "1" : "0" }, //загружать координаты как оператор (если нет - как РА)
                    { "params[useQueue]", useQueue ? "1" : "0" }, //Грузить в очереди
                    { "params[curlTimeout]", curlTimeout.ToString() }, //Таймаут
                    { "addToQueue", "Добавить в очередь" }
                };

                //nvc.Add("login", UserLogin); //Имя пользователя
                //nvc.Add("password", UserPassword); //Пароль
                //nvc.Add("companyId", operatorId.ToString()); //Оператор
                //nvc.Add("params[activate]", activate ? "1" : "0"); //автоматически активировать плоскости после импорта
                //nvc.Add("params[coordinatesApproved]", coordinatesApproved ? "1" : "0"); //загружать координаты как оператор (если нет - как РА)
                //nvc.Add("params[useQueue]", useQueue ? "1" : "0"); //Грузить в очереди
                //nvc.Add("params[curlTimeout]", curlTimeout.ToString()); //Таймаут
                //nvc.Add("addToQueue", "Добавить в очередь");
                HttpUploadFile(
                    GetUrl(PathUploadToQueue),
                    filePath,
                    "csv",
                    "csv/plain",
                    nvc,
                    Cookies,
                    Proxy);
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException || IsWebDebug);
            }
        }

        #endregion
        #region Object to static links

        private string GetUrl(string subUrl)
        {
            return GetUrl(Server, subUrl);
        }

        private string GetLoginPasswordCouple(bool hashPassword = DefaultHashPassword)
        {
            return GetLoginPasswordCouple(UserLogin, UserPassword, hashPassword);
        }

        #endregion
        #region Static

        private static string GetConnectionString(XElement element)
        {
            bool wasException = false;
            var logSession = Helpers.Log.SessionStart(string.Format("HttpDataClient.GetConnectionString(element:'{0}')", element.ToString()), true);
            try
            {
                return GetConnectionString(
                    element.Attribute("host").Value, 
                    element.Attribute("username").Value, 
                    element.Attribute("password").Value, 
                    element.Attribute("dbname").Value
                    );
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException);
            }
        }

        private static string GetConnectionString(string server, string user, string password, string database)
        {
            return new MySql.Data.MySqlClient.MySqlConnectionStringBuilder()
            {
                Password = password,
                Server = server,
                UserID = user,
                Database = database,
                ConvertZeroDateTime = true,
                AllowZeroDateTime = true,
                DefaultCommandTimeout = 15,
                ConnectionTimeout = 15,
                CharacterSet = "utf8"
            }.ConnectionString;
        }

        private static string GetUrl(string server, string subUrl)
        {
            return (new Uri(new Uri(server, UriKind.Absolute), new Uri(subUrl, UriKind.Relative))).AbsoluteUri;
        }

        private static string Md5Hash(string value)
        {
            var result = string.Empty;
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                result = sBuilder.ToString();
            }
            return result;
        }

        private static string GetLoginPasswordCouple(string userLogin, string userPassword, bool hashPassword = DefaultHashPassword)
        {
            return string.Format("login={0}&password={1}", userLogin, hashPassword ? Md5Hash(userPassword) : userPassword);
        }

        private static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static string Post(string url, string data, CookieContainer cookies, WebProxy proxy)
        {
            string Out = string.Empty;
            bool wasException = false;
            var logSession = Helpers.Log.SessionStart(string.Format("HttpDataClient.Post(url:'{0}')", url), true);
            try
            {
                var oldServerCertificateValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = "POST";
                    req.Timeout = 10000;
                    req.ContentType = "application/x-www-form-urlencoded";
                    req.KeepAlive = true;
                    req.Credentials = System.Net.CredentialCache.DefaultCredentials;
                    req.Proxy = proxy;
                    req.CookieContainer = cookies;

                    byte[] sentData = Encoding.GetEncoding(1251).GetBytes(data);
                    req.ContentLength = sentData.Length;
                    using (System.IO.Stream sendStream = req.GetRequestStream())
                    {
                        sendStream.Write(sentData, 0, sentData.Length);
                        sendStream.Close();

                        using (HttpWebResponse wresp = (HttpWebResponse)req.GetResponse())
                        {
                            if (wresp.StatusCode == HttpStatusCode.OK)
                                using (Stream stream2 = wresp.GetResponseStream())
                                {
                                    System.IO.StreamReader sr = new System.IO.StreamReader(stream2, Encoding.UTF8);

                                    Char[] read = new Char[256];
                                    int count = sr.Read(read, 0, 256);
                                    while (count > 0)
                                    {
                                        String str = new String(read, 0, count);
                                        Out += str;
                                        count = sr.Read(read, 0, 256);
                                    }
                                }
                            else
                                throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));
                        }
                    }
                }
                finally
                {
                    ServicePointManager.ServerCertificateValidationCallback = oldServerCertificateValidationCallback;
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException || IsWebDebug);
            }

            return Out;
        }

        private static void HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc, CookieContainer cookies, WebProxy proxy)
        {
            bool wasException = false;

            var logSession = Log.SessionStart(string.Format("HttpDataClient.HttpUploadFile('{0}' to '{1}')", file, url), true);

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            try
            {
                var oldServerCertificateValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

                try
                {
                    HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                    wr.ContentType = "multipart/form-data; boundary=" + boundary;
                    wr.Method = "POST";
                    wr.KeepAlive = true;
                    wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
                    wr.Proxy = proxy;
                    wr.CookieContainer = cookies;

                    using (Stream rs = wr.GetRequestStream())
                    {

                        string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                        foreach (string key in nvc.Keys)
                        {
                            rs.Write(boundarybytes, 0, boundarybytes.Length);
                            string formitem = string.Format(formdataTemplate, key, nvc[key]);
                            byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                            rs.Write(formitembytes, 0, formitembytes.Length);
                        }
                        rs.Write(boundarybytes, 0, boundarybytes.Length);

                        string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                        string header = string.Format(headerTemplate, paramName, file, contentType);
                        byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                        rs.Write(headerbytes, 0, headerbytes.Length);

                        FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                        byte[] buffer = new byte[4096];
                        int bytesRead = 0;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            rs.Write(buffer, 0, bytesRead);
                        }
                        fileStream.Close();

                        byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                        rs.Write(trailer, 0, trailer.Length);
                        rs.Close();

                        #region Response
                        try
                        {
                            using (HttpWebResponse wresp = (HttpWebResponse)wr.GetResponse())
                            {
                                if (wresp.StatusCode == HttpStatusCode.OK)
                                    using (Stream stream2 = wresp.GetResponseStream())
                                    {
                                        HtmlDocument doc = new HtmlDocument();
                                        doc.Load(stream2, true);
                                        var nodes = doc.DocumentNode.SelectNodes("//p[@id='pErrors']");
                                        if (nodes != null && nodes.Count > 0)
                                        {
                                            throw new ApplicationException(nodes[0].InnerText.Trim());
                                        }

                                        string addedText = "Задание поставлено в очередь";

                                        nodes = doc.DocumentNode.SelectNodes("//div[@class='stat-padd']");
                                        if (nodes != null && nodes.Count > 0 && nodes[0].InnerText.Trim().Contains(addedText))
                                        {
                                            string txt = nodes[0].InnerText.Trim();
                                            txt = txt.Substring(txt.IndexOf(addedText) + addedText.Length + 2);
                                            txt = txt.Substring(0, txt.IndexOf("\n") - 1);
                                            Log.Add(logSession, string.Format("file added '{0}'", txt));
                                        }
                                        else
                                            throw new ApplicationException("По неизвестным причинам файл не был добавлен в очередь");

                                        //Log.Add(string.Format("HttpDataAccess.HttpUploadFile() :: file uploaded, server response is: {0}", doc.DocumentNode.InnerHtml));
                                    }
                                else
                                    throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));
                            }
                        }
                        finally
                        {
                            wr = null;
                        }
                        #endregion
                    }
                }
                finally
                {
                    ServicePointManager.ServerCertificateValidationCallback = oldServerCertificateValidationCallback;
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException || IsWebDebug);
            }
        }

        private static void WebLogout(string server, CookieContainer cookies, WebProxy proxy)
        {
            if (cookies != null)
            {
                bool wasError = false;
                var logSession = Log.SessionStart(string.Format("HttpDataClient.WebLogout(server:'{0}')", server), true);
                try
                {
                    var oldServerCertificateValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
                    ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                    try
                    {
                        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(GetUrl(server, PathWebLogout));
                        wr.Method = "GET";
                        wr.KeepAlive = true;
                        wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
                        wr.CookieContainer = cookies;
                        wr.Proxy = proxy;
                        wr.Timeout = 3000;
                        using (HttpWebResponse wresp = (HttpWebResponse)wr.GetResponse())
                        {
                            if (wresp.StatusCode != HttpStatusCode.OK)
                                throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));
                        }
                    }
                    finally
                    {
                        ServicePointManager.ServerCertificateValidationCallback = oldServerCertificateValidationCallback;
                    }
                }
                catch (Exception ex)
                {
                    wasError = true;
                    Log.Add(logSession, ex.GetExceptionText());
                    throw ex;
                }
                finally
                {
                    Log.SessionEnd(logSession, wasError || IsWebDebug);
                }
            }
        }

        private static void WebLogin(string server, string userName, string userPassword, CookieContainer cookies, WebProxy proxy)
        {
            bool wasException = false;
            var logSession = Log.SessionStart(string.Format("HttpDataClient.WebLogin(server:'{0}',user:'{1}')", server, userName), true);

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            try
            {
                var oldServerCertificateValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(GetUrl(server, PathWebLogin));
                try
                {
                    wr.ContentType = "multipart/form-data; boundary=" + boundary;
                    wr.Method = "POST";
                    wr.Timeout = 10000;
                    wr.KeepAlive = true;
                    wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
                    wr.CookieContainer = cookies;
                    wr.Proxy = proxy;
                    using (Stream rs = wr.GetRequestStream())
                    {
                        NameValueCollection nvc = new NameValueCollection();
                        nvc.Add("username", userName);
                        nvc.Add("password", userPassword);

                        string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                        foreach (string key in nvc.Keys)
                        {
                            rs.Write(boundarybytes, 0, boundarybytes.Length);
                            string formitem = string.Format(formdataTemplate, key, nvc[key]);
                            byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                            rs.Write(formitembytes, 0, formitembytes.Length);
                        }
                        //rs.Write(boundarybytes, 0, boundarybytes.Length);

                        rs.Write(trailer, 0, trailer.Length);
                        rs.Close();

                        using (HttpWebResponse wresp = (HttpWebResponse)wr.GetResponse())
                        {
                            if (wresp.StatusCode == HttpStatusCode.OK)
                                using (Stream stream2 = wresp.GetResponseStream())
                                {
                                    HtmlDocument doc = new HtmlDocument();
                                    doc.Load(stream2, true);
                                    var nodes = doc.DocumentNode.SelectNodes("//p[@id='pErrors']");
                                    if (nodes != null && nodes.Count > 0)
                                        throw new ApplicationException(nodes[0].InnerText.Trim());

                                    var node2 = doc.DocumentNode.SelectSingleNode("//div[@class='welcome']");
                                    if (node2 == null)
                                        throw new ApplicationException("Отсутствует подтверждение логина");

                                    //Log.Add(string.Format("Authenticated, server response is: {0}", doc.DocumentNode.InnerHtml )); 


                                    //using (StreamReader reader2 = new StreamReader(stream2))
                                    //{
                                    //    Log.Add(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));
                                    //}
                                }
                            else
                                throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));

                        }
                    }
                }
                finally
                {
                    wr = null;
                    ServicePointManager.ServerCertificateValidationCallback = oldServerCertificateValidationCallback;
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Log.Add(logSession, ex.GetExceptionText());
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException || IsWebDebug);

            }
        }

        #endregion
    }
#region Old
    //public static class HttpDataAccess2
    //{
    //    private static string httpServerName = string.Empty;
    //    public static string HttpServerName
    //    {
    //        get 
    //        {
    //            return httpServerName;
    //        }
    //        set
    //        {
    //            if (httpServerName == value)
    //                return;
    //            httpServerName = value;
    //        }
    //    }
    //    //done
    //    private static string GetUrl(string subUrl)
    //    {
    //        return (new Uri(new Uri(HttpServerName, UriKind.Absolute), new Uri(subUrl, UriKind.Relative))).AbsoluteUri;
    //    }
    //    //done
    //    public static void AddFillRectangle(FillArea rect)
    //    {
    //        var pwd = "this_is_test_pwd";
    //        var hash = GetMd5Hash(pwd);

    //        //var hash = PasswordHash;

    //        var strRequest = "company[0]=" + rect.FKOperatorID + 
    //                         "&data[0][height]=" + rect.Height +
    //                         "&data[0][type]=" + rect.Type + 
    //                         "&data[0][width]=" + rect.Width + 
    //                         "&data[0][x1]=" + rect.X1 + 
    //                         "&data[0][x2]=" + rect.X2 + 
    //                         "&data[0][y1]=" + rect.Y1 + 
    //                         "&data[0][y2]=" + rect.Y2 + 
    //                         "&converterPwd=" + hash;



    //        var result = Post(GetUrl("ajax/save-company-import-rectangle/"), strRequest);
    //        if (result == "error")
    //        {
    //            throw new InvalidDataException();
    //        }
    //    }
    //    //done
    //    public static void RemoveFillRectangle(int id)
    //    {
    //        var pwd = "this_is_test_pwd";
    //        var hash = GetMd5Hash(pwd);

    //        //var hash = PasswordHash;

    //        var strRequest = "id="+id+"&converterPwd=" + hash;

    //        var result = Post(GetUrl("ajax/delete-company-import-rectangle/"), strRequest);
    //        if (result == "error")
    //        {
    //            throw new InvalidDataException();
    //        }
    //    }
    //    //done
    //    private static string Post(string Url, string Data)
    //    {
    //        string Out = string.Empty;

    //        //System.Net.WebRequest req = System.Net.WebRequest.Create(Url);

    //        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
    //        req.Method = "POST";
    //        req.Timeout = 100000;
    //        req.ContentType = "application/x-www-form-urlencoded";
    //        req.KeepAlive = true;
    //        req.Credentials = System.Net.CredentialCache.DefaultCredentials;
    //        //req.CookieContainer = AuthCookieContainer;

    //        byte[] sentData = Encoding.GetEncoding(1251).GetBytes(Data);
    //        req.ContentLength = sentData.Length;
    //        using (System.IO.Stream sendStream = req.GetRequestStream())
    //        {
    //            sendStream.Write(sentData, 0, sentData.Length);
    //            sendStream.Close();

    //            using (HttpWebResponse wresp = (HttpWebResponse)req.GetResponse())
    //            {
    //                if (wresp.StatusCode == HttpStatusCode.OK)
    //                    using (Stream stream2 = wresp.GetResponseStream())
    //                    {
    //                        System.IO.StreamReader sr = new System.IO.StreamReader(stream2, Encoding.UTF8);

    //                        Char[] read = new Char[256];
    //                        int count = sr.Read(read, 0, 256);
    //                        while (count > 0)
    //                        {
    //                            String str = new String(read, 0, count);
    //                            Out += str;
    //                            count = sr.Read(read, 0, 256);
    //                        }
    //                    }
    //                else
    //                    throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));
    //            }


    //            //System.Net.WebResponse res = req.GetResponse();
    //            //using (System.IO.Stream ReceiveStream = res.GetResponseStream())
    //            //{
    //            //    System.IO.StreamReader sr = new System.IO.StreamReader(ReceiveStream, Encoding.UTF8);

    //            //    Char[] read = new Char[256];
    //            //    int count = sr.Read(read, 0, 256);
    //            //    while (count > 0)
    //            //    {
    //            //        String str = new String(read, 0, count);
    //            //        Out += str;
    //            //        count = sr.Read(read, 0, 256);
    //            //    }
    //            //}
    //        }
    //        return Out;
    //    }
    //    //done
    //    public static void UploadFileToQueue(HttpDataAccessQueueParameters param)
    //    {
    //        UploadFileToQueue(
    //            param.OperatorID,
    //            param.FilePath,
    //            param.Activate,
    //            param.CoordinatesApproved,
    //            param.UseQueue,
    //            param.Timeout
    //            );
    //    }
    //    //done
    //    public static void UploadFileToQueue(long operatorId, string filePath, bool activate, bool coordinatesApproved, bool useQueue, int curlTimeout)
    //    {
    //        NameValueCollection nvc = new NameValueCollection();
    //        nvc.Add("companyId", operatorId.ToString()); //Оператор
    //        nvc.Add("params[activate]", activate ? "1" : "0"); //автоматически активировать плоскости после импорта
    //        nvc.Add("params[coordinatesApproved]", coordinatesApproved ? "1" : "0"); //загружать координаты как оператор (если нет - как РА)
    //        nvc.Add("params[useQueue]", useQueue ? "1" : "0"); //Грузить в очереди
    //        nvc.Add("params[curlTimeout]", curlTimeout.ToString()); //Таймаут
    //        nvc.Add("addToQueue", "Добавить в очередь");
    //        HttpUploadFile(
    //            GetUrl("resources/import-queue/"),
    //            filePath,
    //            "csv",
    //            "csv/plain",
    //            nvc);
    //    }

    //    private static string login = string.Empty;
    //    public static string Login { get { return login; } set { login = value; authCookieContainer = null; } }
    //    private static string password = string.Empty;
    //    public static string Password { get { return password; } set { password = value; authCookieContainer = null; } }
    //    public static string PasswordHash { get { return GetMd5Hash(password); } }

    //    public static bool Authenticated
    //    {
    //        get
    //        {
    //            return authCookieContainer != null;
    //        }
    //    }

    //    private static CookieContainer authCookieContainer = null;
    //    public static CookieContainer AuthCookieContainer
    //    {
    //        get
    //        {
    //            if (authCookieContainer != null)
    //                return authCookieContainer;

    //            authCookieContainer = new CookieContainer();
    //            try
    //            {
    //                NameValueCollection nvc = new NameValueCollection();
    //                nvc.Add("username", Login);
    //                nvc.Add("password", Password);
    //                Auth(nvc, authCookieContainer);
    //            }
    //            catch(Exception ex)
    //            {
    //                authCookieContainer = null;
    //                throw ex;
    //            }
    //            return authCookieContainer;
    //        }
    //    }

    //    public static void LogOut()
    //    {
    //        if (authCookieContainer != null)
    //            try
    //            {
    //                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(GetUrl("logout"));
    //                wr.Method = "GET";
    //                wr.KeepAlive = true;
    //                wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
    //                wr.CookieContainer = AuthCookieContainer;
    //                wr.Timeout = 3000;
    //                using (HttpWebResponse wresp = (HttpWebResponse)wr.GetResponse())
    //                {
    //                    if (wresp.StatusCode != HttpStatusCode.OK)
    //                        throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Log.Add(string.Format("HttpDataAccess.LogOut() :: exception: {1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
    //                throw ex;
    //            }
    //    }

    //    public static void Auth(NameValueCollection nvc, CookieContainer container)
    //    {
    //        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
    //        byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
    //        byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

    //        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(GetUrl("auth/login"));
    //        try
    //        {
    //            wr.ContentType = "multipart/form-data; boundary=" + boundary;
    //            wr.Method = "POST";
    //            wr.KeepAlive = true;
    //            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
    //            wr.CookieContainer = container;
    //            using (Stream rs = wr.GetRequestStream())
    //            {
    //                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
    //                foreach (string key in nvc.Keys)
    //                {
    //                    rs.Write(boundarybytes, 0, boundarybytes.Length);
    //                    string formitem = string.Format(formdataTemplate, key, nvc[key]);
    //                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
    //                    rs.Write(formitembytes, 0, formitembytes.Length);
    //                }
    //                //rs.Write(boundarybytes, 0, boundarybytes.Length);
                   
    //                rs.Write(trailer, 0, trailer.Length);
    //                rs.Close();

    //                try
    //                {
    //                    using (HttpWebResponse wresp = (HttpWebResponse)wr.GetResponse())
    //                    {
    //                        if (wresp.StatusCode == HttpStatusCode.OK)
    //                            using (Stream stream2 = wresp.GetResponseStream())
    //                            {
    //                                HtmlDocument doc = new HtmlDocument();
    //                                doc.Load(stream2, true);
    //                                var nodes = doc.DocumentNode.SelectNodes("//p[@id='pErrors']");
    //                                if (nodes != null && nodes.Count > 0)
    //                                    throw new ApplicationException(nodes[0].InnerText.Trim());

    //                                var node2 = doc.DocumentNode.SelectSingleNode("//div[@class='welcome']");
    //                                if (node2 == null)
    //                                    throw new ApplicationException("Отсутствует подтверждение логина");

    //                                //Log.Add(string.Format("Authenticated, server response is: {0}", doc.DocumentNode.InnerHtml )); 


    //                                //using (StreamReader reader2 = new StreamReader(stream2))
    //                                //{
    //                                //    Log.Add(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));
    //                                //}
    //                            }
    //                        else
    //                            throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));
                            
    //                    }
    //                }
    //                catch (Exception ex)
    //                {
    //                    Log.Add(string.Format("HttpDataAccess.Auth() :: exception: {1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
    //                    throw ex;
    //                }

    //            }
    //        }
    //        finally
    //        {
    //            wr = null;
    //        }   
    //    }

    //    //done
    //    public static void HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc)
    //    {
    //        Log.Add(string.Format("Uploading {0} to {1}", file, url));
    //        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
    //        byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

    //        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
    //        wr.ContentType = "multipart/form-data; boundary=" + boundary;
    //        wr.Method = "POST";
    //        wr.KeepAlive = true;
    //        wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
    //        wr.CookieContainer = AuthCookieContainer;

    //        using (Stream rs = wr.GetRequestStream())
    //        {

    //            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
    //            foreach (string key in nvc.Keys)
    //            {
    //                rs.Write(boundarybytes, 0, boundarybytes.Length);
    //                string formitem = string.Format(formdataTemplate, key, nvc[key]);
    //                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
    //                rs.Write(formitembytes, 0, formitembytes.Length);
    //            }
    //            rs.Write(boundarybytes, 0, boundarybytes.Length);

    //            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
    //            string header = string.Format(headerTemplate, paramName, file, contentType);
    //            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
    //            rs.Write(headerbytes, 0, headerbytes.Length);

    //            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
    //            byte[] buffer = new byte[4096];
    //            int bytesRead = 0;
    //            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
    //            {
    //                rs.Write(buffer, 0, bytesRead);
    //            }
    //            fileStream.Close();

    //            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
    //            rs.Write(trailer, 0, trailer.Length);
    //            rs.Close();

    //            try
    //            {
    //                using (HttpWebResponse wresp = (HttpWebResponse)wr.GetResponse())
    //                {
    //                    if (wresp.StatusCode == HttpStatusCode.OK)
    //                        using (Stream stream2 = wresp.GetResponseStream())
    //                        {
    //                            HtmlDocument doc = new HtmlDocument();
    //                            doc.Load(stream2, true);
    //                            var nodes = doc.DocumentNode.SelectNodes("//p[@id='pErrors']");
    //                            if (nodes != null && nodes.Count > 0)
    //                            {
    //                                authCookieContainer = null;
    //                                throw new ApplicationException(nodes[0].InnerText.Trim());
    //                            }

    //                            string addedText = "Задание поставлено в очередь";

    //                            nodes = doc.DocumentNode.SelectNodes("//div[@class='stat-padd']");
    //                            if (nodes != null && nodes.Count > 0 && nodes[0].InnerText.Trim().Contains(addedText))
    //                            {
    //                                string txt = nodes[0].InnerText.Trim();
    //                                txt = txt.Substring(txt.IndexOf(addedText) + addedText.Length + 2);
    //                                txt = txt.Substring(0, txt.IndexOf("\n")-1);
    //                                Log.Add(string.Format("HttpDataAccess.HttpUploadFile() :: file added {0}", txt));
    //                            }
    //                            else
    //                                throw new ApplicationException("По неизвестным причинам файл не был добавлен в очередь");

    //                            //Log.Add(string.Format("HttpDataAccess.HttpUploadFile() :: file uploaded, server response is: {0}", doc.DocumentNode.InnerHtml));
    //                        }
    //                    else
    //                        throw new ApplicationException(string.Format("Ответ от сервера: {0}", wresp.StatusCode.ToString()));
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                Log.Add(string.Format("HttpDataAccess.HttpUploadFile() :: exception: {1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
    //                throw ex;
    //            }
    //            finally
    //            {
    //                wr = null;
    //            }
    //        }
    //    }
    //    //done
    //    private static string GetMd5Hash(string value)
    //    {
    //        var result = string.Empty;
    //        using (MD5 md5Hash = MD5.Create())
    //        {
    //            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
    //            StringBuilder sBuilder = new StringBuilder();
    //            for (int i = 0; i < data.Length; i++)
    //            {
    //                sBuilder.Append(data[i].ToString("x2"));
    //            }
    //            result = sBuilder.ToString();
    //        }
    //        return result;
    //    }
    //    //done
    //    public static void GetResourcesList(long companyId, IList<DataWriter.ReExportData> idsToGet, out string map, out string pdf)
    //    {
    //        var pwd = "this_is_test_pwd";
    //        var hash = GetMd5Hash(pwd);

    //        //var hash = PasswordHash;

    //        string items = string.Empty;
    //        int n = 0;
    //        foreach (string item in idsToGet.Select(i => i.Code).Distinct())
    //            items += string.Format("&codes[]={1}", n++, item);

    //        //foreach (string item in idsToGet.Select(i => i.Code).Distinct())
    //        //    items += (string.IsNullOrWhiteSpace(items) ? string.Empty : "|") +  item;
    //        //items = "&codes=" + items;

    //        var strRequest = "companyId=" + companyId +
    //                         "&converterPwd=" + hash +
    //                         items;

    //        strRequest = Uri.EscapeUriString(strRequest);

    //        var result = Post(GetUrl("converter/get-resources-urls/"), strRequest);

    //        var serializer = new JavaScriptSerializer();
    //        serializer.RegisterConverters(new[] { new DynamicJsonConverter() });
    //        dynamic data = serializer.Deserialize(result, typeof(object));

    //        if (!string.IsNullOrWhiteSpace(data["error"]))
    //        {
    //            throw new InvalidDataException(data["error"]);
    //        }
            
    //        if (data["resources"] != null)
    //        {
    //            foreach (var item in idsToGet)
    //            {
    //                dynamic dynItem = data["resources"][item.Code];
    //                if (dynItem != null)
    //                {
    //                    item.LinkPhoto = dynItem["photo"];
    //                    item.LinkLocation = dynItem["location"];
    //                    item.LinkMap = dynItem["map"];
    //                }
    //            }
    //        }

    //        map = data["map"];
    //        pdf = data["pdf"];
    //    }
    //}
#endregion
}
