namespace Nikse.SubtitleEdit.Logic.ContainerFormats.Mp4.Boxes
{
    using System.IO;

    public class Mvhd : Box
    {
        public uint CreationTime { get; set; }
       
        public uint ModificationTime { get; set; }
        
        public uint Duration { get; set; }
        
        public uint TimeScale { get; set; }

        public Mvhd(FileStream fs)
        {
            Buffer = new byte[20];
            int bytesRead = fs.Read(Buffer, 0, Buffer.Length);
            if (bytesRead < Buffer.Length)
            {
                return;
            }

            CreationTime = GetUInt(4);
            ModificationTime = GetUInt(8);
            TimeScale = GetUInt(12);
            Duration = GetUInt(16);
        }
    }
}
