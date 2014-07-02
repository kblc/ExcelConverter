using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExelConverter.Core.DataAccess;
using MySql.Data.MySqlClient;
using Helpers;
using Helpers.Serialization;
using System.Collections.Generic;
using ExelConverter.Core.Converter;
using System.IO;
using System.Diagnostics;

namespace ExcelConverter.Core.Test
{
    [TestClass]
    public class DataAccessTest
    {
        private TestContext testContextInstance;
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        #region Main

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_Test_Connection_To_DB()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                using (MySqlConnection conMain = new MySqlConnection() { ConnectionString = connectionMain })
                {
                    conMain.Open();
                }

                using (MySqlConnection conApp = new MySqlConnection() { ConnectionString = connectionApp })
                {
                    conApp.Open();
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            Assert.AreEqual(passed, shouldPass, string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetSidesNames()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            string[] result = new string[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                //using (MySqlConnection conMain = new MySqlConnection() { ConnectionString = connectionMain })
                //{
                //    conMain.Open();
                //}

                //using (MySqlConnection conApp = new MySqlConnection() { ConnectionString = connectionApp })
                //{
                //    conApp.Open();
                //}

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetSidesNames();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL",Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetSizes()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            ExelConverter.Core.DataObjects.Size[] result = new ExelConverter.Core.DataObjects.Size[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetSizes();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetCities()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            ExelConverter.Core.DataObjects.City[] result = new ExelConverter.Core.DataObjects.City[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetCities();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetLights()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new string[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetLights();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetOperators()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.DataObjects.Operator[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetOperators();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetRegions()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.DataObjects.Region[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetRegions();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetRegionsByCity()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            //var result = new ExelConverter.Core.DataObjects.Size[] { };
            bool passed = false;
            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();

                ExelConverter.Core.DataObjects.City[] tps = da.GetCities();
                int cnt = tps.Length > 5 ? 5 : tps.Length;
                for (int i = 0; i < tps.Length; i+= (int)((float)tps.Length / (float)cnt))
                {
                    var result = da.GetRegionsByCity(tps[i]);
                    //Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL for type:{1}", Environment.NewLine, tp.Name));
                    //Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY for type:{1}", Environment.NewLine, tp.Name));
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);
            Assert.AreEqual(passed, shouldPass, errMsg);
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetTypes()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.DataObjects.Type[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetTypes();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetSizesByType()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.DataObjects.Size[] { };
            bool passed = false;
            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();

                ExelConverter.Core.DataObjects.Type[] tps = da.GetTypes();
                foreach (var tp in tps)
                { 
                    result = da.GetSizesByType(tp);
                    //Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL for type:{1}", Environment.NewLine, tp.Name));
                    //Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY for type:{1}", Environment.NewLine, tp.Name));
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);
            Assert.AreEqual(passed, shouldPass, errMsg);
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetUsers()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.DataObjects.User[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                result = da.GetUsers();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_GetFillRects()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            //var result = new ExelConverter.Core.DataObjects.Size[] { };
            bool passed = false;
            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();

                ExelConverter.Core.DataObjects.Operator[] tps = da.GetOperators();

                int cnt = tps.Length >= 5 ? 5 : tps.Length;
                int step = (int)((float)tps.Length / (float)cnt);

                List<ExelConverter.Core.DataObjects.FillArea> resLst = new List<ExelConverter.Core.DataObjects.FillArea>();

                for (int i = 0; i < tps.Length; i+= step )
                {
                    foreach(var item in da.GetFillRects((int)tps[i].Id))
                        resLst.Add(item);
                    //Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL for type:{1}", Environment.NewLine, tp.Name));
                    //Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY for type:{1}", Environment.NewLine, tp.Name));
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);
            Assert.AreEqual(passed, shouldPass, errMsg);
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_FillRectExists()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            //var result = new ExelConverter.Core.DataObjects.Size[] { };
            bool passed = false;
            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                //exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();

                ExelConverter.Core.DataObjects.Operator[] tps = da.GetOperators();

                int cnt = tps.Length >= 5 ? 5 : tps.Length;
                int step = (int)((float)tps.Length / (float)cnt);

                List<ExelConverter.Core.DataObjects.FillArea> resLst = new List<ExelConverter.Core.DataObjects.FillArea>();

                for (int i = 0; i < tps.Length; i += step)
                {
                    foreach (var item in da.GetFillRects((int)tps[i].Id))
                        resLst.Add(item);
                    //Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL for type:{1}", Environment.NewLine, tp.Name));
                    //Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY for type:{1}", Environment.NewLine, tp.Name));
                }

                da.FillRectExists(resLst.ToArray());

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);
            Assert.AreEqual(passed, shouldPass, errMsg);
        }

        #endregion
        #region App

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_APP_GetRules()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.Converter.ExelConvertionRule[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                ExelConverter.Core.Settings.SettingsProvider.Initialize(da);

                var operators = da.GetOperators();

                for (int i = 0; i < Math.Min(3, operators.Length); i++)
                {
                    var id = new Random().Next(operators.Length - 1);
                    result = da.GetRules(new int[] { (int)operators[id].Id });
                }
                passed = true;
            }
            catch (Exception ex) { exception = ex.GetExceptionText(); }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_APP_GetRulesByOperator()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.Converter.ExelConvertionRule[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                ExelConverter.Core.Settings.SettingsProvider.Initialize(da);

                var ops = da.GetOperators();
                int cnt = ops.Length > 3 ? 3 : ops.Length;

                for (int i = 0; i < ops.Length; i+= (int)((float)ops.Length / (float)cnt))
                    result = da.GetRulesByOperator(ops[i]);

                passed = true;
            }
            catch (Exception ex) { exception = ex.GetExceptionText(); }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            //Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            //Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_APP_UpdateOperatorRules()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.Converter.ExelConvertionRule[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                ExelConverter.Core.Settings.SettingsProvider.Initialize(da);

                var ops = da.GetOperators();
                int cnt = ops.Length > 2 ? 2 : ops.Length;

                for (int i = 0; i < ops.Length; i += (int)((float)ops.Length / (float)cnt))
                {
                    result = da.GetRulesByOperator(ops[i]);
                    foreach (var r in result)
                        da.UpdateOperatorRules(new ExelConverter.Core.Converter.ExelConvertionRule[] { r });
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.GetExceptionText(); }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_APP_AddAndRemoveOperatorRules()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.Converter.ExelConvertionRule[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                ExelConverter.Core.Settings.SettingsProvider.Initialize(da);

                var ops = da.GetOperators();
                int cnt = ops.Length > 2 ? 2 : ops.Length;

                for (int i = 0; i < ops.Length; i += (int)((float)ops.Length / (float)cnt))
                {
                    ExelConverter.Core.Converter.ExelConvertionRule r = 
                        new ExelConverter.Core.Converter.ExelConvertionRule()
                        {
                            Name = "test_rule",
                            FkOperatorId = (int)ops[i].Id
                        };
                    da.AddOperatorRule(r);
                    da.RemoveOperatorRule(r);
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.GetExceptionText(); }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            //Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            //Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_APP_GetAndSetOperatorLockers()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;
            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.Converter.ExelConvertionRule[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                //ExelConverter.Core.Settings.SettingsProvider.Initialize(da);

                var ops = da.GetOperators();
                var usrs = da.GetUsers();

                for (int i = 0; i < 4; i++ )
                {
                    ExelConverter.Core.DataObjects.Operator op = ops[new Random().Next(0, ops.Length - 1)];
                    ExelConverter.Core.DataObjects.User usr = usrs[new Random().Next(0, usrs.Length - 1)];
                    if (da.GetOperatorLocker(op) == null)
                    {
                        if (da.SetOperatorLocker(op, usr, true)
                            && da.GetOperatorLocker(op) == usr)
                        da.SetOperatorLocker(op, usr, false);
                    }
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.GetExceptionText(); }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);

            //Assert.IsNotNull(result, errMsg + string.Format("{0}result is NULL", Environment.NewLine));
            //Assert.AreNotEqual(result.Length, 0, errMsg + string.Format("{0}result is EMPTY", Environment.NewLine));
        }

        #endregion

        //[TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\DataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\DataAccessTestData.xml",
                   "TestConnectionToDBRow",
                    DataAccessMethod.Sequential)]
        public void DataAccessTest_Get_FULL_Rules()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            string connectionMain = string.Empty;
            string connectionApp = string.Empty;

            //if (!server.Contains("alpha"))
            //    return;

            HttpDataClient test = new HttpDataClient();

            var result = new ExelConverter.Core.Converter.ExelConvertionRule[] { };

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                connectionMain = test.ConnectionStringMain;
                connectionApp = test.ConnectionStringApp;

                alphaEntities.ProviderConnectionString = connectionMain;
                exelconverterEntities2.ProviderConnectionString = connectionApp;

                DataAccess da = new DataAccess();
                ExelConverter.Core.Settings.SettingsProvider.Initialize(da);

                //var operators = da.GetOperators();
                //for (int i = 0; i < operators.Length; i++)
                //{
                //    List<ExelConvertionRule> rules = new List<ExelConvertionRule>(da.GetRules(new int[] { (int)operators[i].Id }));
                //    da.UpdateOperatorRules(rules.ToArray());

                //    //string text = rules.SerializeToXML(false);

                //    //Uri uri = new Uri(server);

                //    //string fileName = string.Format("{0}.{1}.{2}.xml", uri.Host, operators[i].Id, operators[i].Name);

                //    //foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                //    //    fileName = fileName.Replace(c.ToString(), "");

                //    //fileName = System.IO.Path.Combine(Helpers.Log.CurrentPath, fileName);

                //    //using (StreamWriter w = File.AppendText(fileName))
                //    //    w.Write(text);

                //    Console.WriteLine("({0}) operator: ({1}){2}, progress: {3}/{4}", server, operators[i].Id, operators[i].Name, i+1, operators.Length);
                //    Trace.WriteLine(string.Format("({0}) operator: ({1}){2}, progress: {3}/{4}", server, operators[i].Id, operators[i].Name, i+1, operators.Length));
                //}

                //delete old data
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(exelconverterEntities2.ProviderConnectionString))
                using (MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(@"UPDATE convertion_rules SET convertion_rule = '', convertion_rule_image = NULL;", conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                passed = true;
            }
            catch (Exception ex) 
            { 
                exception = ex.GetExceptionText();
            }

            string errMsg = string.Format("server:{1}{0}login:{2}{0}password:{3}{0}connectionMain:{4}{0}connectionApp:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, connectionMain, connectionApp, shouldPass, passed, exception);

            Assert.AreEqual(passed, shouldPass, errMsg);
        }
    }


}
