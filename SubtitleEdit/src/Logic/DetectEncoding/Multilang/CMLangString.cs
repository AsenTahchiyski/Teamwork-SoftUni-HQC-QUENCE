namespace Nikse.SubtitleEdit.Logic.DetectEncoding.Multilang
{
    using System.Runtime.InteropServices;
    using MultiLanguage;

    [ComImport, CoClass(typeof(CMLangStringClass)), Guid("C04D65CE-B70D-11D0-B188-00AA0038C969")]
    public interface ICMLangString : IMLangString
    {
    }
}
