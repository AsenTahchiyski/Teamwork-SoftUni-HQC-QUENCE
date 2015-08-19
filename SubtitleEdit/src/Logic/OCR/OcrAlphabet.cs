namespace Nikse.SubtitleEdit.Logic.Ocr
{
    using System.Collections.Generic;
    using System.Linq;

    public class OcrAlphabet
    {
        public OcrAlphabet()
        {
            OcrCharacters = new List<OcrCharacter>();
        }

        public List<OcrCharacter> OcrCharacters { get; private set; }

        public int CalculateMaximumSize()
        {
            return (from c in OcrCharacters from img in c.OcrImages select img.Bmp.Width*img.Bmp.Height).Concat(new[] { 0 }).Max();
        }

        public OcrCharacter GetOcrCharacter(string text, bool addIfNotExists)
        {
            foreach (var ocrCharacter in OcrCharacters.Where(ocrCharacter => ocrCharacter.Text == text))
            {
                return ocrCharacter;
            }

            if (!addIfNotExists)
            {
                return null;
            }

            OcrCharacter ch = new OcrCharacter(text);
            OcrCharacters.Add(ch);
            return ch;
        }
    }
}