namespace Nikse.SubtitleEdit.Logic.VobSub
{
    /// <summary>
    /// http://www.mpucoder.com/DVD/packhdr.html
    /// </summary>
    public class Mpeg2Header
    {
        public const int Length = 14;

        public uint StartCode { get; set; }
     
        public byte PackIndentifier { get; set; }

        // public readonly UInt64 SystemClockReferenceQuotient;
        // public readonly UInt64 SystemClockReferenceRemainder;
      
        public ulong ProgramMuxRate { get; set; }
       
        public int PackStuffingLength { get; set; }

        public Mpeg2Header(byte[] buffer)
        {
            this.StartCode = Helper.GetEndian(buffer, 0, 3);
            this.PackIndentifier = buffer[3];

            // string b4To9AsBinary = Helper.GetBinaryString(buffer, 4, 6);
            // b4To9AsBinary = b4To9AsBinary.Substring(2,3) + b4To9AsBinary.Substring(6,15) + b4To9AsBinary.Substring(22,15);
            // SystemClockReferenceQuotient = Helper.GetUInt32FromBinaryString(b4To9AsBinary);

            // SystemClockReferenceRemainder = (ulong)(((buffer[8] & Helper.B00000011) << 8) + buffer[9])
            this.ProgramMuxRate = Helper.GetEndian(buffer, 10, 3) >> 2;

            this.PackStuffingLength = buffer[13] & Helper.B00000111;
        }
    }
}
