namespace Nikse.SubtitleEdit.Controls
{
    using System;

    using Nikse.SubtitleEdit.Logic;

    public class ParagraphEventArgs : EventArgs
    {
        public ParagraphEventArgs(Paragraph p)
        {
            this.Paragraph = p;
        }

        public ParagraphEventArgs(double seconds, Paragraph p)
        {
            this.Seconds = seconds;
            this.Paragraph = p;
        }

        public ParagraphEventArgs(double seconds, Paragraph p, Paragraph b)
        {
            this.Seconds = seconds;
            this.Paragraph = p;
            this.BeforeParagraph = b;
        }

        public ParagraphEventArgs(double seconds, Paragraph p, Paragraph b, MouseDownParagraphType mouseDownParagraphType)
        {
            this.Seconds = seconds;
            this.Paragraph = p;
            this.BeforeParagraph = b;
            this.MouseDownParagraphType = mouseDownParagraphType;
        }

        public ParagraphEventArgs(double seconds, Paragraph p, Paragraph b, MouseDownParagraphType mouseDownParagraphType, bool movePreviousOrNext)
        {
            this.Seconds = seconds;
            this.Paragraph = p;
            this.BeforeParagraph = b;
            this.MouseDownParagraphType = mouseDownParagraphType;
            this.MovePreviousOrNext = movePreviousOrNext;
        }

        public Paragraph Paragraph { get; private set; }

        public double Seconds { get; private set; }

        public Paragraph BeforeParagraph { get; set; }

        public MouseDownParagraphType MouseDownParagraphType { get; set; }

        public bool MovePreviousOrNext { get; set; }
    }
}