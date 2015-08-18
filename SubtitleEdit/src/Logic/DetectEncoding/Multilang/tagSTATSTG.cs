namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct TagStatstg
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pwcsName;
        public uint type;
        public UlargeInteger cbSize;
        public Filetime mtime;
        public Filetime ctime;
        public Filetime atime;
        public uint grfMode;
        public uint grfLocksSupported;
        public Guid clsid;
        public uint grfStateBits;
        public uint reserved;
    }
}
