namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    public enum Mldetectcp
    {
        // Default setting will be used.
        MldetectcpNone = 0,

        // Input stream consists of 7-bit data.
        Mldetectcp7Bit = 1,

        // Input stream consists of 8-bit data.
        Mldetectcp8Bit = 2,

        // Input stream consists of double-byte data.
        MldetectcpDbcs = 4,

        // Input stream is an HTML page.
        MldetectcpHtml = 8,

        //Not currently supported.
        MldetectcpNumber = 16
    }
}
