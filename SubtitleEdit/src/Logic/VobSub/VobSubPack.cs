namespace Nikse.SubtitleEdit.Logic.VobSub
{
    public class VobSubPack
    {
        public PacketizedElementaryStream PacketizedElementaryStream;

        public Mpeg2Header Mpeg2Header;
        public IdxParagraph IdxLine { get; private set; }

        private readonly byte[] buffer;

        public byte[] Buffer
        {
            get
            {
                return this.buffer;
            }
        }

        public VobSubPack(byte[] buffer, IdxParagraph idxLine)
        {
            this.buffer = buffer;
            this.IdxLine = idxLine;

            if (VobSubParser.IsMpeg2PackHeader(buffer))
            {
                this.Mpeg2Header = new Mpeg2Header(buffer);
                this.PacketizedElementaryStream = new PacketizedElementaryStream(buffer, Mpeg2Header.Length);
            }
            else if (VobSubParser.IsPrivateStream1(buffer, 0))
            {
                this.PacketizedElementaryStream = new PacketizedElementaryStream(buffer, 0);
            }
        }
    }
}
