namespace Nikse.SubtitleEdit.Logic.Ocr
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Xml;
    using Core;
    using SubtitleEdit.Forms;
    using Dictionaries;
    using SpellCheck;

    public class OcrFixEngine : IDisposable
    {
        private string userWordListXmlFileName;
        private string fiveLetterWordListLanguageName;

        private readonly OcrFixReplaceList ocrFixReplaceList;
        private NamesList namesList;
        private HashSet<string> namesEtcList = new HashSet<string>();
        private HashSet<string> namesEtcListUppercase = new HashSet<string>();
        private HashSet<string> namesEtcListWithApostrophe = new HashSet<string>();
        private HashSet<string> namesEtcMultiWordList = new HashSet<string>(); // case sensitive phrases
        private HashSet<string> abbreviationList;
        private HashSet<string> userWordList = new HashSet<string>();
        private HashSet<string> wordSkipList = new HashSet<string>();
        private Hunspell hunspell;
        private OcrSpellCheck spellCheck;
        private readonly Form parentForm;
        private string spellCheckDictionaryName;
        private readonly string threeLetterIsoLanguageName;

        private static readonly Regex RegexAloneI = new Regex(@"\bi\b", RegexOptions.Compiled);
        private static readonly Regex RegexAloneIasL = new Regex(@"\bl\b", RegexOptions.Compiled);
        private static readonly Regex RegexSpaceBetweenNumbers = new Regex(@"\d \d", RegexOptions.Compiled);
        private static readonly Regex RegExLowercaseL = new Regex("[A-ZÆØÅÄÖÉÈÀÙÂÊÎÔÛËÏ]l[A-ZÆØÅÄÖÉÈÀÙÂÊÎÔÛËÏ]", RegexOptions.Compiled);
        private static readonly Regex RegExUppercaseI = new Regex("[a-zæøåöääöéèàùâêîôûëï]I.", RegexOptions.Compiled);
        private static readonly Regex RegExNumber1 = new Regex(@"\d\ 1", RegexOptions.Compiled);
        
        public enum AutoGuessLevel
        {
            None,
            Cautious,
            Aggressive
        }

        public bool Abort { get; set; }
       
        public List<string> AutoGuessesUsed { get; set; }
        
        public List<string> UnknownWordsFound { get; set; }
        
        public bool IsDictionaryLoaded { get; private set; }

        public CultureInfo DictionaryCulture { get; private set; }

        public string SpellCheckDictionaryName
        {
            get
            {
                if (spellCheckDictionaryName == null)
                {
                    return string.Empty;
                }

                string[] parts = spellCheckDictionaryName.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 0 ? parts[parts.Length - 1] : string.Empty;
            }

            set
            {
                string checkDictionaryName = Path.Combine(Utilities.DictionaryFolder, value);
                CultureInfo ci;
                try
                {
                    ci = value == "sh" ? new CultureInfo("sr-Latn-RS") : new CultureInfo(value);
                }
                catch
                {
                    ci = CultureInfo.CurrentCulture;
                }

                LoadSpellingDictionariesViaDictionaryFileName(ci.ThreeLetterISOLanguageName, ci, checkDictionaryName, false);
            }
        }

        /// <summary>
        /// Advanced OCR fixing via replace/spelling dictionaries + some hardcoded rules
        /// </summary>
        /// <param name="threeLetterIsoLanguageName">E.g. eng for English</param>
        /// <param name="hunspellName">Name of hunspell dictionary</param>
        /// <param name="parentForm">Used for centering/show spell check dialog</param>
        public OcrFixEngine(string threeLetterIsoLanguageName, string hunspellName, Form parentForm)
        {
            if (threeLetterIsoLanguageName == "per")
            {
                threeLetterIsoLanguageName = "fas";
            }

            this.threeLetterIsoLanguageName = threeLetterIsoLanguageName;
            this.parentForm = parentForm;

            spellCheck = new OcrSpellCheck { StartPosition = FormStartPosition.Manual };
            spellCheck.Location = new Point(parentForm.Left + (parentForm.Width / 2 - spellCheck.Width / 2),
                                             parentForm.Top + (parentForm.Height / 2 - spellCheck.Height / 2));

            ocrFixReplaceList = OcrFixReplaceList.FromLanguageId(threeLetterIsoLanguageName);
            LoadSpellingDictionaries(threeLetterIsoLanguageName, hunspellName); // Hunspell etc.

            AutoGuessesUsed = new List<string>();
            UnknownWordsFound = new List<string>();
        }

        private void LoadSpellingDictionaries(string threeLetterIsoLangName, string hunspellName)
        {
            string dictionaryFolder = Utilities.DictionaryFolder;
            if (!Directory.Exists(dictionaryFolder))
            {
                return;
            }

            if (!string.IsNullOrEmpty(hunspellName) && threeLetterIsoLangName == "eng" && 
                hunspellName.Equals("en_gb", StringComparison.OrdinalIgnoreCase) && 
                File.Exists(Path.Combine(dictionaryFolder, "en_GB.dic")))
            {
                LoadSpellingDictionariesViaDictionaryFileName("eng", new CultureInfo("en-GB"), "en_GB.dic", true);
                return;
            }

            if (!string.IsNullOrEmpty(hunspellName) && threeLetterIsoLangName == "eng" && 
                hunspellName.Equals("en_ca", StringComparison.OrdinalIgnoreCase) && 
                File.Exists(Path.Combine(dictionaryFolder, "en_CA.dic")))
            {
                LoadSpellingDictionariesViaDictionaryFileName("eng", new CultureInfo("en-CA"), "en_CA.dic", true);
                return;
            }

            if (!string.IsNullOrEmpty(hunspellName) && threeLetterIsoLangName == "eng" && 
                hunspellName.Equals("en_au", StringComparison.OrdinalIgnoreCase) && 
                File.Exists(Path.Combine(dictionaryFolder, "en_AU.dic")))
            {
                LoadSpellingDictionariesViaDictionaryFileName("eng", new CultureInfo("en-AU"), "en_AU.dic", true);
                return;
            }

            if (!string.IsNullOrEmpty(hunspellName) && threeLetterIsoLangName == "eng" && 
                hunspellName.Equals("en_za", StringComparison.OrdinalIgnoreCase) && 
                File.Exists(Path.Combine(dictionaryFolder, "en_ZA.dic")))
            {
                LoadSpellingDictionariesViaDictionaryFileName("eng", new CultureInfo("en-ZA"), "en_ZA.dic", true);
                return;
            }

            if (threeLetterIsoLangName == "eng" && File.Exists(Path.Combine(dictionaryFolder, "en_US.dic")))
            {
                LoadSpellingDictionariesViaDictionaryFileName("eng", new CultureInfo("en-US"), "en_US.dic", true);
                return;
            }

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
            {
                if (culture.ThreeLetterISOLanguageName != threeLetterIsoLangName)
                {
                    continue;
                }

                string dictionaryFileName = null;
                foreach (string dic in Directory.GetFiles(dictionaryFolder, "*.dic"))
                {
                    string name = Path.GetFileNameWithoutExtension(dic);
                    if (name.StartsWith("hyph", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    try
                    {
                        name = name.Replace('_', '-');
                        if (name.Length > 5)
                        {
                            name = name.Substring(0, 5);
                        }

                        var ci = new CultureInfo(name);
                        if (ci.ThreeLetterISOLanguageName != threeLetterIsoLangName && 
                            !ci.ThreeLetterWindowsLanguageName.Equals(threeLetterIsoLangName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        dictionaryFileName = dic;
                        break;
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                    }
                }

                if (dictionaryFileName == null)
                {
                    return;
                }

                LoadSpellingDictionariesViaDictionaryFileName(threeLetterIsoLangName, culture, dictionaryFileName, true);
                return;
            }

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (culture.ThreeLetterISOLanguageName != threeLetterIsoLangName)
                {
                    continue;
                }

                string dictionaryFileName = null;
                foreach (string dic in Directory.GetFiles(dictionaryFolder, "*.dic"))
                {
                    string name = Path.GetFileNameWithoutExtension(dic);
                    if (name != null && name.StartsWith("hyph", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    try
                    {
                        name = name.Replace('_', '-');
                        if (name.Length > 5)
                        {
                            name = name.Substring(0, 5);
                        }

                        var ci = new CultureInfo(name);
                        if (ci.ThreeLetterISOLanguageName != threeLetterIsoLangName && !ci.ThreeLetterWindowsLanguageName.Equals(threeLetterIsoLangName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        dictionaryFileName = dic;
                        break;
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Debug.WriteLine(exception.Message);
                    }
                }

                if (dictionaryFileName == null)
                {
                    return;
                }

                LoadSpellingDictionariesViaDictionaryFileName(threeLetterIsoLangName, culture, dictionaryFileName, true);
                return;
            }
        }

        private void LoadSpellingDictionariesViaDictionaryFileName(string threeLetterIsoLanguageName, CultureInfo culture, string dictionaryFileName, bool resetSkipList)
        {
            fiveLetterWordListLanguageName = Path.GetFileNameWithoutExtension(dictionaryFileName);
            if (fiveLetterWordListLanguageName != null && fiveLetterWordListLanguageName.Length > 5)
            {
                fiveLetterWordListLanguageName = fiveLetterWordListLanguageName.Substring(0, 5);
            }

            string dictionary = Utilities.DictionaryFolder + fiveLetterWordListLanguageName;
            if (resetSkipList)
            {
                wordSkipList = new HashSet<string> { Configuration.Settings.Tools.MusicSymbol, "*", "%", "#", "+", "$" };
            }

            // Load names etc list (names/noise words)
            namesList = new NamesList(Configuration.DictionariesFolder, fiveLetterWordListLanguageName, Configuration.Settings.WordLists.UseOnlineNamesEtc, Configuration.Settings.WordLists.NamesEtcUrl);
            namesEtcList = namesList.GetNames();
            namesEtcMultiWordList = namesList.GetMultiNames();
            namesEtcListUppercase = new HashSet<string>();
            foreach (string name in namesEtcList)
            {
                namesEtcListUppercase.Add(name.ToUpper());
            }

            namesEtcListWithApostrophe = new HashSet<string>();
            if (threeLetterIsoLanguageName.Equals("eng", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string namesItem in namesEtcList)
                {
                    if (!namesItem.EndsWith('s'))
                    {
                        namesEtcListWithApostrophe.Add(namesItem + "'s");
                    }
                    else
                    {
                        namesEtcListWithApostrophe.Add(namesItem + "'");
                    }
                }
            }

            // Load user words
            userWordList = new HashSet<string>();
            userWordListXmlFileName = Utilities.LoadUserWordList(userWordList, fiveLetterWordListLanguageName);

            // Find abbreviations
            abbreviationList = new HashSet<string>();
            foreach (string name in namesEtcList.Where(name => name.EndsWith('.')))
            {
                abbreviationList.Add(name);
            }

            if (threeLetterIsoLanguageName.Equals("eng", StringComparison.OrdinalIgnoreCase))
            {
                if (!abbreviationList.Contains("a.m."))
                {
                    abbreviationList.Add("a.m.");
                }

                if (!abbreviationList.Contains("p.m."))
                {
                    abbreviationList.Add("p.m.");
                }

                if (!abbreviationList.Contains("o.r."))
                {
                    abbreviationList.Add("o.r.");
                }
            }

            foreach (string name in userWordList.Where(name => name.EndsWith('.')))
            {
                abbreviationList.Add(name);
            }

            // Load Hunspell spell checker
            try
            {
                if (!File.Exists(dictionary + ".dic"))
                {
                    var fileMatches = Directory.GetFiles(Utilities.DictionaryFolder, fiveLetterWordListLanguageName + "*.dic");
                    if (fileMatches.Length > 0)
                    {
                        dictionary = fileMatches[0].Substring(0, fileMatches[0].Length - 4);
                    }
                }

                if (hunspell != null)
                {
                    hunspell.Dispose();
                }

                hunspell = Hunspell.GetHunspell(dictionary);
                IsDictionaryLoaded = true;
                spellCheckDictionaryName = dictionary;
                DictionaryCulture = culture;
            }
            catch
            {
                IsDictionaryLoaded = false;
            }
        }

        internal static Dictionary<string, string> LoadReplaceList(XmlDocument doc, string name)
        {
            var list = new Dictionary<string, string>();
            if (doc.DocumentElement == null)
            {
                return list;
            }

            XmlNode node = doc.DocumentElement.SelectSingleNode(name);
            if (node == null) return list;
            foreach (XmlNode item in node.ChildNodes)
            {
                if (item.Attributes == null || item.Attributes["to"] == null || item.Attributes["from"] == null)
                {
                    continue;
                }

                string to = item.Attributes["to"].InnerText;
                string from = item.Attributes["from"].InnerText;
                if (!list.ContainsKey(@from))
                {
                    list.Add(@from, to);
                }
            }

            return list;
        }

        internal static Dictionary<string, string> LoadRegExList(XmlDocument doc, string name)
        {
            var list = new Dictionary<string, string>();
            if (doc.DocumentElement == null)
            {
                return list;
            }

            XmlNode node = doc.DocumentElement.SelectSingleNode(name);
            if (node == null)
            {
                return list;
            }

            foreach (XmlNode item in node.ChildNodes)
            {
                if (item.Attributes == null || item.Attributes["replaceWith"] == null || item.Attributes["find"] == null)
                {
                    continue;
                }

                string to = item.Attributes["replaceWith"].InnerText;
                string from = item.Attributes["find"].InnerText;
                if (!list.ContainsKey(@from))
                {
                    list.Add(@from, to);
                }
            }

            return list;
        }

        public string FixOcrErrors(string text, int index, string lastLine, bool logSuggestions, AutoGuessLevel autoGuess)
        {
            var sb = new StringBuilder();
            var word = new StringBuilder();

            if (Configuration.Settings.Tools.OcrFixUseHardcodedRules)
            {
                text = text.Replace("ﬁ", "fi"); // fb01
                text = text.Replace("ﬂ", "fl"); // fb02
                text = text.Replace('ν', 'v'); // NOTE: first 'v' is a special unicode character!!!!
            }

            text = ReplaceWordsBeforeLineFixes(text);

            text = FixCommenOcrLineErrors(text, lastLine);

            string lastWord = null;
            foreach (char t in text)
            {
                if (" ¡¿,.!?:;()[]{}+-£\"#&%\r\n".Contains(t)) // removed $
                {
                    if (word.Length > 0)
                    {
                        string fixedWord;
                        if (lastWord != null && lastWord.Contains("COLOR=", StringComparison.OrdinalIgnoreCase))
                        {
                            fixedWord = word.ToString();
                        }
                        else
                        {
                            bool doFixWord = !(word.Length == 1 && sb.Length > 1 && sb.EndsWith('-'));
                            fixedWord = doFixWord ? ocrFixReplaceList.FixCommonWordErrors(word.ToString()) : word.ToString();
                        }

                        sb.Append(fixedWord);
                        lastWord = fixedWord;
                        word.Clear();
                    }

                    sb.Append(t);
                }
                else
                {
                    word.Append(t);
                }
            }

            if (word.Length > 0) // last word
            {
                bool doFixWord = !(word.Length == 1 && sb.Length > 1 && sb.EndsWith('-'));
                var fixedWord = doFixWord ? ocrFixReplaceList.FixCommonWordErrors(word.ToString()) : word.ToString();
                sb.Append(fixedWord);
            }

            text = FixCommenOcrLineErrors(sb.ToString(), lastLine);
            int wordsNotFound;
            text = FixUnknownWordsViaGuessOrPrompt(out wordsNotFound, text, index, null, true, false, logSuggestions, autoGuess);
            if (!Configuration.Settings.Tools.OcrFixUseHardcodedRules)
            {
                return text;
            }

            text = FixLowercaseIToUppercaseI(text, lastLine);
            if (SpellCheckDictionaryName.StartsWith("en_", StringComparison.Ordinal) || threeLetterIsoLanguageName == "eng")
            {
                string oldText = text;
                text = FixCommonErrors.FixAloneLowercaseIToUppercaseLine(RegexAloneI, oldText, text, 'i');
                text = FixCommonErrors.FixAloneLowercaseIToUppercaseLine(RegexAloneIasL, oldText, text, 'l');
            }
            else if (threeLetterIsoLanguageName == "fra")
            {
                text = FixFrenchLApostrophe(text, " I'", lastLine);
                text = FixFrenchLApostrophe(text, " L'", lastLine);
                text = FixFrenchLApostrophe(text, " l'", lastLine);
                text = FixFrenchLApostrophe(text, " I’", lastLine);
                text = FixFrenchLApostrophe(text, " L’", lastLine);
                text = FixFrenchLApostrophe(text, " l’", lastLine);
            }

            text = RemoveSpaceBetweenNumbers(text);
            return text;
        }

        private static string FixFrenchLApostrophe(string text, string tag, string lastLine)
        {
            bool endingBeforeThis = string.IsNullOrEmpty(lastLine) || lastLine.EndsWith('.') || lastLine.EndsWith('!') || lastLine.EndsWith('?') ||
                                    lastLine.EndsWith(".</i>") || lastLine.EndsWith("!</i>") || lastLine.EndsWith("?</i>") ||
                                    lastLine.EndsWith(".</font>") || lastLine.EndsWith("!</font>") || lastLine.EndsWith("?</font>");
            if (text.StartsWith(tag.TrimStart(), StringComparison.Ordinal) && text.Length > 3)
            {
                if (endingBeforeThis || Utilities.UppercaseLetters.Contains(text[2]))
                {
                    text = @"L" + text.Substring(1);
                }
                else if (Utilities.LowercaseLetters.Contains(text[2]))
                {
                    text = @"l" + text.Substring(1);
                }
            }
            else if (text.StartsWith("<i>" + tag.TrimStart(), StringComparison.Ordinal) && text.Length > 6)
            {
                if (endingBeforeThis || Utilities.UppercaseLetters.Contains(text[5]))
                {
                    text = text.Remove(3, 1).Insert(3, "L");
                }
                else if (Utilities.LowercaseLetters.Contains(text[5]))
                {
                    text = text.Remove(3, 1).Insert(3, "l");
                }
            }

            int start = text.IndexOf(tag, StringComparison.Ordinal);
            while (start > 0)
            {
                lastLine = HtmlUtil.RemoveHtmlTags(text.Substring(0, start)).TrimEnd().TrimEnd('-').TrimEnd();
                endingBeforeThis = string.IsNullOrEmpty(lastLine) || lastLine.EndsWith('.') || lastLine.EndsWith('!') || lastLine.EndsWith('?');
                if (start < text.Length - 4)
                {
                    if (start == 1 && text.StartsWith('-'))
                    {
                        endingBeforeThis = true;
                    }

                    if (endingBeforeThis || Utilities.UppercaseLetters.Contains(text[start + 3]))
                    {
                        text = text.Remove(start + 1, 1).Insert(start + 1, "L");
                    }
                    else if (Utilities.LowercaseLetters.Contains(text[start + 3]))
                    {
                        text = text.Remove(start + 1, 1).Insert(start + 1, "l");
                    }
                }

                start = text.IndexOf(tag, start + 1, StringComparison.Ordinal);
            }

            tag = Environment.NewLine + tag.Trim();
            start = text.IndexOf(tag, StringComparison.Ordinal);
            while (start > 0)
            {
                lastLine = HtmlUtil.RemoveHtmlTags(text.Substring(0, start)).TrimEnd().TrimEnd('-').TrimEnd();
                endingBeforeThis = string.IsNullOrEmpty(lastLine) || lastLine.EndsWith('.') || lastLine.EndsWith('!') || lastLine.EndsWith('?') || lastLine.EndsWith(".</i>");
                if (start < text.Length - 5)
                {
                    if (endingBeforeThis || Utilities.UppercaseLetters.Contains(text[start + 2 + Environment.NewLine.Length]))
                    {
                        text = text.Remove(start + Environment.NewLine.Length, 1).Insert(start + Environment.NewLine.Length, "L");
                    }
                    else if (Utilities.LowercaseLetters.Contains(text[start + 2 + Environment.NewLine.Length]))
                    {
                        text = text.Remove(start + Environment.NewLine.Length, 1).Insert(start + Environment.NewLine.Length, "l");
                    }
                }

                start = text.IndexOf(tag, start + 1, StringComparison.Ordinal);
            }

            tag = Environment.NewLine + "<i>" + tag.Trim();
            start = text.IndexOf(tag, StringComparison.Ordinal);
            while (start > 0)
            {
                lastLine = HtmlUtil.RemoveHtmlTags(text.Substring(0, start)).TrimEnd().TrimEnd('-').TrimEnd();
                endingBeforeThis = string.IsNullOrEmpty(lastLine) || lastLine.EndsWith('.') || lastLine.EndsWith('!') || lastLine.EndsWith('?') || lastLine.EndsWith(".</i>", StringComparison.Ordinal);
                if (start < text.Length - 8)
                {
                    if (endingBeforeThis || Utilities.UppercaseLetters.Contains(text[start + 5 + Environment.NewLine.Length]))
                    {
                        text = text.Remove(start + Environment.NewLine.Length + 3, 1).Insert(start + Environment.NewLine.Length + 3, "L");
                    }
                    else if (Utilities.LowercaseLetters.Contains(text[start + 5 + Environment.NewLine.Length]))
                    {
                        text = text.Remove(start + Environment.NewLine.Length + 3, 1).Insert(start + Environment.NewLine.Length + 3, "l");
                    }
                }

                start = text.IndexOf(tag, start + 1, StringComparison.Ordinal);
            }

            return text;
        }

        private string ReplaceWordsBeforeLineFixes(string text)
        {
            string lastWord = null;
            var sb = new StringBuilder();
            var word = new StringBuilder();
            foreach (char t in text)
            {
                if (" ¡¿,.!?:;()[]{}+-£\"#&%\r\n".Contains(t)) // removed $
                {
                    if (word.Length > 0)
                    {
                        string fixedWord;
                        if (lastWord != null && lastWord.Contains("COLOR=", StringComparison.OrdinalIgnoreCase))
                        {
                            fixedWord = word.ToString();
                        }
                        else
                        {
                            fixedWord = ocrFixReplaceList.FixCommonWordErrorsQuick(word.ToString());
                        }

                        sb.Append(fixedWord);
                        lastWord = fixedWord;
                        word = new StringBuilder();
                    }

                    sb.Append(t);
                }
                else
                {
                    word.Append(t);
                }
            }

            if (word.Length > 0) // last word
            {
                string fixedWord = ocrFixReplaceList.FixCommonWordErrorsQuick(word.ToString());
                sb.Append(fixedWord);
            }

            return sb.ToString();
        }

        private static string RemoveSpaceBetweenNumbers(string text)
        {
            Match match = RegexSpaceBetweenNumbers.Match(text);
            while (match.Success)
            {
                bool doFix = !(match.Index + 4 < text.Length && text[match.Index + 3] == '/' && char.IsDigit(text[match.Index + 4]));
                if (doFix)
                {
                    text = text.Remove(match.Index + 1, 1);
                    match = RegexSpaceBetweenNumbers.Match(text);
                }
                else
                {
                    match = RegexSpaceBetweenNumbers.Match(text, match.Index + 1);
                }
            }

            return text;
        }

        public static string FixLowerCaseLInsideUpperCaseWord(string word)
        {
            if (word.Length <= 3 || word.Replace("l", string.Empty).ToUpper() != word.Replace("l", string.Empty))
            {
                return word;
            }

            if (!word.Contains(new[] { '<', '>', '\'' }))
            {
                word = word.Replace('l', 'I');
            }

            return word;
        }

        private string FixCommenOcrLineErrors(string input, string lastLine)
        {
            input = FixOcrErrorViaLineReplaceList(input);
            input = FixOcrErrorsViaHardcodedRules(input, lastLine, abbreviationList);
            input = FixOcrErrorViaLineReplaceList(input);

            if (!Configuration.Settings.Tools.OcrFixUseHardcodedRules)
            {
                return input;
            }

            if (input.StartsWith('~'))
            {
                input = ("- " + input.Remove(0, 1)).Replace("  ", " ");
            }

            input = input.Replace(Environment.NewLine + "~", Environment.NewLine + "- ").Replace("  ", " ");

            if (input.Length < 10 && input.Length > 4 && !input.Contains(Environment.NewLine) && input.StartsWith("II") && input.EndsWith("II"))
            {
                input = "\"" + input.Substring(2, input.Length - 4) + "\"";
            }

            // e.g. "selectionsu." -> "selections..."
            if (input.EndsWith("u.", StringComparison.Ordinal) && hunspell != null)
            {
                string[] words = input.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0)
                {
                    string lastWord = words[words.Length - 1].Trim();
                    if (lastWord.Length > 2 &&
                        char.IsLower(lastWord[0]) &&
                        !IsWordOrWordsCorrect(lastWord) &&
                        IsWordOrWordsCorrect(lastWord.Substring(0, lastWord.Length - 1)))
                    {
                        input = input.Substring(0, input.Length - 2) + "...";
                    }
                }
            }

            // music notes
            if (input.StartsWith(".'", StringComparison.Ordinal) && input.EndsWith(".'", StringComparison.Ordinal))
            {
                input = input.Replace(".'", Configuration.Settings.Tools.MusicSymbol);
            }

            return input;
        }

        private string FixLowercaseIToUppercaseI(string input, string lastLine)
        {
            var sb = new StringBuilder();
            var lines = input.SplitToLines();
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];

                if (i > 0)
                {
                    lastLine = lines[i - 1];
                }

                lastLine = HtmlUtil.RemoveHtmlTags(lastLine);

                if (string.IsNullOrEmpty(lastLine) ||
                    lastLine.EndsWith('.') ||
                    lastLine.EndsWith('!') ||
                    lastLine.EndsWith('?'))
                {
                    var st = new StripableText(l);
                    if (st.StrippedText.StartsWith('i') && !st.Pre.EndsWith('[') && !st.Pre.EndsWith('(') && !st.Pre.EndsWith("...", StringComparison.Ordinal))
                    {
                        if (string.IsNullOrEmpty(lastLine) || (!lastLine.EndsWith("...", StringComparison.Ordinal) && !EndsWithAbbreviation(lastLine, abbreviationList)))
                        {
                            l = st.Pre + "I" + st.StrippedText.Remove(0, 1) + st.Post;
                        }
                    }
                }

                sb.AppendLine(l);
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }

        private static bool EndsWithAbbreviation(string line, HashSet<string> abbreviationList)
        {
            if (string.IsNullOrEmpty(line) || abbreviationList == null)
            {
                return false;
            }

            line = line.ToLower();
            if (abbreviationList.Any(abbreviation => line.EndsWith(" " + abbreviation.ToLower())))
            {
                return true;
            }

            return line.Length > 5 && line[line.Length - 3] == '.' && Utilities.AllLetters.Contains(line[line.Length - 2]);
        }

        public string FixOcrErrorsViaHardcodedRules(string input, string lastLine, HashSet<string> abbreviationList)
        {
            if (!Configuration.Settings.Tools.OcrFixUseHardcodedRules)
            {
                return input;
            }

            input = input.Replace(",...", "...");

            if (input.StartsWith("..") && !input.StartsWith("...", StringComparison.Ordinal))
            {
                input = "." + input;
            }

            string pre = string.Empty;
            if (input.StartsWith("- ", StringComparison.Ordinal))
            {
                pre = "- ";
                input = input.Remove(0, 2);
            }
            else if (input.StartsWith('-'))
            {
                pre = "-";
                input = input.Remove(0, 1);
            }

            bool hasDotDot = input.Contains("..") || input.Contains(". .");
            if (hasDotDot)
            {
                if (input.Length > 5 && input.StartsWith("..") && Utilities.AllLettersAndNumbers.Contains(input[2]))
                {
                    input = "..." + input.Remove(0, 2);
                }

                if (input.Length > 7 && input.StartsWith("<i>..") && Utilities.AllLettersAndNumbers.Contains(input[5]))
                {
                    input = "<i>..." + input.Remove(0, 5);
                }

                if (input.Length > 5 && input.StartsWith(".. ") && Utilities.AllLettersAndNumbers.Contains(input[3]))
                {
                    input = "..." + input.Remove(0, 3);
                }

                if (input.Length > 7 && input.StartsWith("<i>.. ") && Utilities.AllLettersAndNumbers.Contains(input[6]))
                {
                    input = "<i>..." + input.Remove(0, 6);
                }

                if (input.Contains(Environment.NewLine + ".. "))
                {
                    input = input.Replace(Environment.NewLine + ".. ", Environment.NewLine + "...");
                }

                if (input.Contains(Environment.NewLine + "<i>.. "))
                {
                    input = input.Replace(Environment.NewLine + "<i>.. ", Environment.NewLine + "<i>...");
                }

                if (input.StartsWith(". ..", StringComparison.Ordinal))
                {
                    input = "..." + input.Remove(0, 4);
                }

                if (input.StartsWith(".. .", StringComparison.Ordinal))
                {
                    input = "..." + input.Remove(0, 4);
                }

                if (input.StartsWith(". . ."))
                {
                    input = "..." + input.Remove(0, 5);
                }

                if (input.StartsWith("... ", StringComparison.Ordinal))
                {
                    input = input.Remove(3, 1);
                }
            }

            input = pre + input;

            if (hasDotDot)
            {
                if (input.StartsWith("<i>. ..", StringComparison.Ordinal))
                {
                    input = "<i>..." + input.Remove(0, 7);
                }

                if (input.StartsWith("<i>.. .", StringComparison.Ordinal))
                {
                    input = "<i>..." + input.Remove(0, 7);
                }

                if (input.StartsWith("<i>. . .", StringComparison.Ordinal))
                {
                    input = "<i>..." + input.Remove(0, 8);
                }

                if (input.StartsWith("<i>... ", StringComparison.Ordinal))
                {
                    input = input.Remove(6, 1);
                }

                if (input.StartsWith(". . <i>.", StringComparison.Ordinal))
                {
                    input = "<i>..." + input.Remove(0, 8);
                }

                if (input.StartsWith("...<i>", StringComparison.Ordinal) && (input.IndexOf("</i>", StringComparison.Ordinal) > input.IndexOf(' ')))
                {
                    input = "<i>..." + input.Remove(0, 6);
                }

                if (input.EndsWith(". ..", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 4, 4) + "...";
                }

                if (input.EndsWith(".. .", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 4, 4) + "...";
                }

                if (input.EndsWith(". . .", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 5, 5) + "...";
                }

                if (input.EndsWith(". ..."))
                {
                    input = input.Remove(input.Length - 5, 5) + "...";
                }

                if (input.EndsWith(". ..</i>", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 8, 8) + "...</i>";
                }

                if (input.EndsWith(".. .</i>", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 8, 8) + "...</i>";
                }

                if (input.EndsWith(". . .</i>", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 9, 9) + "...</i>";
                }

                if (input.EndsWith(". ...</i>", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 9, 9) + "...</i>";
                }

                if (input.EndsWith(".</i> . .", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 9, 9) + "...</i>";
                }

                if (input.EndsWith(".</i>..", StringComparison.Ordinal))
                {
                    input = input.Remove(input.Length - 7, 7) + "...</i>";
                }

                input = input.Replace(".</i> . ." + Environment.NewLine, "...</i>" + Environment.NewLine);

                input = input.Replace(".. ?", "..?");
                input = input.Replace("..?", "...?");
                input = input.Replace("....?", "...?");

                input = input.Replace(".. !", "..!");
                input = input.Replace("..!", "...!");
                input = input.Replace("....!", "...!");

                input = input.Replace("... ?", "...?");
                input = input.Replace("... !", "...!");

                input = input.Replace("....", "...");
                input = input.Replace("....", "...");

                if (input.StartsWith("- ...") && lastLine != null && 
                    lastLine.EndsWith("...") && !(input.Contains(Environment.NewLine + "-")))
                {
                    input = input.Remove(0, 2);
                }

                if (input.StartsWith("-...") && lastLine != null && 
                    lastLine.EndsWith("...") && !(input.Contains(Environment.NewLine + "-")))
                {
                    input = input.Remove(0, 1);
                }
            }

            if (input.Length > 2 && input[0] == '-' && Utilities.UppercaseLetters.Contains(input[1]))
            {
                input = input.Insert(1, " ");
            }

            if (input.Length > 5 && input.StartsWith("<i>-", StringComparison.Ordinal) && Utilities.UppercaseLetters.Contains(input[4]))
            {
                input = input.Insert(4, " ");
            }

            int idx = input.IndexOf(Environment.NewLine + "-", StringComparison.Ordinal);
            if (idx > 0 && idx + Environment.NewLine.Length + 1 < input.Length && 
                Utilities.UppercaseLetters.Contains(input[idx + Environment.NewLine.Length + 1]))
            {
                input = input.Insert(idx + Environment.NewLine.Length + 1, " ");
            }

            idx = input.IndexOf(Environment.NewLine + "<i>-", StringComparison.Ordinal);
            if (idx > 0 && Utilities.UppercaseLetters.Contains(input[idx + Environment.NewLine.Length + 4]))
            {
                input = input.Insert(idx + Environment.NewLine.Length + 4, " ");
            }

            if (string.IsNullOrEmpty(lastLine) ||
                lastLine.EndsWith('.') ||
                lastLine.EndsWith('!') ||
                lastLine.EndsWith('?') ||
                lastLine.EndsWith(']') ||
                lastLine.EndsWith('♪'))
            {
                lastLine = HtmlUtil.RemoveHtmlTags(lastLine);
                var st = new StripableText(input);
                if (lastLine == null || (!lastLine.EndsWith("...") && !EndsWithAbbreviation(lastLine, abbreviationList)))
                {
                    if (st.StrippedText.Length > 0 && !char.IsUpper(st.StrippedText[0]) && 
                        !st.Pre.EndsWith('[') && !st.Pre.EndsWith('(') && !st.Pre.EndsWith("..."))
                    {
                        if (!HtmlUtil.StartsWithUrl(st.StrippedText))
                        {
                            var uppercaseLetter = char.ToUpper(st.StrippedText[0]);
                            if (st.StrippedText.Length > 1 && uppercaseLetter == 'L' && @"abcdfghjklmnpqrstvwxz".Contains(st.StrippedText[1]))
                            {
                                uppercaseLetter = 'I';
                            }

                            if ((st.StrippedText.StartsWith("lo ") || st.StrippedText == "lo.") && threeLetterIsoLanguageName == "ita")
                            {
                                uppercaseLetter = 'I';
                            }

                            if ((st.StrippedText.StartsWith("k ", StringComparison.Ordinal) || st.StrippedText.StartsWith("m ", StringComparison.Ordinal) || st.StrippedText.StartsWith("n ") || st.StrippedText.StartsWith("r ") || st.StrippedText.StartsWith("s ") || st.StrippedText.StartsWith("t ")) &&
                                st.Pre.EndsWith('\'') && threeLetterIsoLanguageName == "nld")
                            {
                                uppercaseLetter = st.StrippedText[0];
                            }

                            if ((st.StrippedText.StartsWith("l-I'll ", StringComparison.Ordinal) || st.StrippedText.StartsWith("l-l'll ", StringComparison.Ordinal)) && threeLetterIsoLanguageName == "eng")
                            {
                                uppercaseLetter = 'I';
                                st.StrippedText = "I-I" + st.StrippedText.Remove(0, 3);
                            }

                            st.StrippedText = uppercaseLetter + st.StrippedText.Substring(1);
                            input = st.Pre + st.StrippedText + st.Post;
                        }
                    }
                }
            }

            // lines ending with ". should often end at ... (of no other quotes exists near by)
            if ((lastLine == null || !lastLine.Contains('"')) &&
                input.EndsWith("\".") && input.IndexOf('"') == input.LastIndexOf('"') && input.Length > 3)
            {
                var lastChar = input[input.Length - 3];
                if (!char.IsDigit(lastChar))
                {
                    int position = input.Length - 2;
                    input = input.Remove(position).Insert(position, "...");
                }
            }

            // change '<number><space>1' to '<number>1'
            if (input.Contains('1'))
            {
                Match match = RegExNumber1.Match(input);
                while (match.Success)
                {
                    bool doFix = !(match.Index + 4 < input.Length && input[match.Index + 3] == '/' && char.IsDigit(input[match.Index + 4]));

                    if (doFix)
                    {
                        input = input.Substring(0, match.Index + 1) + input.Substring(match.Index + 2);
                        match = RegExNumber1.Match(input);
                    }
                    else
                    {
                        match = RegExNumber1.Match(input, match.Index + 1);
                    }
                }
            }

            // change '' to "
            input = input.Replace("''", "\"");

            // change 'sequeI of' to 'sequel of'
            if (input.Contains('I'))
            {
                var match = RegExUppercaseI.Match(input);
                while (match.Success)
                {
                    bool doFix = !(match.Index >= 1 && input.Substring(match.Index - 1).StartsWith("Mc"));
                    if (match.Index >= 2 && input.Substring(match.Index - 2).StartsWith("Mac"))
                    {
                        doFix = false;
                    }

                    if (doFix)
                    {
                        input = input.Substring(0, match.Index + 1) + "l" + input.Substring(match.Index + 2);
                    }

                    if (match.Index + 1 < input.Length)
                    {
                        match = RegExUppercaseI.Match(input, match.Index + 1);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // change 'NlCE' to 'NICE'
            if (input.Contains('l'))
            {
                var match = RegExLowercaseL.Match(input);
                while (match.Success)
                {
                    input = input.Substring(0, match.Index + 1) + "I" + input.Substring(match.Index + 2);
                    match = RegExLowercaseL.Match(input);
                }
            }

            return input;
        }

        public string FixOcrErrorViaLineReplaceList(string input)
        {
            return ocrFixReplaceList.FixOcrErrorViaLineReplaceList(input);
        }

        public string FixUnknownWordsViaGuessOrPrompt(out int wordsNotFound, string line, int index, Bitmap bitmap, bool autoFix, bool promptForFixingErrors, bool log, AutoGuessLevel autoGuess)
        {
            var localIgnoreWords = new List<string>();
            wordsNotFound = 0;

            if (promptForFixingErrors && line.Length == 1 && !IsWordKnownOrNumber(line, line))
            {
                SpellCheckOcrTextResult res = SpellCheckOcrText(line, bitmap, line, localIgnoreWords);
                if (res.FixedWholeLine || res.Fixed)
                {
                    return res.Line;
                }

                wordsNotFound++;
                return line;
            }

            if (hunspell == null)
            {
                return line;
            }

            string tempLine = line;
            //foreach (string name in _namesEtcList)
            //{
            //    int start = tempLine.IndexOf(name);
            //    if (start >= 0)
            //    {
            //        if (start == 0 || (Environment.NewLine + " ¡¿,.!?:;()[]{}+-$£\"”“#&%…—").Contains(tempLine[start - 1].ToString()))
            //        {
            //            int end = start + name.Length;
            //            if (end >= tempLine.Length || (Environment.NewLine + " ¡¿,.!?:;()[]{}+-$£\"”“#&%…—").Contains(tempLine[end].ToString()))
            //                tempLine = tempLine.Remove(start, name.Length);
            //        }
            //    }
            //}

            const string p = @" ¡¿,.!?:;()[]{}+-$£""”“#&%…—♪";
            foreach (string name in namesEtcMultiWordList)
            {
                int start = tempLine.FastIndexOf(name);
                if (start >= 0)
                {
                    if (start == 0 || (Environment.NewLine + p).Contains(tempLine[start - 1]))
                    {
                        int end = start + name.Length;
                        if (end >= tempLine.Length || (Environment.NewLine + p).Contains(tempLine[end]))
                        {
                            tempLine = tempLine.Remove(start, name.Length);
                        }
                    }
                }
            }

            string[] words = tempLine
                .Replace("</i>", string.Empty)
                .Split((Environment.NewLine + " ¡¿,.!?:;()[]{}+-£\"”“#&%…—♪")
                .ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i].TrimStart('\'');
                string wordNotEndTrimmed = word;
                word = word.TrimEnd('\'');
                string wordNoItalics = HtmlUtil.RemoveOpenCloseTags(word, HtmlUtil.TagItalic);
                if (!IsWordKnownOrNumber(wordNoItalics, line) && !localIgnoreWords.Contains(wordNoItalics))
                {
                    bool correct = DoSpell(word);
                    if (!correct)
                    {
                        correct = DoSpell(word.Trim('\''));
                    }

                    if (!correct && word.Length > 3 && !word.EndsWith("ss") && !string.IsNullOrEmpty(threeLetterIsoLanguageName) &&
                        (threeLetterIsoLanguageName == "eng" || threeLetterIsoLanguageName == "dan" || threeLetterIsoLanguageName == "swe" || threeLetterIsoLanguageName == "nld"))
                    {
                        correct = DoSpell(word.TrimEnd('s'));
                    }

                    if (!correct)
                    {
                        correct = DoSpell(wordNoItalics);
                    }

                    if (!correct && userWordList.Contains(wordNoItalics))
                    {
                        correct = true;
                    }

                    if (!correct && !line.Contains(word))
                    {
                        correct = true; // already fixed
}

                    if (!correct && Configuration.Settings.Tools.SpellCheckEnglishAllowInQuoteAsIng && wordNotEndTrimmed.EndsWith('\'') &&
                        SpellCheckDictionaryName.StartsWith("en_") && word.EndsWith("in", StringComparison.OrdinalIgnoreCase))
                    {
                        correct = DoSpell(word + "g");
                    }

                    if (!correct)
                    {
                        //look for match via dash'ed word, e.g. sci-fi
                        string dashedWord = GetDashedWordBefore(wordNoItalics, line, words, i);
                        if (!string.IsNullOrEmpty(dashedWord))
                        {
                            correct = IsWordKnownOrNumber(dashedWord, line);
                            if (!correct)
                            {
                                correct = DoSpell(dashedWord);
                            }
                        }

                        if (!correct)
                        {
                            dashedWord = GetDashedWordAfter(wordNoItalics, line, words, i);
                            if (!string.IsNullOrEmpty(dashedWord))
                            {
                                correct = IsWordKnownOrNumber(dashedWord, line);
                                if (!correct)
                                {
                                    correct = DoSpell(dashedWord);
                                }
                            }
                        }
                    }

                    if (!correct && word.Contains('/') && !word.Contains("//"))
                    {
                        var slashedWords = word.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        bool allSlashedCorrect = true;
                        foreach (var slashedWord in slashedWords)
                        {
                            if (slashedWord.Length < 2)
                            {
                                allSlashedCorrect = false;
                            }

                            if (allSlashedCorrect && !(DoSpell(slashedWord) || IsWordKnownOrNumber(slashedWord, line)))
                            {
                                allSlashedCorrect = false;
                            }
                        }

                        correct = allSlashedCorrect;
                    }

                    if (word.Length == 0)
                    {
                        correct = true;
                    }

                    if (correct)
                    {
                        continue;
                    }

                    wordsNotFound++;
                    if (log)
                    {
                        string nf = word;
                        if (nf.StartsWith("<i>", StringComparison.Ordinal))
                        {
                            nf = nf.Remove(0, 3);
                        }

                        UnknownWordsFound.Add(string.Format("#{0}: {1}", index + 1, nf));
                    }

                    if (autoFix && autoGuess != AutoGuessLevel.None)
                    {
                        var guesses = new List<string>();
                        if (word.Length > 5 && autoGuess == AutoGuessLevel.Aggressive)
                        {
                            guesses = (List<string>)CreateGuessesFromLetters(word);

                            if (word[0] == 'L')
                            {
                                guesses.Add("I" + word.Substring(1));
                            }

                            if (word.Contains('$'))
                            {
                                guesses.Add(word.Replace("$", "s"));
                            }

                            string wordWithCasingChanged = GetWordWithDominatedCasing(word);
                            if (DoSpell(word.ToLower()))
                            {
                                guesses.Insert(0, wordWithCasingChanged);
                            }
                        }
                        else if (Configuration.Settings.Tools.OcrFixUseHardcodedRules)
                        {
                            if (word[0] == 'L')
                            {
                                guesses.Add("I" + word.Substring(1));
                            }

                            if (word.Length > 2 && word[0] == 'I' && char.IsLower(word[1]))
                            {
                                guesses.Add("l" + word.Substring(1));
                            }

                            if (i == 0)
                            {
                                guesses.Add(word.Replace(@"\/", "V"));
                            }
                            else
                            {
                                guesses.Add(word.Replace(@"\/", "v"));
                            }

                            guesses.Add(word.Replace("ﬁ", "fi"));
                            guesses.Add(word.Replace("ﬁ", "fj"));
                            guesses.Add(word.Replace("ﬂ", "fl"));
                            if (word.Contains('$'))
                            {
                                guesses.Add(word.Replace("$", "s"));
                            }

                            if (!word.EndsWith('€') && !word.StartsWith('€'))
                            {
                                guesses.Add(word.Replace("€", "e"));
                            }

                            guesses.Add(word.Replace("/", "l"));
                            guesses.Add(word.Replace(")/", "y"));
                        }

                        foreach (string guess in guesses)
                        {
                            if (!IsWordOrWordsCorrect(guess) || guess.StartsWith("f "))
                            {
                                continue;
                            }

                            string replacedLine = OcrFixReplaceList.ReplaceWord(line, word, guess);
                            if (replacedLine != line)
                            {
                                if (log)
                                {
                                    AutoGuessesUsed.Add(string.Format("#{0}: {1} -> {2} in line via '{3}': {4}", index + 1, word, guess, "OCRFixReplaceList.xml", line.Replace(Environment.NewLine, " ")));
                                }

                                //line = line.Remove(match.Index, match.Value.Length).Insert(match.Index, guess);
                                line = replacedLine;
                                wordsNotFound--;
                                if (log && UnknownWordsFound.Count > 0)
                                {
                                    UnknownWordsFound.RemoveAt(UnknownWordsFound.Count - 1);
                                }

                                correct = true;
                                break;
                            }
                        }
                    }

                    if (correct || !promptForFixingErrors)
                    {
                        continue;
                    }

                    var suggestions = new List<string>();

                    if ((word == "Lt's" || word == "Lt'S") && SpellCheckDictionaryName.StartsWith("en_", StringComparison.Ordinal))
                    {
                        suggestions.Add("It's");
                    }
                    else
                    {
                        if (word.ToUpper() != "LT'S" && word.ToUpper() != "SOX'S") // TODO: Get fixed nhunspell
                        {
                            suggestions = DoSuggest(word); // 0.9.6 fails on "Lt'S"
                        }
                    }

                    if (word.StartsWith("<i>"))
                    {
                        word = word.Remove(0, 3);
                    }

                    if (word.EndsWith("</i>"))
                    {
                        word = word.Remove(word.Length - 4, 4);
                    }

                    SpellCheckOcrTextResult res = SpellCheckOcrText(line, bitmap, word, suggestions);

                    if (res.FixedWholeLine)
                    {
                        return res.Line;
                    }

                    if (!res.Fixed)
                    {
                        continue;
                    }

                    localIgnoreWords.Add(word);
                    line = res.Line;
                    wordsNotFound--;
                }
            }

            return line;
        }

        private static string GetDashedWordBefore(string word, string line, string[] words, int index)
        {
            if (index > 0 && line.Contains(words[index - 1] + "-" + word))
            {
                return HtmlUtil.RemoveOpenCloseTags(words[index - 1] + "-" + word, HtmlUtil.TagItalic);
            }

            return null;
        }

        private static string GetDashedWordAfter(string word, string line, string[] words, int index)
        {
            if (index < words.Length - 1 && line.Contains(word + "-" + words[index + 1].Replace("</i>", string.Empty)))
            {
                return HtmlUtil.RemoveOpenCloseTags(word + "-" + words[index + 1], HtmlUtil.TagItalic);
            }

            return null;
        }

        private static string GetWordWithDominatedCasing(string word)
        {
            int lowercase = 0;
            int uppercase = 0;
            foreach (char t in word)
            {
                if (Utilities.LowercaseLetters.Contains(t))
                {
                    lowercase++;
                }
                else if (Utilities.UppercaseLetters.Contains(t))
                {
                    uppercase++;
                }
            }

            return uppercase > lowercase ? word.ToUpper() : word.ToLower();
        }

        /// <summary>
        /// SpellCheck for OCR
        /// </summary>
        /// <returns>True, if word is fixed</returns>
        private SpellCheckOcrTextResult SpellCheckOcrText(string line, Bitmap bitmap, string word, List<string> suggestions)
        {
            var result = new SpellCheckOcrTextResult { Fixed = false, FixedWholeLine = false, Line = null, Word = null };
            spellCheck.Initialize(word, suggestions, line, bitmap);
            spellCheck.ShowDialog(parentForm);
            switch (spellCheck.ActionResult)
            {
                case OcrSpellCheck.Action.Abort:
                    Abort = true;
                    result.FixedWholeLine = true;
                    result.Line = line;
                    break;
                case OcrSpellCheck.Action.AddToUserDictionary:
                    if (userWordListXmlFileName != null)
                    {
                        userWordList.Add(spellCheck.Word.Trim().ToLower());
                        Utilities.AddToUserDictionary(spellCheck.Word.Trim().ToLower(), fiveLetterWordListLanguageName);
                    }

                    result.Word = spellCheck.Word;
                    result.Fixed = true;
                    result.Line = line;
                    if (word == result.Word)
                    {
                        return result;
                    }

                    break;
                case OcrSpellCheck.Action.AddToNames:
                    result.Word = spellCheck.Word;
                    result.Fixed = true;
                    try
                    {
                        string s = spellCheck.Word.Trim();
                        if (s.Contains(' '))
                        {
                            namesEtcMultiWordList.Add(s);
                        }
                        else
                        {
                            namesEtcList.Add(s);
                            namesEtcListUppercase.Add(s.ToUpper());
                            if (fiveLetterWordListLanguageName.StartsWith("en"))
                            {
                                if (!s.EndsWith('s'))
                                {
                                    namesEtcListWithApostrophe.Add(s + "'s");
                                }
                                else
                                {
                                    namesEtcListWithApostrophe.Add(s + "'");
                                }
                            }
                        }

                        if (namesList != null)
                        {
                            namesList.Add(s);
                        }
                    }
                    catch
                    {
                        wordSkipList.Add(spellCheck.Word);
                    }

                    result.Line = line;
                    if (word == result.Word)
                    {
                        return result;
                    }

                    break;
                case OcrSpellCheck.Action.AllwaysUseSuggestion:
                    try
                    {
                        ocrFixReplaceList.AddWordOrPartial(word, spellCheck.Word);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception + Environment.NewLine + exception.StackTrace);
                        wordSkipList.Add(word);
                    }

                    result.Fixed = true;
                    result.Word = spellCheck.Word;
                    break;
                case OcrSpellCheck.Action.ChangeAndSave:
                    try
                    {
                        ocrFixReplaceList.AddWordOrPartial(word, spellCheck.Word);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception + Environment.NewLine + exception.StackTrace);
                        wordSkipList.Add(word);
                    }

                    result.Fixed = true;
                    result.Word = spellCheck.Word;
                    break;
                case OcrSpellCheck.Action.ChangeOnce:
                    result.Fixed = true;
                    result.Word = spellCheck.Word;
                    break;
                case OcrSpellCheck.Action.ChangeWholeText:
                    result.Line = spellCheck.Paragraph;
                    result.FixedWholeLine = true;
                    break;
                case OcrSpellCheck.Action.ChangeAllWholeText:
                    ocrFixReplaceList.AddToWholeLineList(spellCheck.OriginalWholeText, spellCheck.Paragraph);
                    result.Line = spellCheck.Paragraph;
                    result.FixedWholeLine = true;
                    break;
                case OcrSpellCheck.Action.SkipAll:
                    wordSkipList.Add(spellCheck.Word);
                    wordSkipList.Add(spellCheck.Word.ToUpper());
                    if (spellCheck.Word.Length > 1)
                    {
                        wordSkipList.Add(char.ToUpper(spellCheck.Word[0]) + spellCheck.Word.Substring(1));
                    }

                    break;
                case OcrSpellCheck.Action.SkipOnce:
                    break;
                case OcrSpellCheck.Action.SkipWholeText:
                    result.Line = line;
                    result.FixedWholeLine = true;
                    break;
                case OcrSpellCheck.Action.UseSuggestion:
                    result.Word = spellCheck.Word;
                    result.Fixed = true;
                    break;
            }

            if (result.Fixed)
            {
                result.Line = OcrFixReplaceList.ReplaceWord(line, word, result.Word);
            }

            return result;
        }

        public bool DoSpell(string word)
        {
            return hunspell.Spell(word);
        }

        public List<string> DoSuggest(string word)
        {
            return hunspell.Suggest(word);
        }

        public bool IsWordOrWordsCorrect(string word)
        {
            foreach (string s in word.Split(' ').Where(s => !DoSpell(s)))
            {
                if (IsWordKnownOrNumber(word, word))
                {
                    return true;
                }

                if (s.Length <= 10 || !s.Contains('/'))
                {
                    return false;
                }

                string[] ar = s.Split('/');
                if (ar.Length != 2)
                {
                    return false;
                }

                if (ar[0].Length <= 3 || ar[1].Length <= 3)
                {
                    return false;
                }

                string a = ar[0];
                if (a == a.ToUpper())
                {
                    a = a[0] + a.Substring(1).ToLower();
                }

                string b = ar[0];
                if (b == b.ToUpper())
                {
                    b = b[0] + b.Substring(1).ToLower();
                }

                return (DoSpell(a) || IsWordKnownOrNumber(a, word)) &&
                       (DoSpell(b) || IsWordKnownOrNumber(b, word));
            }

            return true;
        }

        public IEnumerable<string> CreateGuessesFromLetters(string word)
        {
            return ocrFixReplaceList.CreateGuessesFromLetters(word);
        }

        public bool IsWordKnownOrNumber(string word, string line)
        {
            double number;
            if (double.TryParse(word.TrimStart('\'').Replace("$", string.Empty).Replace("£", string.Empty).Replace("¢", string.Empty), out number))
            {
                return true;
            }

            if (wordSkipList.Contains(word))
            {
                return true;
            }

            if (namesEtcList.Contains(word.Trim('\'')))
            {
                return true;
            }

            if (namesEtcListUppercase.Contains(word.Trim('\'')))
            {
                return true;
            }

            if (userWordList.Contains(word.ToLower()))
            {
                return true;
            }

            if (userWordList.Contains(word.Trim('\'').ToLower()))
            {
                return true;
            }

            if (word.Length > 2 && namesEtcListUppercase.Contains(word))
            {
                return true;
            }

            if (word.Length > 2 && namesEtcListWithApostrophe.Contains(word))
            {
                return true;
            }

            return namesList != null && namesList.IsInNamesEtcMultiWordList(line, word);
        }

        public int CountUnknownWordsViaDictionary(string line, out int numberOfCorrectWords)
        {
            numberOfCorrectWords = 0;
            if (hunspell == null)
            {
                return 0;
            }

            int wordsNotFound = 0;
            var words = HtmlUtil.RemoveOpenCloseTags(line, HtmlUtil.TagItalic).Split((Environment.NewLine + " ¡¿,.!?:;()[]{}+-$£\"#&%…“”").ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                if (!IsWordKnownOrNumber(word, line))
                {
                    bool correct = hunspell.Spell(word);
                    if (!correct)
                    {
                        correct = hunspell.Spell(word.Trim('\''));
                    }

                    if (correct)
                    {
                        numberOfCorrectWords++;
                    }
                    else
                    {
                        wordsNotFound++;
                    }
                }

                else if (word.Length > 3)
                {
                    numberOfCorrectWords++;
                }
            }

            return wordsNotFound;
        }

        public void Dispose()
        {
            if (hunspell != null)
            {
                hunspell.Dispose();
                hunspell = null;
            }

            if (spellCheck == null)
            {
                return;
            }

            spellCheck.Dispose();
            spellCheck = null;
        }
    }
}