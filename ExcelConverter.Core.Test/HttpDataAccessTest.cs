using System;
using ExelConverter.Core.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Collections.Generic;
using MySql.Data;

namespace ExcelConverter.Core.Test
{
    [TestClass]
    public class HttpDataAccessTest
    {
        private TestContext testContextInstance;
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\HttpDataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\HttpDataAccessTestData.xml",
                   "LoginRow",
                    DataAccessMethod.Sequential)]
        public void HttpDataAccessTest_Login()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            HttpDataClient test = new HttpDataClient();

            bool passed = false;
            try
            {
                test.Login(server, login, password);
                passed = true;
            }
            catch { }

            Assert.AreEqual(passed, shouldPass, string.Format("server:{1}{0}login:{2}{0}password:{3}{0}shouldpass:{4}{0}passed:{5}", Environment.NewLine, server, login, password, shouldPass, passed));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\HttpDataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\HttpDataAccessTestData.xml",
                   "LoginRow",
                    DataAccessMethod.Sequential)]
        public void HttpDataAccessTest_WebLogin_And_WebLogout()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            string exception = string.Empty;
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            HttpDataClient test = new HttpDataClient();

            bool passed = false;
            try
            {
                test.Login(server, login, password);

                test.WebLogin();
                test.WebLogout();

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            Assert.AreEqual(passed, shouldPass, string.Format("server:{1}{0}login:{2}{0}password:{3}{0}shouldpass:{4}{0}passed:{5}{0}exception:{6}", Environment.NewLine, server, login, password, shouldPass, passed, exception));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\HttpDataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\HttpDataAccessTestData.xml",
                   "AddRemoveFillRectangleRow",
                    DataAccessMethod.Sequential)]
        public void HttpDataAccessTest_Add_AndRemove_Fill_Rectangle()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            int operatorId = int.Parse(testContextInstance.DataRow["OperatorId"].ToString());
            int rectangleId = int.Parse(testContextInstance.DataRow["RectangleId"].ToString());
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            bool addFillRectanglePassed = false;
            string exception = string.Empty;
            HttpDataClient test = new HttpDataClient();

            bool passed = false;
            try
            {
                test.Login(server, login, password);
                test.AddFillRectangle(new ExelConverter.Core.DataObjects.FillArea() { FKOperatorID = operatorId, Height = 100, ID = rectangleId, Type = "test_type", Width = 100, X1 = 0, X2 = 100, Y1 = 0, Y2 = 100 });

                addFillRectanglePassed = true;
                
                test.RemoveFillRectangle(rectangleId);
                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            Assert.AreEqual(passed, shouldPass, string.Format("server:{1}{0}login:{2}{0}password:{3}{0}operatorid:{4}{0}rectangleid:{5}{0}shouldpass:{6}{0}passed:{7}{0}addrectanglepassed:{8}{0}exception:{9}", Environment.NewLine, server, login, password, operatorId, rectangleId, shouldPass, passed, addFillRectanglePassed, exception));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\HttpDataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\HttpDataAccessTestData.xml",
                   "GetSourcesListRow",
                    DataAccessMethod.Sequential)]
        public void HttpDataAccessTest_Get_Sources_List()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            int operatorId = int.Parse(testContextInstance.DataRow["OperatorId"].ToString());
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string code = testContextInstance.DataRow["Code"].ToString();
            string exception = string.Empty;
            HttpDataClient test = new HttpDataClient();

            bool passed = false;
            try
            {
                test.Login(server, login, password);
                string map, pdf;

                test.GetResourcesList(
                    operatorId,
                    new System.Collections.Generic.List<ExelConverter.Core.DataWriter.ReExportData>(
                        new ExelConverter.Core.DataWriter.ReExportData[] { new ExelConverter.Core.DataWriter.ReExportData(code) }
                        )
                    , out map
                    , out pdf);

                Assert.IsNotNull(map, "map should not be empty");
                Assert.IsNotNull(pdf, "pdf should not be empty");

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            Assert.AreEqual(passed, shouldPass, string.Format("server:{1}{0}login:{2}{0}password:{3}{0}operatorid:{4}{0}code:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, operatorId, code, shouldPass, passed, exception));
        }

        [TestMethod]
        [DeploymentItem(@"ExcelConverter.Core.Test\HttpDataAccessTestData.xml")]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.XML",
                   "|DataDirectory|\\HttpDataAccessTestData.xml",
                   "UploadFileToQueueRow",
                    DataAccessMethod.Sequential)]
        public void HttpDataAccessTest_Upload_File_To_Queue()
        {
            string server = testContextInstance.DataRow["Server"].ToString();
            string login = testContextInstance.DataRow["Login"].ToString();
            string password = testContextInstance.DataRow["Password"].ToString();
            string filePath = testContextInstance.DataRow["FilePath"].ToString();
            int operatorId = int.Parse(testContextInstance.DataRow["OperatorId"].ToString());
            bool shouldPass = bool.Parse(testContextInstance.DataRow["ShouldPass"].ToString());
            string exception = string.Empty;
            HttpDataClient test = new HttpDataClient();

            bool passed = false;
            try
            {
                test.Login(server, login, password);
                test.WebLogin();
                try
                { 
                    test.UploadFileToQueue(new HttpDataAccessQueueParameters()
                    {
                        FilePath = filePath,
                        Activate = false,
                        CoordinatesApproved = false,
                        OperatorID = operatorId, 
                        Timeout = 100,
                        UseQueue = true
                    });
                }
                finally
                {
                    test.WebLogout();
                }

                passed = true;
            }
            catch (Exception ex) { exception = ex.Message; }

            Assert.AreEqual(passed, shouldPass, string.Format("server:{1}{0}login:{2}{0}password:{3}{0}operatorid:{4}{0}filepath:{5}{0}shouldpass:{6}{0}passed:{7}{0}exception:{8}", Environment.NewLine, server, login, password, operatorId, filePath, shouldPass, passed, exception));
        }
    }
}
