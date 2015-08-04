using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExcelConverter.Core.Test
{
    [TestClass]
    public class ExcelSheetTest
    {
        [TestMethod]
        public void ExcelSheet_TagsTest()
        {
            var res0 = ExelConverter.Core.Tag.FromString("=1");
            Assert.AreEqual("1", res0.Value);
            Assert.AreEqual(false, res0.IsStrong);
            Assert.AreEqual(ExelConverter.Core.TagDirection.Include, res0.Direction);

            var res1 = ExelConverter.Core.Tag.FromString("-=1");
            Assert.AreEqual("1", res1.Value);
            Assert.AreEqual(false, res1.IsStrong);
            Assert.AreEqual(ExelConverter.Core.TagDirection.Exclude, res1.Direction);

            var res2 = ExelConverter.Core.Tag.FromString("-!=1");
            Assert.AreEqual("1", res2.Value);
            Assert.AreEqual(true, res2.IsStrong);
            Assert.AreEqual(ExelConverter.Core.TagDirection.Exclude, res2.Direction);

            var res3 = ExelConverter.Core.Tag.FromString("!1");
            Assert.AreEqual("*1*", res3.Value);
            Assert.AreEqual(true, res3.IsStrong);
            Assert.AreEqual(ExelConverter.Core.TagDirection.Include, res3.Direction);
        }
    }
}
