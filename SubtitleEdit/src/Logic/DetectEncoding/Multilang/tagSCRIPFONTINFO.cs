namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct TagScripfontinfo
    {
        public long scripts;
      
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public ushort[] wszFont;
    }
}
