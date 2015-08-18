namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TagUnicoderange
    {
        public ushort wcFrom;
        public ushort wcTo;
    }
}
