namespace Nikse.SubtitleEdit.Logic.VobSub
{
    using System;

    public class IdxParagraph
    {
        public IdxParagraph(TimeSpan startTime, long filePosition)
        {
            this.StartTime = startTime;
            this.FilePosition = filePosition;
        }

        public TimeSpan StartTime { get; private set; }

        public long FilePosition { get; private set; }
    }
}
