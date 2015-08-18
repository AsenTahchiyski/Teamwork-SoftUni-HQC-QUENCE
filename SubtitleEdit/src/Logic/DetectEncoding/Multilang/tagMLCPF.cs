namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System;

    [Flags]
    public enum Mlcpf
    {
        // Not currently supported.
        MldetectfMailnews = 0x0001,

        // Not currently supported.
        MldetectfBrowser = 0x0002,

        // Detection result must be valid for conversion and text rendering.
        MldetectfValid = 0x0004,

        // Detection result must be valid for conversion.
        MldetectfValidNls = 0x0008,

        //Preserve preferred code page order.
        //This is meaningful only if you have set the puiPreferredCodePages parameter in IMultiLanguage3::DetectOutboundCodePage or IMultiLanguage3::DetectOutboundCodePageInIStream.
        MldetectfPreserveOrder = 0x0010,

        // Only return one of the preferred code pages as the detection result.
        // This is meaningful only if you have set the puiPreferredCodePages parameter in IMultiLanguage3::DetectOutboundCodePage or IMultiLanguage3::DetectOutboundCodePageInIStream.
        MldetectfPreferredOnly = 0x0020,

        // Filter out graphical symbols and punctuation.
        MldetectfFilterSpecialchar = 0x0040,

        // Return only Unicode codepages if the euro character is detected.
        MldetectfEuroUtf8 = 0x0080
    }
}
