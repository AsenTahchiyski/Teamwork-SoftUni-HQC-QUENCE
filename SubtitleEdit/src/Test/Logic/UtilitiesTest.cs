namespace Test.Logic
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Nikse.SubtitleEdit.Core;
    using Nikse.SubtitleEdit.Logic;
    using Nikse.SubtitleEdit.Logic.Forms;

    [TestClass]
    public class UtilitiesTest
    {
        [TestMethod]
        public void TestGetRegExGroupCorrecktInput()
        {
            string expectedResult = "test";
            string correctInput = "[A-Za-z](?<test>)[a-z]";
            string actualResult = Utilities.GetRegExGroup(correctInput);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestGetRegExGroupIncorrecktInput()
        {
            string incorrectInput = "test";
            string number = Utilities.GetRegExGroup(incorrectInput);
            Assert.IsNull(number);
        }

        [TestMethod]
        public void TestGetRegExGroupIncorrecktInput2()
        {
            string incorrectInput = "?<test";
            string number = Utilities.GetRegExGroup(incorrectInput);
            Assert.IsNull(number);
        }

        [TestMethod]
        public void TestGetNumber0To7FromUserName()
        {
            int number = Utilities.GetNumber0To7FromUserName("abc");
            Assert.AreEqual(6, number);
        }

        [TestMethod]
        public void TestGetNumber0To7FromUserNameEmptyString()
        {
            int number = Utilities.GetNumber0To7FromUserName(string.Empty);
            Assert.AreEqual(0, number);
        }

        [TestMethod]
        public void TestGetColorFromUserName()
        {
            var color = Utilities.GetColorFromUserName("t");
            var a = color.A;
            var b = color.B;
            Assert.AreEqual(255, a);
            Assert.AreEqual(0, b);
        }

        [TestMethod]
        public void TestGetColorFromUserNameEmptyString()
        {
            var color = Utilities.GetColorFromUserName(string.Empty);
            var a = color.A;
            var b = color.B;
            Assert.AreEqual(255, a);
            Assert.AreEqual(203, b);
        }

        [TestMethod]
        public void TestLoadUserWordList()
        {
            string result = Utilities.LoadUserWordList(new List<string>(), "names_etc");
            string[] resultArgs = result.Split('.');
            Assert.AreEqual("xml", resultArgs[1]);
        }

        [TestMethod]
        public void TestMakeWordSearchRegexWithNumbers()
        {
            string testInput = "?";
            var regex = Utilities.MakeWordSearchRegexWithNumbers(testInput);
            Assert.AreEqual(@"[\b ,\.\?\!]\?[\b !\.,\r\n\?]", regex.ToString());
        }

        [TestMethod]
        public void TestMakeWordSearchRegex()
        {
            string testInput = "?";
            var regex = Utilities.MakeWordSearchRegex(testInput);
            Assert.AreEqual(@"\b\?\b", regex.ToString());
        }

        [TestMethod]
        public void TestIsValidRegexWithIncorrectPattern()
        {
            string pattern = "\\";
            bool isValidRegex = Utilities.IsValidRegex(pattern);
            Assert.IsFalse(isValidRegex);
        }

        [TestMethod]
        public void TestIsValidRegexWithCorrectPattern()
        {
            string pattern = "\\s+";
            bool isValidRegex = Utilities.IsValidRegex(pattern);
            Assert.IsTrue(isValidRegex);
        }

        [TestMethod]
        public void TestIsValidRegexWithEmptyString()
        {
            bool isValidRegex = Utilities.IsValidRegex(string.Empty);
            Assert.IsFalse(isValidRegex);
        }

        [TestMethod]
        public void TestIsQuartsDllInstalled()
        {
            var isQuartsDllInstalled = Utilities.IsQuartsDllInstalled;
            Assert.IsTrue(isQuartsDllInstalled);
        }

        [TestMethod]
        public void TestGetOpenDialogFilter()
        {
            var result = Utilities.GetOpenDialogFilter();
            string[] resultParams = result.Split('|');
            Assert.AreEqual("Subtitle files", resultParams[0]);
            Assert.AreEqual("All files", resultParams[2]);
        }

        [TestMethod]
        public void TestFormatBytesToDisplayFileSize_WithGigaBytes()
        {
            long fileSize = 1073741825;
            string expectedRsult = string.Format("{0:0.0} gb", (float)fileSize / (1024 * 1024 * 1024));

            string actualResult = Utilities.FormatBytesToDisplayFileSize(fileSize);
            Assert.AreEqual(expectedRsult, actualResult);
        }

        [TestMethod]
        public void TestFormatBytesToDisplayFileSize_WithKiloBytes()
        {
            string expectedRsult = "1024 kb";

            string actualResult = Utilities.FormatBytesToDisplayFileSize(1024 * 1024);
            Assert.AreEqual(expectedRsult, actualResult);
        }

        [TestMethod]
        public void TestFormatBytesToDisplayFileSize_WithBytes()
        {
            string expectedRsult = "1024 bytes";

            string actualResult = Utilities.FormatBytesToDisplayFileSize(1024);
            Assert.AreEqual(expectedRsult, actualResult);
        }

        [TestMethod]
        public void TestGetSubtitleFormatByFriendlyName()
        {
            string formatName = "Adobe After Effects ft-MarkerExporter";
            string formatExtension = ".xml";
            var format = Utilities.GetSubtitleFormatByFriendlyName(formatName);
            Assert.AreEqual(formatName, format.Name);
            Assert.AreEqual(formatExtension, format.Extension);
        }

        [TestMethod]
        public void TestGetSubtitleFormatByFriendlyName_WithIncorrectName()
        {
            string formatName = "a";
            var format = Utilities.GetSubtitleFormatByFriendlyName(formatName);
            Assert.IsNull(format);
        }

        [TestMethod]
        public void TestGetVideoFileFilter()
        {
            string result = Utilities.GetVideoFileFilter(true);
            string[] resultParams = result.Split('|');
            Assert.AreEqual("Video files", resultParams[0]);
            Assert.AreEqual("Audio files", resultParams[2]);
            Assert.AreEqual("All files", resultParams[4]);
            Assert.AreEqual("*.*", resultParams[5]);
        }

        [TestMethod]
        public void TestGetMovieFileExtensions()
        {
            var fileExtensions = Utilities.GetMovieFileExtensions();
            bool containsAvi = fileExtensions.Contains(".avi");
            bool containsMpg = fileExtensions.Contains(".mpg");
            Assert.IsTrue(containsAvi);
            Assert.IsTrue(containsMpg);
        }

        [TestMethod]
        public void AutoBreakLine1()
        {
            const int MaxLength = 43;
            var s = Utilities.AutoBreakLine("You have a private health insurance and life insurance." + Environment.NewLine + "A digital clone included.", 5, 33, string.Empty);
            var arr = s.Replace(Environment.NewLine, "\n").Split('\n');
            Assert.AreEqual(2, arr.Length);
            Assert.IsFalse(arr[0].Length > MaxLength);
            Assert.IsFalse(arr[1].Length > MaxLength);
        }

        [TestMethod]
        public void AutoBreakLine2()
        {
            // TODO: Implement me
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void AutoBreakLine3()
        {
            string s1 = "- We're gonna lose him." + Environment.NewLine + "- He's left him four signals in the last week.";
            string s2 = Utilities.AutoBreakLine(s1);
            Assert.AreEqual(s1, s2);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void AutoBreakLine4()
        {
            Configuration.Settings.General.SubtitleLineMaximumLength = 43;
            const string TestInput = "- Seriously, though. Are you being bullied? - Nope.";
            string s2 = Utilities.AutoBreakLine(TestInput);
            string target = "- Seriously, though. Are you being bullied?" + Environment.NewLine + "- Nope.";
            Assert.AreEqual(target, s2);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void AutoBreakLine5DoNoBreakAtPeriod()
        {
            Configuration.Settings.General.SubtitleLineMaximumLength = 43;
            const string TestInput = "Oh, snap, we're still saying the same thing. This is amazing!";
            string s2 = Utilities.AutoBreakLine(TestInput);
            string target = "Oh, snap, we're still saying the" + Environment.NewLine + "same thing. This is amazing!";
            Assert.AreEqual(target, s2);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void AutoBreakLineDoNotBreakAfterDashDash()
        {
            Configuration.Settings.General.SubtitleLineMaximumLength = 43;
            string s1 = "- That's hilarious, I don't--" + Environment.NewLine + "- Are the cheeks turning nice and pink?";
            string s2 = Utilities.AutoBreakLine(s1);
            Assert.AreEqual(s1, s2);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void AutoBreakLineDialog1()
        {
            const string TestInput = "- Qu'est ce qui se passe ? - Je veux voir ce qu'ils veulent être.";
            string s2 = Utilities.AutoBreakLine(TestInput);
            Assert.AreEqual("- Qu'est ce qui se passe ?" + Environment.NewLine + "- Je veux voir ce qu'ils veulent être.", s2);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void AutoBreakLineDialog2()
        {
            const string TestInput = "- Je veux voir ce qu'ils veulent être. - Qu'est ce qui se passe ?";
            string s2 = Utilities.AutoBreakLine(TestInput);
            Assert.AreEqual("- Je veux voir ce qu'ils veulent être." + Environment.NewLine + "- Qu'est ce qui se passe ?", s2);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags2()
        {
            const string TestInput = "Gledaj prema kameri i rici <i>zdravo!";
            string s2 = HtmlUtil.FixInvalidItalicTags(TestInput);
            Assert.AreEqual(s2, "Gledaj prema kameri i rici zdravo!");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags3()
        {
            // TODO: Implement me
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags4()
        {
            string s1 = "It <i>is</i> a telegram," + Environment.NewLine + "it <i>is</i> ordering an advance,";
            string s2 = HtmlUtil.FixInvalidItalicTags(s1);
            Assert.AreEqual(s2, s1);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags5()
        {
            string s1 = "- <i>It is a telegram?</i>" + Environment.NewLine + "<i>- It is.</i>";
            string s2 = HtmlUtil.FixInvalidItalicTags(s1);
            Assert.AreEqual(s2, "<i>- It is a telegram?" + Environment.NewLine + "- It is.</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags6()
        {
            string s1 = "- <i>Text1!</i>" + Environment.NewLine + "- <i>Text2.</i>";
            string s2 = HtmlUtil.FixInvalidItalicTags(s1);
            Assert.AreEqual(s2, "<i>- Text1!" + Environment.NewLine + "- Text2.</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags7()
        {
            string s1 = "<i>- You think they're they gone?<i>" + Environment.NewLine + "<i>- That can't be.</i>";
            string s2 = HtmlUtil.FixInvalidItalicTags(s1);
            Assert.AreEqual(s2, "<i>- You think they're they gone?" + Environment.NewLine + "- That can't be.</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags8()
        {
            string s1 = "<i>- You think they're they gone?</i>" + Environment.NewLine + "<i>- That can't be.<i>";
            string s2 = HtmlUtil.FixInvalidItalicTags(s1);
            Assert.AreEqual(s2, "<i>- You think they're they gone?" + Environment.NewLine + "- That can't be.</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixInvalidItalicTags9()
        {
            const string TestInput = "FALCONE:<i> I didn't think</i>\r\n<i>it was going to be you,</i>";
            string s2 = HtmlUtil.FixInvalidItalicTags(TestInput);
            Assert.AreEqual(s2, "FALCONE: <i>I didn't think\r\nit was going to be you,</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesDoubleSpace1()
        {
            const string TestInput = "This is  a test";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, "This is a test");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesDoubleSpace2()
        {
            const string TestInput = "This is a test  ";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, "This is a test");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesItalics1()
        {
            const string TestInput = "<i> This is a test</i>";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, "<i>This is a test</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesItalics2()
        {
            const string TestInput = "<i>This is a test </i>";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, "<i>This is a test</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesHyphen1()
        {
            const string TestInput = "This is a low- budget job";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, "This is a low-budget job");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesHyphen2()
        {
            const string TestInput = "This is a low- budget job";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, "This is a low-budget job");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesHyphenDoNotChange1()
        {
            const string TestInput = "This is it - and he likes it!";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, TestInput);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesHyphenDoNotChange2()
        {
            const string TestInput = "What are your long- and altitude stats?";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, TestInput);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesHyphenDoNotChange3()
        {
            const string TestInput = "Did you buy that first- or second-handed?";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "en");
            Assert.AreEqual(s2, TestInput);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesHyphenDoNotChangeDutch1()
        {
            const string TestInput = "Wat zijn je voor- en familienaam?";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "nl");
            Assert.AreEqual(s2, TestInput);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesHyphenDoNotChangeDutch2()
        {
            const string TestInput = "Was het in het voor- of najaar?";
            string s2 = Utilities.RemoveUnneededSpaces(TestInput, "nl");
            Assert.AreEqual(s2, TestInput);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesDialogDotDotDotLine1()
        {
            string s = Utilities.RemoveUnneededSpaces("- ... Careful", "en");
            Assert.AreEqual(s, "- ...Careful");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesDialogDotDotDotLine2()
        {
            string s = Utilities.RemoveUnneededSpaces("- Hi!" + Environment.NewLine + "- ... Careful", "en");
            Assert.AreEqual(s, "- Hi!" + Environment.NewLine + "- ...Careful");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesFontTag1()
        {
            string s = Utilities.RemoveUnneededSpaces("<font color=\"#808080\"> (PEOPLE SPEAKING INDISTINCTLY) </font>", "en");
            Assert.AreEqual(s, "<font color=\"#808080\">(PEOPLE SPEAKING INDISTINCTLY)</font>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesFontTag2()
        {
            string s = Utilities.RemoveUnneededSpaces("Foobar\r\n<font color=\"#808080\"> (PEOPLE SPEAKING INDISTINCTLY) </font>", "en");
            Assert.AreEqual(s, "Foobar\r\n<font color=\"#808080\">(PEOPLE SPEAKING INDISTINCTLY)</font>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixUnneededSpacesFontTag3()
        {
            string s = Utilities.RemoveUnneededSpaces("<FONT COLOR=\"#808080\">- Foobar! </FONT>\r\n<font color=\"#808080\"> (PEOPLE SPEAKING INDISTINCTLY) </font>", "en");
            Assert.AreEqual(s, "<font color=\"#808080\">- Foobar!</font>\r\n<font color=\"#808080\">(PEOPLE SPEAKING INDISTINCTLY)</font>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void CountTagInTextStringOneLetterString()
        {
            int count = Utilities.CountTagInText("HHH", "H");
            Assert.AreEqual(count, 3);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void CountTagInTextStringNotThere()
        {
            int count = Utilities.CountTagInText("HHH", "B");
            Assert.AreEqual(count, 0);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void CountTagInTextCharNormal()
        {
            int count = Utilities.CountTagInText("HHH", 'H');
            Assert.AreEqual(count, 3);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void CountTagInTextCharNotThere()
        {
            int count = Utilities.CountTagInText("HHH", 'B');
            Assert.AreEqual(count, 0);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void FixHyphensAddTest()
        {
            string test1 = "<font color=\"#008080\">- Foobar." + Environment.NewLine + "Foobar.</font>";
            string expected1 = "<font color=\"#008080\">- Foobar." + Environment.NewLine + "- Foobar.</font>";

            string test2 = "<i>Foobar.</i>" + Environment.NewLine + "- Foobar.";
            var expected2 = "<i>- Foobar.</i>" + Environment.NewLine + "- Foobar.";

            var sub = new Subtitle();
            sub.Paragraphs.Add(new Paragraph(test1, 0000, 11111));
            sub.Paragraphs.Add(new Paragraph(test2, 0000, 11111));

            string output1 = FixCommonErrorsHelper.FixHyphensAdd(sub, 0, "en");
            string output2 = FixCommonErrorsHelper.FixHyphensAdd(sub, 1, "en");

            Assert.AreEqual(output1, expected1);
            Assert.AreEqual(output2, expected2);
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void RemoveLineBreaks1()
        {
            string result = Utilities.RemoveLineBreaks("Hey" + Environment.NewLine + "you!");
            Assert.AreEqual(result, "Hey you!");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void RemoveLineBreaks2()
        {
            string result = Utilities.RemoveLineBreaks("<i>Foobar " + Environment.NewLine + "</i> foobar.");
            Assert.AreEqual(result, "<i>Foobar</i> foobar.");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void RemoveLineBreaks3()
        {
            string result = Utilities.RemoveLineBreaks("<i>Foobar " + Environment.NewLine + "</i>foobar.");
            Assert.AreEqual(result, "<i>Foobar</i> foobar.");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void RemoveLineBreaks4()
        {
            string result = Utilities.RemoveLineBreaks("<i>Hey</i>" + Environment.NewLine + "<i>you!</i>");
            Assert.AreEqual(result, "<i>Hey you!</i>");
        }

        [TestMethod]
        [DeploymentItem("SubtitleEdit.exe")]
        public void RemoveLineBreaks5()
        {
            string result = Utilities.RemoveLineBreaks("<i>Foobar" + Environment.NewLine + "</i>");
            Assert.AreEqual(result, "<i>Foobar</i>");
        }
    }
}
