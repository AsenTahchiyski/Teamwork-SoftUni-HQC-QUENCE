namespace Nikse.SubtitleEdit.Logic
{
    using System;
    using System.Reflection;
    using System.Windows.Forms;
    using SubtitleEdit.Forms;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Microsoft Word methods (late bound) for spell checking by Nikse
    /// Mostly a bunch of hacks...
    /// </summary>
    internal class WordSpellChecker
    {
        private const int HwndBottom = 1;

        private const int SwpNoactivate = 0x0010;
        private const short SwpNomove = 0X2;
        private const short SwpNosize = 1;
        //private const short SwpNozorder = 0X4;
        private const int SwpShowwindow = 0x0040;

        private const int WdWindowStateNormal = 0;
        //private const int WdWindowStateMaximize = 1;
        //private const int WdWindowStateMinimize = 2;

        private object wordApplication;
        private object wordDocument;
        private readonly Type wordApplicationType;
        private Type wordDocumentType;
        private readonly IntPtr mainHandle;
        private int languageId = 1033; // English

        public WordSpellChecker(Main main, string languageId)
        {
            mainHandle = main.Handle;
            SetLanguageId(languageId);

            wordApplicationType = Type.GetTypeFromProgID("Word.Application");
            wordApplication = Activator.CreateInstance(wordApplicationType);

            Application.DoEvents();
            wordApplicationType.InvokeMember("WindowState", BindingFlags.SetProperty, null, wordApplication, new object[] { WdWindowStateNormal });
            wordApplicationType.InvokeMember("Top", BindingFlags.SetProperty, null, wordApplication, new object[] { -5000 }); // hide window - it's a hack
            Application.DoEvents();
        }

        private void SetLanguageId(string languageId)
        {
            try
            {
                var ci = new System.Globalization.CultureInfo(languageId);
                this.languageId = ci.LCID;
            }
            catch
            {
                this.languageId = System.Globalization.CultureInfo.CurrentUICulture.LCID;
            }
        }

        public void NewDocument()
        {
            wordDocumentType = Type.GetTypeFromProgID("Word.Document");
            wordDocument = Activator.CreateInstance(wordDocumentType);
        }

        public void CloseDocument()
        {
            object saveChanges = false;
            object p = Missing.Value;
            wordDocumentType.InvokeMember("Close", BindingFlags.InvokeMethod, null, wordDocument, new object[] { saveChanges, p, p });
        }

        public string Version
        {
            get
            {
                object wordVersion = wordApplicationType.InvokeMember("Version", BindingFlags.GetProperty, null, wordApplication, null);
                return wordVersion.ToString();
            }
        }

        public void Quit()
        {
            object saveChanges = false;
            object originalFormat = Missing.Value;
            object routeDocument = Missing.Value;
            wordApplicationType.InvokeMember("Quit", BindingFlags.InvokeMethod, null, wordApplication, new object[] { saveChanges, originalFormat, routeDocument });
            try
            {
                Marshal.ReleaseComObject(wordDocument);
                Marshal.ReleaseComObject(wordApplication);
            }
            finally
            {
                wordDocument = null;
                wordApplication = null;
            }
        }

        public string CheckSpelling(string text, out int errorsBefore, out int errorsAfter)
        {
            // insert text
            object words = wordDocumentType.InvokeMember("Words", BindingFlags.GetProperty, null, wordDocument, null);
            object range = words.GetType().InvokeMember("First", BindingFlags.GetProperty, null, words, null);
            range.GetType().InvokeMember("InsertBefore", BindingFlags.InvokeMethod, null, range, new Object[] { text });

            // set language...
            range.GetType().InvokeMember("LanguageId", BindingFlags.SetProperty, null, range, new object[] { languageId });

            // spell check error count
            object spellingErrors = wordDocumentType.InvokeMember("SpellingErrors", BindingFlags.GetProperty, null, wordDocument, null);
            object spellingErrorsCount = spellingErrors.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, spellingErrors, null);
            errorsBefore = int.Parse(spellingErrorsCount.ToString());
            Marshal.ReleaseComObject(spellingErrors);

            // perform spell check
            object p = Missing.Value;
            if (errorsBefore > 0)
            {
                wordApplicationType.InvokeMember("WindowState", BindingFlags.SetProperty, null, wordApplication, new object[] { WdWindowStateNormal });
                wordApplicationType.InvokeMember("Top", BindingFlags.SetProperty, null, wordApplication, new object[] { -10000 }); // hide window - it's a hack
                NativeMethods.SetWindowPos(mainHandle, HwndBottom, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpNoactivate); // make sure c# form is behind spell check dialog
                wordDocumentType.InvokeMember("CheckSpelling", BindingFlags.InvokeMethod, null, wordDocument, new Object[] { p, p, p, p, p, p, p, p, p, p, p, p }); // 12 parameters
                NativeMethods.SetWindowPos(mainHandle, 0, 0, 0, 0, 0, SwpShowwindow | SwpNomove | SwpNosize | SwpNoactivate); // bring c# form to front again
                wordApplicationType.InvokeMember("Top", BindingFlags.SetProperty, null, wordApplication, new object[] { -10000 }); // hide window - it's a hack
            }

            // spell check error count
            spellingErrors = wordDocumentType.InvokeMember("SpellingErrors", BindingFlags.GetProperty, null, wordDocument, null);
            spellingErrorsCount = spellingErrors.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, spellingErrors, null);
            errorsAfter = int.Parse(spellingErrorsCount.ToString());
            Marshal.ReleaseComObject(spellingErrors);

            // Get spell check text
            object resultText = range.GetType().InvokeMember("Text", BindingFlags.GetProperty, null, range, null);
            range.GetType().InvokeMember("Delete", BindingFlags.InvokeMethod, null, range, null);

            Marshal.ReleaseComObject(words);
            Marshal.ReleaseComObject(range);

            return resultText.ToString().TrimEnd(); // result needs a trimming at the end
        }
    }
}
