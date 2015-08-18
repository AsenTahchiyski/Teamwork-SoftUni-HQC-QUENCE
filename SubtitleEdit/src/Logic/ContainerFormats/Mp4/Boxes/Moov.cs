namespace Nikse.SubtitleEdit.Logic.ContainerFormats.Mp4.Boxes
{
    using System.Collections.Generic;
    using System.IO;

    public class Moov : Box
    {
        public Mvhd Mvhd;
        public List<Trak> Tracks;

        public Moov(FileStream fs, ulong maximumLength)
        {
            Tracks = new List<Trak>();
            Position = (ulong)fs.Position;
            while (fs.Position < (long)maximumLength)
            {
                if (!InitializeSizeAndName(fs))
                {
                    return;
                }

                switch (Name)
                {
                    case "trak":
                        Tracks.Add(new Trak(fs, Position));
                        break;
                    case "mvhd":
                        Mvhd = new Mvhd(fs);
                        break;
                }

                fs.Seek((long)Position, SeekOrigin.Begin);
            }
        }
    }
}
