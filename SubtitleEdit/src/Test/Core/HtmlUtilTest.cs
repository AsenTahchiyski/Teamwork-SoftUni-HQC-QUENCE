namespace Test.Core
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Nikse.SubtitleEdit.Core;

    [TestClass]
    public class HtmlUtilTest
    {
        [TestMethod]
        public void TestRemoveOpenCloseTagCyrillicI()
        {
            const string Source = "<\u0456>SubtitleEdit</\u0456>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(Source, HtmlUtil.TagCyrillicI));
        }

        [TestMethod]
        public void TestRemoveOpenCloseTagFont()
        {
            const string Source = "<font>SubtitleEdit</font>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(Source, HtmlUtil.TagFont));
        }

        [TestMethod]
        public void TestRemoveOpenCloseTagUnderline()
        {
            const string Source = "<u>SubtitleEdit</u>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(Source, HtmlUtil.TagUnderline));
        }

        [TestMethod]
        public void TestRemoveOpenCloseTagBold()
        {
            const string Source = "<b>SubtitleEdit</b>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(Source, HtmlUtil.TagBold));
        }

        [TestMethod]
        public void TestRemoveOpenCloseTagParagraph()
        {
            const string Source = "<p>SubtitleEdit</p>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(Source, HtmlUtil.TagParagraph));
        }

        [TestMethod]
        public void TestRemoveOpenCloseTagItalic()
        {
            const string Source = "<i>SubtitleEdit</i>";
            Assert.AreEqual("SubtitleEdit", HtmlUtil.RemoveOpenCloseTags(Source, HtmlUtil.TagItalic));
        }

        [TestMethod]
        public void TestEncodeNamed_WithEmptyString_ShouldReturnEmpttyString()
        {
            string expectedResult = string.Empty;
            string actualResult = HtmlUtil.EncodeNamed("");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithLetterA_ShouldReturnLetterA()
        {
            string expectedResult = "A";
            string actualResult = HtmlUtil.EncodeNamed("A");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithSymbolAmpersand_ShouldReturnEncodedSymbolAmpersand()
        {
            string expectedResult = "&amp;";
            string actualResult = HtmlUtil.EncodeNamed("&");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithSymbolInExtendedASCIITable()
        {
            string expectedResult = string.Format("&#" + 178 + ";");
            string actualResult = HtmlUtil.EncodeNamed(((char)178).ToString());
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
