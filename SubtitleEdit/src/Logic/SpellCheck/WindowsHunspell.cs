namespace Nikse.SubtitleEdit.Logic.SpellCheck
{
    using System;
    using System.Collections.Generic;

    public class WindowsHunspell : Hunspell
    {
        private NHunspell.Hunspell hunspell;

        public WindowsHunspell(string affDictionary, string dicDictionary)
        {
            hunspell = new NHunspell.Hunspell(affDictionary, dicDictionary);
        }

        public override bool Spell(string word)
        {
            return hunspell.Spell(word);
        }

        public override List<string> Suggest(string word)
        {
            return hunspell.Suggest(word);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (hunspell != null && !hunspell.IsDisposed)
            {
                hunspell.Dispose();
            }

            hunspell = null;
        }
    }
}
