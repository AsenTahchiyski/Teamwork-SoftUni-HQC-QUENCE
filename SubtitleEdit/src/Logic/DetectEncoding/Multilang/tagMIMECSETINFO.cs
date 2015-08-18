namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TagMimecsetinfo
    {
        public uint uiCodePage;
        public uint uiInternetEncoding;
       
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public ushort[] wszCharset;
    }
}
