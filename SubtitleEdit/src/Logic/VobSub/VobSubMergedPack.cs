namespace Nikse.SubtitleEdit.Logic.VobSub
{
    using System;

    public class VobSubMergedPack
    {
        public VobSubMergedPack(byte[] subPictureData, TimeSpan presentationTimestamp, int streamId, IdxParagraph idxLine)
        {
            this.SubPicture = new SubPicture(subPictureData);
            this.StartTime = presentationTimestamp;
            this.StreamId = streamId;
            this.IdxLine = idxLine;
        }

        public SubPicture SubPicture { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; set; }

        public int StreamId { get; private set; }

        public IdxParagraph IdxLine { get; private set; }
    }
}
