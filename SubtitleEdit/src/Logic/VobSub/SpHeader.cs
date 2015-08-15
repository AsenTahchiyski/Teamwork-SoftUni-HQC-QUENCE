namespace Nikse.SubtitleEdit.Logic.VobSub
{
    using System;

    public class SpHeader
    {
        public const int SpHeaderLength = 14;

        public SpHeader(byte[] buffer)
        {
            this.Identifier = System.Text.Encoding.ASCII.GetString(buffer, 0, 2);
            int startMilliseconds = Helper.GetLittleEndian32(buffer, 2) / 90;
            this.StartTime = TimeSpan.FromMilliseconds(startMilliseconds);
            this.NextBlockPosition = Helper.GetEndianWord(buffer, 10) - 4;
            this.ControlSequencePosition = Helper.GetEndianWord(buffer, 12) - 4;
        }

        public string Identifier { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public int NextBlockPosition { get; private set; }

        public int ControlSequencePosition { get; private set; }

        public SubPicture Picture { get; private set; }

        public SubPicture AddPicture(byte[] buffer)
        {
            this.Picture = new SubPicture(buffer, this.ControlSequencePosition, -4);
            return this.Picture;
        }
    }
}
