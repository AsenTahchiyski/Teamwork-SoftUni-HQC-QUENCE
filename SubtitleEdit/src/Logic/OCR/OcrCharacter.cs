namespace Nikse.SubtitleEdit.Logic.Ocr
{
    using System.Collections.Generic;

    public class OcrCharacter
    {
        public string Text { get; private set; }
     
        public List<OcrImage> OcrImages { get; set; }

        public OcrCharacter(string text)
        {
            Text = text;
            OcrImages = new List<OcrImage>();
        }
    }
}