namespace Nikse.SubtitleEdit.Logic
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class TarReader : IDisposable
    {
        private Stream stream;

        public List<TarHeader> Files { get; private set; }

        public TarReader(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            OpenTarFile(fs);
        }

        public TarReader(Stream stream)
        {
            OpenTarFile(stream);
        }

        private void OpenTarFile(Stream stream)
        {
            this.stream = stream;
            Files = new List<TarHeader>();
            long length = stream.Length;
            long pos = 0;
            stream.Position = 0;
            while (pos + 512 < length)
            {
                stream.Seek(pos, SeekOrigin.Begin);
                var tarHeader = new TarHeader(stream);
                if (tarHeader.FileSizeInBytes > 0)
                {
                    Files.Add(tarHeader);
                }

                pos += TarHeader.HeaderSize + tarHeader.FileSizeInBytes;
                if (pos%TarHeader.HeaderSize > 0)
                {
                    pos += 512 - (pos % TarHeader.HeaderSize);
                }
            }
        }

        public void Close()
        {
            stream.Close();
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
}