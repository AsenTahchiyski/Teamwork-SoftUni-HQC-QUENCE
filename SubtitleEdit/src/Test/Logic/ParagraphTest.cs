namespace Test.Logic
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Nikse.SubtitleEdit.Logic;

    [TestClass]
    public class ParagraphTest
    {
        [TestMethod]
        public void TestMethodNumberOfLinesTwoLines()
        {
            var paragraph = new Paragraph { Text = "Hallo!" + Environment.NewLine + "How are you?" };
            Assert.AreEqual(2, paragraph.NumberOfLines);
        }

        [TestMethod]
        public void TestMethodNumberOfLinesThreeLines()
        {
            var paragraph = new Paragraph { Text = "Hallo!" + Environment.NewLine + "How are you?" + Environment.NewLine + "That's ok." };
            Assert.AreEqual(3, paragraph.NumberOfLines);            
        }

        [TestMethod]
        public void TestMethodNumberOfLinesOneLine()
        {
            var paragraph = new Paragraph { Text = "Hallo!" };
            Assert.AreEqual(1, paragraph.NumberOfLines);
        }

        [TestMethod]
        public void TestMethodNumberOfLinesZero()
        {
            var paragraph = new Paragraph { Text = string.Empty };
            Assert.AreEqual(0, paragraph.NumberOfLines);
        }

        [TestMethod]
        public void TestToStringNewParagraph()
        {
            string expectedOutput = "00:00:00,000 --> 00:00:00,000 ";
            var paragraph = new Paragraph();
            string actualOutput = paragraph.ToString();
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [TestMethod]
        public void TestMethodAdjustOneSecond()
        {
            var paragraph = new Paragraph();
            paragraph.Adjust(0, 1);
            int startSeconds = paragraph.StartTime.Seconds;
            int endSeconds = paragraph.EndTime.Seconds;
            Assert.AreEqual(1, startSeconds);
            Assert.AreEqual(1, endSeconds);
        }

    }

}
