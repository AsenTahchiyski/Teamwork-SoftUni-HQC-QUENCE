namespace Nikse.SubtitleEdit.Logic.ContainerFormats.Mp4.Boxes
{
    using System.IO;

    public class Trak : Box
    {
        public Mdia Mdia { get; set; }
      
        public Tkhd Tkhd { get; set; }

        public Trak(FileStream fs, ulong maximumLength)
        {
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                {
                    return;
                }

                switch (Name)
                {
                    case "mdia":
                        Mdia = new Mdia(fs, Position);
                        break;
                    case "tkhd":
                        Tkhd = new Tkhd(fs);
                        break;
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }
    }
}
