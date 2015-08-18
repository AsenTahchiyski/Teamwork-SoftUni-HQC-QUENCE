namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Filetime
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }
}
