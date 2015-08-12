namespace Test.Core
{
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
        public void TestEncodeNamed_WithEmptyString_ShouldReturnEmptyString()
        {
            string expectedResult = string.Empty;
            string actualResult = HtmlUtil.EncodeNamed("");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithNull_ShouldReturnEmptyString()
        {
            string expectedResult = string.Empty;
            string actualResult = HtmlUtil.EncodeNamed(null);
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

        [TestMethod]
        public void TestEncodeNamed_WithManySymbols()
        {
            string expectedResult = "&lt;Test$&gt;";
            string actualResult = HtmlUtil.EncodeNamed("<Test$>");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithManySymbols2()
        {
            string expectedResult = "&lt;&gt;&quot;&amp;&nbsp;&ndash;&mdash;&iexcl;&iquest;&ldquo;&rdquo;&euml;&Euml;&iacute;";
            string actualResult = HtmlUtil.EncodeNamed("<>\"& –—¡¿“”ëËí");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithManySymbols3()
        {
            string expectedResult = "&lsquo;&rsquo;&laquo;&raquo;&cent;&copy;&divide;&micro;&middot;&para;&plusmn;&ecirc;&Ecirc;";
            string actualResult = HtmlUtil.EncodeNamed("‘’«»¢©÷µ·¶±êÊ");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithManySymbols4()
        {
            string expectedResult = "&euro;&pound;&reg;&sect;&trade;&yen;&aacute;&Aacute;&agrave;&Agrave;&acirc;&egrave;&Egrave;";
            string actualResult = HtmlUtil.EncodeNamed("€£®§™¥áÁàÀâèÈ");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithManySymbols5()
        {
            string expectedResult = "&Acirc;&aring;&Aring;&atilde;&Atilde;&auml;&Auml;&aelig;&AElig;&ccedil;&Ccedil;&eacute;&Eacute;";
            string actualResult = HtmlUtil.EncodeNamed("ÂåÅãÃäÄæÆçÇéÉ");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNamed_WithManySymbols6()
        {
            string expectedResult = "&Iacute;&igrave;&Igrave;&icirc;&Icirc;&iuml;&Iuml;&ntilde;&Ntilde;&oacute;&Oacute;&ograve;&Ograve;";
            string actualResult = HtmlUtil.EncodeNamed("ÍìÌîÎïÏñÑóÓòÒ");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNumeric_WithEmptyString_ShouldReturnEmptyString()
        {
            string expectedResult = string.Empty;
            string actualResult = HtmlUtil.EncodeNumeric(string.Empty);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNumeric_WithNull_ShouldReturnEmptyString()
        {
            string expectedResult = string.Empty;
            string actualResult = HtmlUtil.EncodeNumeric(null);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNumeric_WhiteSpace()
        {
            string expectedResult = "&#160;";
            string actualResult = HtmlUtil.EncodeNumeric(" ");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNumeric_WithSpecialSymbols()
        {
            string expectedResult = "&#60;&#38;&#62;";
            string actualResult = HtmlUtil.EncodeNumeric("<&>");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNumeric_WithEnglishLetters()
        {
            string expectedResult = "abcDEf";
            string actualResult = HtmlUtil.EncodeNumeric("abcDEf");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEncodeNumeric_WithDigits()
        {
            string expectedResult = "1235299";
            string actualResult = HtmlUtil.EncodeNumeric("1235299");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestRemoveHtmlTags_WithShortText()
        {
            string expectedResult = "<2";
            string actualResult = HtmlUtil.RemoveHtmlTags("<2");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestRemoveHtmlTags_WithNull()
        {
            string expectedResult = null;
            string actualResult = HtmlUtil.RemoveHtmlTags(null);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestRemoveHtmlTags_WithoutLessThanSymbol()
        {
            string expectedResult = "SubtitleEdit";
            string actualResult = HtmlUtil.RemoveHtmlTags("SubtitleEdit");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestRemoveHtmlTags_WithHtmlText()
        {
            string expectedResult = "Hello World";
            string actualResult = HtmlUtil.RemoveHtmlTags("<p>Hello <i>World</i></p>");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestIsUrl_WithTextThatContainsWhiteSpace()
        {
            string testInput = "Hello World.";
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsUrl_WithShortText()
        {
            string testInput = "he.";
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsUrl_WithTextThatDontContainDot()
        {
            string testInput = "SubtitleEdit";
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsUrl_WithEmptyString()
        {
            string testInput = string.Empty;
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsUrl_WithCorrect_httpsUrl()
        {
            string testInput = "https://softuni.bg";
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsUrl_WithCorrect_wwwUrl()
        {
            string testInput = "www.softuni.bg";
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsUrl_WithCorrect_comUrl()
        {
            string testInput = "softuni.com";
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsUrl_WithCorrect_comUrl2()
        {
            string testInput = "softuni.com/";
            bool result = HtmlUtil.IsUrl(testInput);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestStartsWithUrl_WithEmptyString()
        {
            string testInput = string.Empty;
            bool result = HtmlUtil.StartsWithUrl(testInput);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestStartsWithUrl_WithCorrectUrl()
        {
            string testInput = "www.test.com .";
            bool result = HtmlUtil.StartsWithUrl(testInput);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestStartsWithUrl_WithCorrectUrl2()
        {
            string testInput = "www.test.com Test example .";
            bool result = HtmlUtil.StartsWithUrl(testInput);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestFixUpperTags_WithNull()
        {
            string expectedResult = null;
            string actualResult = HtmlUtil.FixUpperTags(null);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestFixUpperTags_WithItalicTag()
        {
            string expectedResult = "<i>";
            string actualResult = HtmlUtil.FixUpperTags("<I>");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestFixUpperTags_WithParagraphTag()
        {
            string expectedResult = "</font>";
            string actualResult = HtmlUtil.FixUpperTags("</FONT>");
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestFixUpperTags_WithManyTags()
        {
            string expectedResult = "<b> Test <u> Unit </u> Test </b>";
            string actualResult = HtmlUtil.FixUpperTags("<B> Test <U> Unit </u> Test </B>");
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
