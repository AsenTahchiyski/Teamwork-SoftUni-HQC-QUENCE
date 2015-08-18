namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TagScriptinfo
    {
        public byte ScriptId;
        public uint uiCodePage;
   
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x30)]
        public ushort[] wszDescription;
    
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public ushort[] wszFixedWidthFont;
      
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public ushort[] wszProportionalFont;
    }
}
