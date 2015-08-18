namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TagRfc1766Info
    {
        public uint lcid;
    
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public ushort[] wszRfc1766;
     
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public ushort[] wszLocaleName;
    }
}
