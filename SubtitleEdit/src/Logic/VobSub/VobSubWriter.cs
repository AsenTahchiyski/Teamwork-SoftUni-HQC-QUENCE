﻿namespace Nikse.SubtitleEdit.Logic.VobSub
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Text;

    public class VobSubWriter : IDisposable
    {

        private class MemWriter
        {
            private readonly byte[] buf;
            private long pos;

            public MemWriter(long size)
            {
                buf = new byte[size];
                pos = 0;
            }

            public byte[] GetBuf()
            {
                return buf;
            }

            public long GetPosition()
            {
                return pos;
            }

            public void GotoBegin()
            {
                pos = 0;
            }

            public void WriteByte(byte val)
            {
                buf[pos++] = val;
            }
        }

        private readonly string subFileName;
        private FileStream subFile;
        private readonly StringBuilder idx;
        private readonly int screenWidth = 720;
        private readonly int screenHeight = 480;
        private readonly int bottomMargin = 15;
        private readonly int leftRightMargin = 15;
        private readonly int languageStreamId;
        private Color background = Color.Transparent;
        private Color pattern = Color.White;
        private Color emphasis1 = Color.Black;
        private readonly bool useInnerAntialiasing = true;
        private Color emphasis2 = Color.FromArgb(240, Color.Black);
        private readonly string languageName = "English";
        private readonly string languageNameShort = "en";

        public VobSubWriter(string subFileName, int screenWidth, int screenHeight, int bottomMargin, int leftRightMargin, int languageStreamId, Color pattern, Color emphasis1, bool useInnerAntialiasing, string languageName, string languageNameShort)
        {
            this.subFileName = subFileName;
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.bottomMargin = bottomMargin;
            this.leftRightMargin = leftRightMargin;
            this.languageStreamId = languageStreamId;
            this.pattern = pattern;
            this.emphasis1 = emphasis1;
            this.useInnerAntialiasing = useInnerAntialiasing;
            this.languageName = languageName;
            this.languageNameShort = languageNameShort;
            idx = CreateIdxHeader();
            subFile = new FileStream(subFileName, FileMode.Create);
        }

        public static void WriteEndianWord(int i, Stream stream)
        {
            stream.WriteByte((byte)(i / 256));
            stream.WriteByte((byte)(i % 256));
        }

        private byte[] GetSubImageBuffer(RunLengthTwoParts twoPartBuffer, NikseBitmap nbmp, Paragraph p, ContentAlignment alignment)
        {
            var ms = new MemoryStream();

            // sup picture datasize
            WriteEndianWord(twoPartBuffer.Length + 34, ms);

            // first display control sequence table address
            int startDisplayControlSequenceTableAddress = twoPartBuffer.Length + 4;
            WriteEndianWord(startDisplayControlSequenceTableAddress, ms);

            // Write image
            const int imageTopFieldDataAddress = 4;
            ms.Write(twoPartBuffer.Buffer1, 0, twoPartBuffer.Buffer1.Length);
            int imageBottomFieldDataAddress = 4 + twoPartBuffer.Buffer1.Length;
            ms.Write(twoPartBuffer.Buffer2, 0, twoPartBuffer.Buffer2.Length);

            // Write zero delay
            ms.WriteByte(0);
            ms.WriteByte(0);

            // next display control sequence table address (use current is last)
            WriteEndianWord(startDisplayControlSequenceTableAddress + 24, ms); // start of display control sequence table address

            // Control command start
            if (p.Forced)
            {
                ms.WriteByte(0); // ForcedStartDisplay==0
            }
            else
            {
                ms.WriteByte(1); // StartDisplay==1
            }

            // Control command 3 = SetColor
            WriteColors(ms); // 3 bytes

            // Control command 4 = SetContrast
            WriteContrast(ms); // 3 bytes

            // Control command 5 = SetDisplayArea
            WriteDisplayArea(ms, nbmp, alignment); // 7 bytes

            // Control command 6 = SetPixelDataAddress
            WritePixelDataAddress(ms, imageTopFieldDataAddress, imageBottomFieldDataAddress); // 5 bytes

            // Control command exit
            ms.WriteByte(255); // 1 byte

            // Control Sequence Table
            // Write delay - subtitle duration
            WriteEndianWord(Convert.ToInt32(p.Duration.TotalMilliseconds * 90.0 - 1023) >> 10, ms);

            // next display control sequence table address (use current is last)
            WriteEndianWord(startDisplayControlSequenceTableAddress + 24, ms); // start of display control sequence table address

            // Control command 2 = StopDisplay
            ms.WriteByte(2);

            // extra byte - for compatability with gpac/MP4BOX
            ms.WriteByte(255); // 1 byte

            return ms.ToArray();
        }

        public void WriteParagraph(Paragraph p, Bitmap bmp, ContentAlignment alignment) // inspired by code from SubtitleCreator
        {
            // timestamp: 00:00:33:900, filepos: 000000000
            idx.AppendLine(string.Format("timestamp: {0:00}:{1:00}:{2:00}:{3:000}, filepos: {4}", p.StartTime.Hours, p.StartTime.Minutes, p.StartTime.Seconds, p.StartTime.Milliseconds, subFile.Position.ToString("X").PadLeft(9, '0').ToLower()));

            var nbmp = new NikseBitmap(bmp);
            emphasis2 = nbmp.ConverToFourColors(background, pattern, emphasis1, useInnerAntialiasing);
            var twoPartBuffer = nbmp.RunLengthEncodeForDvd(background, pattern, emphasis1, emphasis2);
            var imageBuffer = GetSubImageBuffer(twoPartBuffer, nbmp, p, alignment);

            int bufferIndex = 0;
            byte vobSubId = (byte)languageStreamId;
            var mwsub = new MemWriter(200000);
            byte[] subHeader = new byte[30];
            byte[] ts = new byte[4];

            // Lended from "Son2VobSub" by Alain Vielle and Petr Vyskocil
            // And also from Sup2VobSub by Emmel
            subHeader[0] = 0x00; // MPEG 2 PACK HEADER
            subHeader[1] = 0x00;
            subHeader[2] = 0x01;
            subHeader[3] = 0xba;
            subHeader[4] = 0x44;
            subHeader[5] = 0x02;
            subHeader[6] = 0xc4;
            subHeader[7] = 0x82;
            subHeader[8] = 0x04;
            subHeader[9] = 0xa9;
            subHeader[10] = 0x01;
            subHeader[11] = 0x89;
            subHeader[12] = 0xc3;
            subHeader[13] = 0xf8;

            subHeader[14] = 0x00; // PES
            subHeader[15] = 0x00;
            subHeader[16] = 0x01;
            subHeader[17] = 0xbd;

            int packetSize = imageBuffer.Length;
            long toWrite = packetSize;  // Image buffer + control sequence length
            bool header0 = true;

            while (toWrite > 0)
            {
                long headerSize;
                if (header0)
                {
                    header0 = false;

                    // This is only for first packet
                    subHeader[20] = 0x81;   // mark as original
                    subHeader[21] = 0x80;   // first packet: PTS
                    subHeader[22] = 0x05;   // PES header data length

                    // PTS (90kHz):
                    //--------------
                    subHeader[23] = (byte)((ts[3] & 0xc0) >> 5 | 0x21);
                    subHeader[24] = (byte)((ts[3] & 0x3f) << 2 | (ts[2] & 0xc0) >> 6);
                    subHeader[25] = (byte)((ts[2] & 0x3f) << 2 | (ts[1] & 0x80) >> 6 | 0x01);
                    subHeader[26] = (byte)((ts[1] & 0x7f) << 1 | (ts[0] & 0x80) >> 7);
                    subHeader[27] = (byte)((ts[0] & 0x7f) << 1 | 0x01);

                    const string pre = "0010"; // 0011 or 0010 ? (KMPlayer will not understand 0011!!!)
                    long newPts = (long)(p.StartTime.TotalSeconds * 90000.0 + 0.5);
                    string bString = Convert.ToString(newPts, 2).PadLeft(33, '0');
                    string fiveBytesString = pre + bString.Substring(0, 3) + "1" + bString.Substring(3, 15) + "1" + bString.Substring(18, 15) + "1";
                    for (int i = 0; i < 5; i++)
                    {
                        subHeader[23 + i] = Convert.ToByte(fiveBytesString.Substring((i * 8), 8), 2);
                    }

                    subHeader[28] = vobSubId;
                    headerSize = 29;
                }
                else
                {
                    subHeader[20] = 0x81; // mark as original
                    subHeader[21] = 0x00; // no PTS
                    subHeader[22] = 0x00; // header data length
                    subHeader[23] = vobSubId;
                    headerSize = 24;
                }

                if ((toWrite + headerSize) <= 0x800)
                {
                    // write whole image in one 0x800 part

                    long j = (headerSize - 20) + toWrite;
                    subHeader[18] = (byte)(j / 0x100);
                    subHeader[19] = (byte)(j % 0x100);

                    // First Write header
                    for (int x = 0; x < headerSize; x++)
                    {
                        mwsub.WriteByte(subHeader[x]);
                    }

                    // Write Image Data
                    for (int x = 0; x < toWrite; x++)
                    {
                        mwsub.WriteByte(imageBuffer[bufferIndex++]);
                    }

                    // Pad remaining space
                    long paddingSize = 0x800 - headerSize - toWrite;
                    for (int x = 0; x < paddingSize; x++)
                    {
                        mwsub.WriteByte(0xff);
                    }

                    toWrite = 0;
                }
                else
                {
                    // write multiple parts

                    long blockSize = 0x800 - headerSize;
                    long j = (headerSize - 20) + blockSize;
                    subHeader[18] = (byte)(j / 0x100);
                    subHeader[19] = (byte)(j % 0x100);

                    // First Write header
                    for (int x = 0; x < headerSize; x++)
                    {
                        mwsub.WriteByte(subHeader[x]);
                    }

                    // Write Image Data
                    for (int x = 0; x < blockSize; x++)
                    {
                        mwsub.WriteByte(imageBuffer[bufferIndex++]);
                    }

                    toWrite -= blockSize;
                }
            }

            // Write whole memory stream to file
            long endPosition = mwsub.GetPosition();
            mwsub.GotoBegin();
            subFile.Write(mwsub.GetBuf(), 0, (int)endPosition);
        }

        private static void WritePixelDataAddress(Stream stream, int imageTopFieldDataAddress, int imageBottomFieldDataAddress)
        {
            stream.WriteByte(6);
            WriteEndianWord(imageTopFieldDataAddress, stream);
            WriteEndianWord(imageBottomFieldDataAddress, stream);
        }

        private void WriteDisplayArea(Stream stream, NikseBitmap nbmp, ContentAlignment alignment)
        {
            stream.WriteByte(5);

            // Write 6 bytes of area - starting X, ending X, starting Y, ending Y, each 12 bits
            ushort startX = (ushort)((screenWidth - nbmp.Width) / 2);
            ushort startY = (ushort)(screenHeight - nbmp.Height - bottomMargin);

            if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight)
            {
                startY = (ushort)bottomMargin;
            }

            if (alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.MiddleRight)
            {
                startY = (ushort)((screenHeight / 2) - (nbmp.Height / 2));
            }

            if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.BottomLeft)
            {
                startX = (ushort)leftRightMargin;
            }

            if (alignment == ContentAlignment.TopRight || alignment == ContentAlignment.MiddleRight || alignment == ContentAlignment.BottomRight)
            {
                startX = (ushort)(screenWidth - nbmp.Width - leftRightMargin);
            }

            ushort endX = (ushort)(startX + nbmp.Width - 1);
            ushort endY = (ushort)(startY + nbmp.Height - 1);

            WriteEndianWord((ushort)(startX << 4 | endX >> 8), stream); // 16 - 12 start x + 4 end x
            WriteEndianWord((ushort)(endX << 8 | startY >> 4), stream); // 16 - 8 endx + 8 starty
            WriteEndianWord((ushort)(startY << 12 | endY), stream);     // 16 - 4 start y + 12 end y
        }

        /// <summary>
        /// Directly provides the four contrast (alpha blend) values to associate with the four pixel values. One nibble per pixel value for a total of 2 bytes. 0x0 = transparent, 0xF = opaque
        /// </summary>
        private void WriteContrast(Stream stream)
        {
            stream.WriteByte(4);
            stream.WriteByte((byte)((emphasis2.A << 4) | emphasis1.A)); // emphasis2 + emphasis1
            stream.WriteByte((byte)((pattern.A << 4) | background.A)); // pattern + background
        }

        /// <summary>
        /// provides four indices into the CLUT for the current PGC to associate with the four pixel values. One nibble per pixel value for a total of 2 bytes.
        /// </summary>
        private static void WriteColors(Stream stream)
        {
            // Index to palette
            const byte emphasis2 = 3;
            const byte emphasis1 = 2;
            const byte pattern = 1;
            const byte background = 0;

            stream.WriteByte(3);
            stream.WriteByte((emphasis2 << 4) | emphasis1); // emphasis2 + emphasis1
            stream.WriteByte((pattern << 4) | background); // pattern + background
        }

        public void WriteIdxFile()
        {
            string idxFileName = subFileName.Substring(0, subFileName.Length - 3) + "idx";
            File.WriteAllText(idxFileName, idx.ToString().Trim());
        }

        private StringBuilder CreateIdxHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"# VobSub index file, v7 (do not modify this line!)
#
# To repair desynchronization, you can insert gaps this way:
# (it usually happens after vob id changes)
#
#    delay: [sign]hh:mm:ss:ms
#
# Where:
#    [sign]: +, - (optional)
#    hh: hours (0 <= hh)
#    mm/ss: minutes/seconds (0 <= mm/ss <= 59)
#    ms: milliseconds (0 <= ms <= 999)
#
#    Note: You can't position a sub before the previous with a negative value.
#
# You can also modify timestamps or delete a few subs you don't like.
# Just make sure they stay in increasing order.

# Settings

# Original frame size
size: " + screenWidth + "x" + screenHeight + @"

# Origin, relative to the upper-left corner, can be overloaded by aligment
org: 0, 0

# Image scaling (hor,ver), origin is at the upper-left corner or at the alignment coord (x, y)
scale: 100%, 100%

# Alpha blending
alpha: 100%

# Smoothing for very blocky images (use OLD for no filtering)
smooth: OFF

# In milliseconds
fadein/out: 50, 50

# Force subtitle placement relative to (org.x, org.y)
align: OFF at LEFT TOP

# For correcting non-progressive desync. (in milliseconds or hh:mm:ss:ms)
# Note: Not effective in DirectVobSub, use 'delay: ... ' instead.
time offset: 0

# ON: displays only forced subtitles, OFF: shows everything
forced subs: OFF

# The original palette of the DVD
palette: 000000, " + ToHexColor(pattern) + ", " + ToHexColor(emphasis1) + ", " + ToHexColor(emphasis2) + @", 828282, 828282, 828282, ffffff, 828282, bababa, 828282, 828282, 828282, 828282, 828282, 828282

# Custom colors (transp idxs and the four colors)
custom colors: OFF, tridx: 0000, colors: 000000, 000000, 000000, 000000

# Language index in use
langidx: 0

# " + languageName + @"
id: " + languageNameShort + @", index: 0
# Decomment next line to activate alternative name in DirectVobSub / Windows Media Player 6.x
# alt: " + languageName + @"
# Vob/Cell ID: 1, 1 (PTS: 0)");
            return sb;
        }

        private static string ToHexColor(Color c)
        {
            return (c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2")).ToLower();
        }

        private void ReleaseManagedResources()
        {
            if (subFile == null)
            {
                return;
            }

            subFile.Dispose();
            subFile = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseManagedResources();
            }
        }
    }
}