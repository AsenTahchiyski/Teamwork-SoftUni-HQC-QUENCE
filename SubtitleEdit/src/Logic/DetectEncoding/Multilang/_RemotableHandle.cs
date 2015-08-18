namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct RemotableHandle
    {
        public int fContext;
        public MidlIWinTypes0009 u;
    }
}
