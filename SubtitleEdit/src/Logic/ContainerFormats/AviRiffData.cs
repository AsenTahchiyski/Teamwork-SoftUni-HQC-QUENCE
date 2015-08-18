// (c) Giora Tamir (giora@gtamir.com), 2005
namespace Nikse.SubtitleEdit.Logic.ContainerFormats
{
    using System.Runtime.InteropServices;

    internal static class AviRiffData
    {
        #region AVI constants

        // AVIMAINHEADER flags
        public const int AvifHasindex = 0x00000010; // Index at end of file?
        public const int AvifMustuseindex = 0x00000020;
        public const int AvifIsinterleaved = 0x00000100;
        public const int AvifTrustcktype = 0x00000800; // Use CKType to find key frames
        public const int AvifWascapturefile = 0x00010000;
        public const int AvifCopyrighted = 0x00020000;

        // AVISTREAMINFO flags
        public const int AvisfDisabled = 0x00000001;
        public const int AvisfVideoPalchanges = 0x00010000;

        // AVIOLDINDEXENTRY flags
        public const int AviifList = 0x00000001;
        public const int AviifKeyframe = 0x00000010;
        public const int AviifNoTime = 0x00000100;
        public const int AviifCompressor = 0x0FFF0000;  // unused?

        // TIMECODEDATA flags
        public const int TimecodeSmpteBinaryGroup = 0x07;
        public const int TimecodeSmpteColorFrame = 0x08;

        // AVI stream FourCC codes
        public static readonly int StreamtypeVideo = RiffParser.ToFourCC("vids");
        public static readonly int StreamtypeAudio = RiffParser.ToFourCC("auds");
        //public static readonly int streamtypeMIDI = RiffParser.ToFourCC("mids");
        //public static readonly int streamtypeTEXT = RiffParser.ToFourCC("txts");

        // AVI section FourCC codes
        public static readonly int CkidAviHeaderList = RiffParser.ToFourCC("hdrl");
        public static readonly int CkidMainAviHeader = RiffParser.ToFourCC("avih");
        //public static readonly int ckidODML = RiffParser.ToFourCC("odml");
        //public static readonly int ckidAVIExtHeader = RiffParser.ToFourCC("dmlh");
        public static readonly int CkidAviStreamList = RiffParser.ToFourCC("strl");
        public static readonly int CkidAviStreamHeader = RiffParser.ToFourCC("strh");
        //public static readonly int ckidStreamFormat = RiffParser.ToFourCC("strf");
        //public static readonly int ckidAVIOldIndex = RiffParser.ToFourCC("idx1");
        public static readonly int CkidInfoList = RiffParser.ToFourCC("INFO");
        public static readonly int CkidAviisft = RiffParser.ToFourCC("ISFT");
        public const int CkidMp3 = 0x0055;
        public static readonly int CkidWaveFmt = RiffParser.ToFourCC("fmt ");

        #endregion AVI constants
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Avimainheader
    {    // 'avih'
        public int dwMicroSecPerFrame;
        public int dwMaxBytesPerSec;
        public int dwPaddingGranularity;
        public int dwFlags;
        public int dwTotalFrames;
        public int dwInitialFrames;
        public int dwStreams;
        public int dwSuggestedBufferSize;
        public int dwWidth;
        public int dwHeight;
        public int dwReserved0;
        public int dwReserved1;
        public int dwReserved2;
        public int dwReserved3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Aviextheader
    {          // 'dmlh'
        public int dwGrandFrames;          // total number of frames in the file
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 244)]
        public int[] dwFuture;             // to be defined later
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Rect
    {
        public short left;
        public short top;
        public short right;
        public short bottom;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Avistreamheader
    { // 'strh'
        public int fccType;      // stream type codes
        public int fccHandler;
        public int dwFlags;
        public short wPriority;
        public short wLanguage;
        public int dwInitialFrames;
        public int dwScale;
        public int dwRate;       // dwRate/dwScale is stream tick rate in ticks/s
        public int dwStart;
        public int dwLength;
        public int dwSuggestedBufferSize;
        public int dwQuality;
        public int dwSampleSize;
        public Rect rcFrame;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Avioldindexentry
    {
        public int dwChunkId;
        public int dwFlags;
        public int dwOffset;    // offset of riff chunk header for the data
        public int dwSize;      // size of the data (excluding riff header size)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Timecode
    {
        public short wFrameRate;
        public short wFrameFract;
        public int cFrames;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Timecodedata
    {
        private readonly Timecode time;
        public int dwSMPTEflags;
        public int dwUser;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Waveformatex
    {
        public short wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;
    }
}
