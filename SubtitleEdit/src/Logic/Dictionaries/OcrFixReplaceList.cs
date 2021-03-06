﻿namespace Nikse.SubtitleEdit.Logic.Dictionaries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using System.Xml;
    using Core;

    public class OcrFixReplaceList
    {
        private static readonly Regex RegExQuestion = new Regex(@"\S\?[A-ZÆØÅÄÖÉÈÀÙÂÊÎÔÛËÏa-zæøåäöéèàùâêîôûëï]", RegexOptions.Compiled);
        private static readonly Regex RegExIandZero = new Regex(@"[a-zæøåöääöéèàùâêîôûëï][I1]", RegexOptions.Compiled);
        private static readonly Regex RegExTime1 = new Regex(@"[a-zæøåöääöéèàùâêîôûëï]0", RegexOptions.Compiled);
        private static readonly Regex RegExTime2 = new Regex(@"0[a-zæøåöääöéèàùâêîôûëï]", RegexOptions.Compiled);
        private static readonly Regex HexNumber = new Regex(@"^#?[\dABDEFabcdef]+$", RegexOptions.Compiled);
        private static readonly Regex StartEndEndsWithNumber = new Regex(@"^\d+.+\d$", RegexOptions.Compiled);

        public Dictionary<string, string> WordReplaceList;
        public Dictionary<string, string> PartialLineWordBoundaryReplaceList;
        private readonly Dictionary<string, string> partialLineAlwaysReplaceList;
        private readonly Dictionary<string, string> beginLineReplaceList;
        private readonly Dictionary<string, string> endLineReplaceList;
        private readonly Dictionary<string, string> wholeLineReplaceList;
        private readonly Dictionary<string, string> partialWordReplaceListAlways;
        private readonly Dictionary<string, string> partialWordReplaceList;
        private readonly Dictionary<string, string> regExList;
        private readonly string replaceListXmlFileName;

        public OcrFixReplaceList(string replaceListXmlFileName)
        {
            this.replaceListXmlFileName = replaceListXmlFileName;
            WordReplaceList = new Dictionary<string, string>();
            PartialLineWordBoundaryReplaceList = new Dictionary<string, string>();
            partialLineAlwaysReplaceList = new Dictionary<string, string>();
            beginLineReplaceList = new Dictionary<string, string>();
            endLineReplaceList = new Dictionary<string, string>();
            wholeLineReplaceList = new Dictionary<string, string>();
            partialWordReplaceListAlways = new Dictionary<string, string>();
            partialWordReplaceList = new Dictionary<string, string>();
            regExList = new Dictionary<string, string>();

            var doc = LoadXmlReplaceListDocument();
            var userDoc = LoadXmlReplaceListUserDocument();

            WordReplaceList = LoadReplaceList(doc, "WholeWords");
            partialWordReplaceListAlways = LoadReplaceList(doc, "PartialWordsAlways");
            partialWordReplaceList = LoadReplaceList(doc, "PartialWords");
            PartialLineWordBoundaryReplaceList = LoadReplaceList(doc, "PartialLines");
            partialLineAlwaysReplaceList = LoadReplaceList(doc, "PartialAlwaysLines");
            beginLineReplaceList = LoadReplaceList(doc, "BeginLines");
            endLineReplaceList = LoadReplaceList(doc, "EndLines");
            wholeLineReplaceList = LoadReplaceList(doc, "WholeLines");
            regExList = LoadRegExList(doc, "RegularExpressions");

            foreach (var kp in LoadReplaceList(userDoc, "RemovedWholeWords"))
            {
                if (WordReplaceList.ContainsKey(kp.Key))
                {
                    WordReplaceList.Remove(kp.Key);
                }
            }
            foreach (var kp in LoadReplaceList(userDoc, "WholeWords"))
            {
                if (!WordReplaceList.ContainsKey(kp.Key))
                {
                    WordReplaceList.Add(kp.Key, kp.Value);
                }
            }

            foreach (var kp in LoadReplaceList(userDoc, "RemovedPartialLines"))
            {
                if (PartialLineWordBoundaryReplaceList.ContainsKey(kp.Key))
                {
                    PartialLineWordBoundaryReplaceList.Remove(kp.Key);
                }
            }
            foreach (var kp in LoadReplaceList(userDoc, "PartialLines"))
            {
                if (!PartialLineWordBoundaryReplaceList.ContainsKey(kp.Key))
                {
                    PartialLineWordBoundaryReplaceList.Add(kp.Key, kp.Value);
                }
            }
        }

        public static OcrFixReplaceList FromLanguageId(string languageId)
        {
            return new OcrFixReplaceList(Configuration.DictionariesFolder + languageId + "_OCRFixReplaceList.xml");
        }

        private static Dictionary<string, string> LoadReplaceList(XmlDocument doc, string name)
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

        private static Dictionary<string, string> LoadRegExList(XmlDocument doc, string name)
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

        public string FixOcrErrorViaLineReplaceList(string input)
        {
            // Whole fromLine
            foreach (string from in wholeLineReplaceList.Keys)
            {
                if (input == from)
                {
                    return wholeLineReplaceList[from];
                }
            }

            string newText = input;
            string pre = string.Empty;
            if (newText.StartsWith("<i>", StringComparison.Ordinal))
            {
                pre += "<i>";
                newText = newText.Remove(0, 3);
            }
            while (newText.Length > 1 && @" -""['¶(".Contains(newText[0]))
            {
                pre += newText[0];
                newText = newText.Substring(1);
            }

            if (newText.StartsWith("<i>", StringComparison.Ordinal))
            {
                pre += "<i>";
                newText = newText.Remove(0, 3);
            }

            // begin fromLine
            var lines = newText.SplitToLines();
            var sb = new StringBuilder();
            foreach (string l in lines)
            {
                string s = l;
                foreach (string from in beginLineReplaceList.Keys)
                {
                    if (!s.Contains(@from))
                    {
                        continue;
                    }

                    if (s.StartsWith(@from))
                    {
                        s = s.Remove(0, @from.Length).Insert(0, beginLineReplaceList[@from]);
                    }

                    if (s.Contains(". " + @from))
                    {
                        s = s.Replace(". " + @from, ". " + beginLineReplaceList[@from]);
                    }

                    if (s.Contains("! " + @from))
                    {
                        s = s.Replace("! " + @from, "! " + beginLineReplaceList[@from]);
                    }

                    if (s.Contains("? " + @from))
                    {
                        s = s.Replace("? " + @from, "? " + beginLineReplaceList[@from]);
                    }

                    if (s.Contains("." + Environment.NewLine + @from))
                    {
                        s = s.Replace(". " + Environment.NewLine + @from, ". " + Environment.NewLine + beginLineReplaceList[@from]);
                    }

                    if (s.Contains("! " + Environment.NewLine + @from))
                    {
                        s = s.Replace("! " + Environment.NewLine + @from, "! " + Environment.NewLine + beginLineReplaceList[@from]);
                    }

                    if (s.Contains("? " + Environment.NewLine + @from))
                    {
                        s = s.Replace("? " + Environment.NewLine + @from, "? " + Environment.NewLine + beginLineReplaceList[@from]);
                    }

                    if (s.StartsWith('"') && !@from.StartsWith('"') && s.StartsWith("\"" + @from))
                    {
                        s = s.Replace("\"" + @from, "\"" + beginLineReplaceList[@from]);
                    }
                }

                sb.AppendLine(s);
            }

            newText = sb.ToString().TrimEnd('\r').TrimEnd('\n').TrimEnd('\r').TrimEnd('\n');
            newText = pre + newText;

            string post = string.Empty;
            if (newText.EndsWith("</i>", StringComparison.Ordinal))
            {
                newText = newText.Remove(newText.Length - 4, 4);
                post = "</i>";
            }

            foreach (string from in endLineReplaceList.Keys)
            {
                if (!newText.EndsWith(@from, StringComparison.Ordinal))
                {
                    continue;
                }

                int position = (newText.Length - @from.Length);
                newText = newText.Remove(position).Insert(position, endLineReplaceList[@from]);
            }

            newText += post;

            foreach (string from in PartialLineWordBoundaryReplaceList.Keys)
            {
                if (newText.FastIndexOf(from) >= 0)
                {
                    newText = ReplaceWord(newText, from, PartialLineWordBoundaryReplaceList[from]);
                }
            }

            foreach (string from in partialLineAlwaysReplaceList.Keys)
            {
                if (newText.FastIndexOf(from) >= 0)
                {
                    newText = newText.Replace(from, partialLineAlwaysReplaceList[from]);
                }
            }

            foreach (string findWhat in regExList.Keys)
            {
                newText = Regex.Replace(newText, findWhat, regExList[findWhat], RegexOptions.Multiline);
            }

            return newText;
        }

        private static string AddToGuessList(List<string> list, string word, int index, string letter, string replaceLetters)
        {
            if (string.IsNullOrEmpty(word) || index < 0 || index + letter.Length - 1 >= word.Length)
            {
                return word;
            }

            string s = word.Remove(index, letter.Length);
            if (index >= s.Length)
            {
                s += replaceLetters;
            }
            else
            {
                s = s.Insert(index, replaceLetters);
            }

            if (!list.Contains(s))
            {
                list.Add(s);
            }

            return s;
        }

        public IEnumerable<string> CreateGuessesFromLetters(string word)
        {
            var list = new List<string>();
            foreach (string letter in partialWordReplaceList.Keys)
            {
                string s = word;
                int i = 0;
                while (s.Contains(letter) && i < 10)
                {
                    int index = s.FastIndexOf(letter);
                    s = AddToGuessList(list, s, index, letter, partialWordReplaceList[letter]);
                    AddToGuessList(list, word, index, letter, partialWordReplaceList[letter]);
                    i++;
                }

                s = word;
                i = 0;
                while (s.Contains(letter) && i < 10)
                {
                    int index = s.LastIndexOf(letter, StringComparison.Ordinal);
                    s = AddToGuessList(list, s, index, letter, partialWordReplaceList[letter]);
                    AddToGuessList(list, word, index, letter, partialWordReplaceList[letter]);
                    i++;
                }
            }

            return list;
        }

        public string FixCommonWordErrors(string word)
        {
            if (Configuration.Settings.Tools.OcrFixUseHardcodedRules)
            {
                word = word.Replace("ﬁ", "fi");
                word = word.Replace("ν", "v"); // NOTE: first 'v' is a special unicode character!!!!

                if (word.Contains('’'))
                {
                    word = word.Replace('’', '\'');
                }

                if (word.Contains('`'))
                {
                    word = word.Replace('`', '\'');
                }

                if (word.Contains('‘'))
                {
                    word = word.Replace('‘', '\'');
                }

                if (word.Contains('—'))
                {
                    word = word.Replace('—', '-');
                }

                while (word.Contains("--"))
                {
                    word = word.Replace("--", "-");
                }

                if (word.Contains('|'))
                {
                    word = word.Replace('|', 'l');
                }

                if (word.Contains("vx/"))
                {
                    word = word.Replace("vx/", "w");
                }

                if (word.Contains('¤'))
                {
                    if (Regex.IsMatch(word, "[A-ZÆØÅÄÖÉÈÀÙÂÊÎÔÛËÏa-zæøåäöéèàùâêîôûëï]¤"))
                    {
                        word = word.Replace('¤', 'o');
                    }
                }
            }

            //always replace list
            word = partialWordReplaceListAlways.Keys.Aggregate(word, (current, letter) => current.Replace(letter, partialWordReplaceListAlways[letter]));

            string pre = string.Empty;
            string post = string.Empty;

            if (word.StartsWith("<i>", StringComparison.Ordinal))
            {
                pre += "<i>";
                word = word.Remove(0, 3);
            }

            while (word.Length > 2 && word.StartsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                pre += Environment.NewLine;
                word = word.Substring(2);
            }

            while (word.Length > 1 && word[0] == '-')
            {
                pre += "-";
                word = word.Substring(1);
            }

            while (word.Length > 1 && word[0] == '.')
            {
                pre += ".";
                word = word.Substring(1);
            }

            while (word.Length > 1 && word[0] == '"')
            {
                pre += "\"";
                word = word.Substring(1);
            }

            if (word.Length > 1 && word[0] == '(')
            {
                pre += "(";
                word = word.Substring(1);
            }

            if (word.StartsWith("<i>", StringComparison.Ordinal))
            {
                pre += "<i>";
                word = word.Remove(0, 3);
            }

            while (word.Length > 2 && word.EndsWith(Environment.NewLine))
            {
                post += Environment.NewLine;
                word = word.Substring(0, word.Length - 2);
            }

            while (word.Length > 1 && word.EndsWith('"'))
            {
                post = post + "\"";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.Length > 1 && word.EndsWith('.'))
            {
                post = post + ".";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith(',') && word.Length > 1)
            {
                post = post + ",";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith('?') && word.Length > 1)
            {
                post = post + "?";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith('!') && word.Length > 1)
            {
                post = post + "!";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith(')') && word.Length > 1)
            {
                post = post + ")";
                word = word.Substring(0, word.Length - 1);
            }

            if (word.EndsWith("</i>", StringComparison.Ordinal))
            {
                post = post + "</i>";
                word = word.Remove(word.Length - 4, 4);
            }

            string preWordPost = pre + word + post;
            if (word.Length == 0)
            {
                return preWordPost;
            }

            if (word.Contains('?'))
            {
                var match = RegExQuestion.Match(word);
                if (match.Success)
                {
                    word = word.Insert(match.Index + 2, " ");
                }
            }

            foreach (string from in WordReplaceList.Keys)
            {
                if (word.Length == from.Length)
                {
                    if (word == from)
                    {
                        return pre + WordReplaceList[from] + post;
                    }
                }
                else if (word.Length + post.Length == from.Length)
                {
                    if (string.CompareOrdinal(word + post, from) == 0)
                    {
                        return pre + WordReplaceList[from];
                    }
                }

                if (pre.Length + word.Length + post.Length == from.Length && string.CompareOrdinal(preWordPost, from) == 0)
                {
                    return WordReplaceList[from];
                }
            }

            if (Configuration.Settings.Tools.OcrFixUseHardcodedRules)
            {
                // uppercase I or 1 inside lowercase fromWord (will be replaced by lowercase L)
                word = FixIor1InsideLowerCaseWord(word);

                // uppercase 0 inside lowercase fromWord (will be replaced by lowercase L)
                word = Fix0InsideLowerCaseWord(word);

                // uppercase I or 1 inside lowercase fromWord (will be replaced by lowercase L)
                word = FixIor1InsideLowerCaseWord(word);

                word = FixLowerCaseLInsideUpperCaseWord(word); // eg. SCARLETTl => SCARLETTI
            }

            // Retry fromWord replace list
            foreach (string from in WordReplaceList.Keys)
            {
                if (word.Length == from.Length)
                {
                    if (string.CompareOrdinal(word, from) == 0)
                    {
                        return pre + WordReplaceList[from] + post;
                    }
                }
                else if (word.Length + post.Length == from.Length)
                {
                    if (string.CompareOrdinal(word + post, from) == 0)
                    {
                        return pre + WordReplaceList[from];
                    }
                }

                if (pre.Length + word.Length + post.Length == from.Length && string.CompareOrdinal(preWordPost, from) == 0)
                {
                    return WordReplaceList[from];
                }
            }

            return preWordPost;
        }

        public static string FixLowerCaseLInsideUpperCaseWord(string word)
        {
            if (word.Length <= 3 || word.Replace("l", string.Empty).ToUpper() != word.Replace("l", string.Empty))
            {
                return word;
            }

            if (!word.Contains('<') && !word.Contains('>') && !word.Contains('\''))
            {
                word = word.Replace('l', 'I');
            }

            return word;
        }

        public static string FixIor1InsideLowerCaseWord(string word)
        {
            if (StartEndEndsWithNumber.IsMatch(word))
            {
                return word;
            }

            if (word.Contains('2') ||
                word.Contains('3') ||
                word.Contains('4') ||
                word.Contains('5') ||
                word.Contains('6') ||
                word.Contains('7') ||
                word.Contains('8') ||
                word.Contains('9'))
            {
                return word;
            }

            if (HexNumber.IsMatch(word))
            {
                return word;
            }

            if (word.LastIndexOf('I') <= 0 && word.LastIndexOf('1') <= 0)
            {
                return word;
            }

            var match = RegExIandZero.Match(word);
            while (match.Success)
            {
                if (word[match.Index + 1] == 'I' || word[match.Index + 1] == '1')
                {
                    bool doFix = word[match.Index + 1] != 'I' && match.Index >= 1 && word.Substring(match.Index - 1).StartsWith("Mc");
                    if (word[match.Index + 1] == 'I' && match.Index >= 2 && word.Substring(match.Index - 2).StartsWith("Mac"))
                    {
                        doFix = false;
                    }

                    if (doFix)
                    {
                        string oldText = word;
                        word = word.Substring(0, match.Index + 1) + "l";
                        if (match.Index + 2 < oldText.Length)
                        {
                            word += oldText.Substring(match.Index + 2);
                        }
                    }
                }

                match = RegExIandZero.Match(word, match.Index + 1);
            }

            return word;
        }

        public static string Fix0InsideLowerCaseWord(string word)
        {
            if (StartEndEndsWithNumber.IsMatch(word))
            {
                return word;
            }

            if (word.Contains('1') ||
                word.Contains('2') ||
                word.Contains('3') ||
                word.Contains('4') ||
                word.Contains('5') ||
                word.Contains('6') ||
                word.Contains('7') ||
                word.Contains('8') ||
                word.Contains('9') ||
                word.EndsWith("a.m", StringComparison.Ordinal) ||
                word.EndsWith("p.m", StringComparison.Ordinal) ||
                word.EndsWith("am", StringComparison.Ordinal) ||
                word.EndsWith("pm", StringComparison.Ordinal))
            {
                return word;
            }

            if (HexNumber.IsMatch(word))
            {
                return word;
            }

            if (word.LastIndexOf('0') <= 0)
            {
                return word;
            }

            Match match = RegExTime1.Match(word);
            while (match.Success)
            {
                if (word[match.Index + 1] == '0')
                {
                    string oldText = word;
                    word = word.Substring(0, match.Index + 1) + "o";
                    if (match.Index + 2 < oldText.Length)
                    {
                        word += oldText.Substring(match.Index + 2);
                    }
                }

                match = RegExTime1.Match(word);
            }

            match = RegExTime2.Match(word);
            while (match.Success)
            {
                if (word[match.Index] == '0')
                {
                    if (match.Index == 0 || !@"123456789".Contains(word[match.Index - 1]))
                    {
                        string oldText = word;
                        word = word.Substring(0, match.Index) + "o";
                        if (match.Index + 1 < oldText.Length)
                        {
                            word += oldText.Substring(match.Index + 1);
                        }
                    }
                }

                match = RegExTime2.Match(word, match.Index + 1);
            }

            return word;
        }

        public string FixCommonWordErrorsQuick(string word)
        {
            //always replace list
            word = partialWordReplaceListAlways.Keys.Aggregate(word, (current, letter) => current.Replace(letter, partialWordReplaceListAlways[letter]));

            string pre = string.Empty;
            string post = string.Empty;

            if (word.StartsWith("<i>", StringComparison.Ordinal))
            {
                pre += "<i>";
                word = word.Remove(0, 3);
            }

            while (word.StartsWith(Environment.NewLine) && word.Length > 2)
            {
                pre += Environment.NewLine;
                word = word.Substring(2);
            }

            while (word.Length > 1 && word[0] == '-')
            {
                pre += "-";
                word = word.Substring(1);
            }

            while (word.Length > 1 && word[0] == '.')
            {
                pre += ".";
                word = word.Substring(1);
            }

            while (word.Length > 1 && word[0] == '"')
            {
                pre += "\"";
                word = word.Substring(1);
            }

            if (word.Length > 1 && word[0] == '(')
            {
                pre += "(";
                word = word.Substring(1);
            }

            if (word.StartsWith("<i>", StringComparison.Ordinal))
            {
                pre += "<i>";
                word = word.Remove(0, 3);
            }

            while (word.EndsWith(Environment.NewLine) && word.Length > 2)
            {
                post += Environment.NewLine;
                word = word.Substring(0, word.Length - 2);
            }

            while (word.EndsWith('"') && word.Length > 1)
            {
                post = post + "\"";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith('.') && word.Length > 1)
            {
                post = post + ".";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith(',') && word.Length > 1)
            {
                post = post + ",";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith('?') && word.Length > 1)
            {
                post = post + "?";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith('!') && word.Length > 1)
            {
                post = post + "!";
                word = word.Substring(0, word.Length - 1);
            }

            while (word.EndsWith(')') && word.Length > 1)
            {
                post = post + ")";
                word = word.Substring(0, word.Length - 1);
            }

            if (word.EndsWith("</i>", StringComparison.Ordinal))
            {
                post = post + "</i>";
                word = word.Remove(word.Length - 4, 4);
            }

            string preWordPost = pre + word + post;
            if (word.Length == 0)
            {
                return preWordPost;
            }

            foreach (string from in WordReplaceList.Keys)
            {
                if (word.Length == from.Length)
                {
                    if (string.CompareOrdinal(word, from) == 0)
                    {
                        return pre + WordReplaceList[from] + post;
                    }
                }
                else if (word.Length + post.Length == from.Length)
                {
                    if (string.CompareOrdinal(word + post, from) == 0)
                    {
                        return pre + WordReplaceList[from];
                    }
                }

                if (pre.Length + word.Length + post.Length == from.Length && string.CompareOrdinal(preWordPost, from) == 0)
                {
                    return WordReplaceList[from];
                }
            }

            return preWordPost;
        }

        public bool RemoveWordOrPartial(string word)
        {
            if (word.Contains(' '))
            {
                if (!DeletePartialLineFromWordList(word))
                {
                    return false;
                }

                if (PartialLineWordBoundaryReplaceList.ContainsKey(word))
                {
                    PartialLineWordBoundaryReplaceList.Remove(word);
                }

                return true;
            }

            if (!DeleteWordFromWordList(word))
            {
                return false;
            }

            if (WordReplaceList.ContainsKey(word))
            {
                WordReplaceList.Remove(word);
            }

            return true;
        }

        private bool DeleteWordFromWordList(string fromWord)
        {
            const string replaceListName = "WholeWords";

            var doc = LoadXmlReplaceListDocument();
            var list = LoadReplaceList(doc, replaceListName);

            var userDoc = LoadXmlReplaceListUserDocument();
            var userList = LoadReplaceList(userDoc, replaceListName);

            return DeleteFromList(fromWord, userDoc, replaceListName, "Word", list, userList);
        }

        private bool DeletePartialLineFromWordList(string fromWord)
        {
            const string replaceListName = "PartialLines";

            var doc = LoadXmlReplaceListDocument();
            var list = LoadReplaceList(doc, replaceListName);

            var userDoc = LoadXmlReplaceListUserDocument();
            var userList = LoadReplaceList(userDoc, replaceListName);

            return DeleteFromList(fromWord, userDoc, replaceListName, "LinePart", list, userList);
        }

        private bool DeleteFromList(string word, XmlDocument userDoc, string replaceListName, string elementName, Dictionary<string, string> dictionary, Dictionary<string, string> userDictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            if (userDictionary == null)
            {
                throw new ArgumentNullException("userDictionary");
            }

            bool removed = false;
            if (userDictionary.ContainsKey((word)))
            {
                userDictionary.Remove(word);
                if (userDoc.DocumentElement != null)
                {
                    XmlNode wholeWordsNode = userDoc.DocumentElement.SelectSingleNode(replaceListName);
                    if (wholeWordsNode != null)
                    {
                        wholeWordsNode.RemoveAll();
                        foreach (var kvp in userDictionary)
                        {
                            XmlNode newNode = userDoc.CreateNode(XmlNodeType.Element, elementName, null);
                            XmlAttribute aFrom = userDoc.CreateAttribute("from");
                            XmlAttribute aTo = userDoc.CreateAttribute("to");
                            aFrom.InnerText = kvp.Key;
                            aTo.InnerText = kvp.Value;
                            if (newNode.Attributes != null)
                            {
                                newNode.Attributes.Append(aTo);
                                newNode.Attributes.Append(aFrom);
                            }

                            wholeWordsNode.AppendChild(newNode);
                        }

                        userDoc.Save(ReplaceListXmlFileNameUser);
                        removed = true;
                    }
                }
            }

            if (!dictionary.ContainsKey((word)))
            {
                return removed;
            }

            if (userDoc.DocumentElement != null)
            {
                XmlNode wholeWordsNode = userDoc.DocumentElement.SelectSingleNode("Removed" + replaceListName);
                if (wholeWordsNode == null)
                {
                    return removed;
                }

                XmlNode newNode = userDoc.CreateNode(XmlNodeType.Element, elementName, null);
                XmlAttribute aFrom = userDoc.CreateAttribute("from");
                XmlAttribute aTo = userDoc.CreateAttribute("to");
                aFrom.InnerText = word;
                aTo.InnerText = string.Empty;
                if (newNode.Attributes != null)
                {
                    newNode.Attributes.Append(aTo);
                    newNode.Attributes.Append(aFrom);
                }

                wholeWordsNode.AppendChild(newNode);
                userDoc.Save(ReplaceListXmlFileNameUser);
                removed = true;
            }

            return removed;
        }

        private XmlDocument LoadXmlReplaceListDocument()
        {
            const string xmlText = "<ReplaceList><WholeWords/><PartialLines/><BeginLines/><EndLines/><WholeLines/></ReplaceList>";
            var doc = new XmlDocument();
            if (File.Exists(replaceListXmlFileName))
            {
                try
                {
                    doc.Load(replaceListXmlFileName);
                }
                catch
                {
                    doc.LoadXml(xmlText);
                }
            }
            else
            {
                doc.LoadXml(xmlText);
            }
            return doc;
        }

        private string ReplaceListXmlFileNameUser
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(replaceListXmlFileName), Path.GetFileNameWithoutExtension(replaceListXmlFileName) + "_User" + Path.GetExtension(replaceListXmlFileName));
            }
        }

        private XmlDocument LoadXmlReplaceListUserDocument()
        {
            const string xmlText = "<ReplaceList><WholeWords/><PartialLines/><BeginLines/><EndLines/><WholeLines/><RemovedWholeWords/><RemovedPartialLines/><RemovedBeginLines/><RemovedEndLines/><RemovedWholeLines/></ReplaceList>";
            var doc = new XmlDocument();
            if (File.Exists(ReplaceListXmlFileNameUser))
            {
                try
                {
                    doc.Load(ReplaceListXmlFileNameUser);
                }
                catch
                {
                    doc.LoadXml(xmlText);
                }
            }
            else
            {
                doc.LoadXml(xmlText);
            }

            return doc;
        }

        public bool AddWordOrPartial(string fromWord, string toWord)
        {
            if (fromWord.Contains(' '))
            {
                if (!SavePartialLineToWordList(fromWord, toWord))
                {
                    return false;
                }

                if (!PartialLineWordBoundaryReplaceList.ContainsKey(fromWord))
                {
                    PartialLineWordBoundaryReplaceList.Add(fromWord, toWord);
                }

                return true;
            }

            if (!SaveWordToWordList(fromWord, toWord))
            {
                return false;
            }

            if (!WordReplaceList.ContainsKey(fromWord))
            {
                WordReplaceList.Add(fromWord, toWord);
            }

            return true;
        }

        private bool SaveWordToWordList(string fromWord, string toWord)
        {
            const string replaceListName = "WholeWords";

            var doc = LoadXmlReplaceListDocument();
            var list = LoadReplaceList(doc, replaceListName);

            var userDoc = LoadXmlReplaceListUserDocument();
            var userList = LoadReplaceList(userDoc, replaceListName);

            return SaveToList(fromWord, toWord, userDoc, replaceListName, "Word", list, userList);
        }

        private bool SavePartialLineToWordList(string fromWord, string toWord)
        {
            const string replaceListName = "PartialLines";

            var doc = LoadXmlReplaceListDocument();
            var list = LoadReplaceList(doc, replaceListName);

            var userDoc = LoadXmlReplaceListUserDocument();
            var userList = LoadReplaceList(userDoc, replaceListName);

            return SaveToList(fromWord, toWord, userDoc, replaceListName, "LinePart", list, userList);
        }

        private bool SaveToList(string fromWord, string toWord, XmlDocument userDoc, string replaceListName, string elementName, Dictionary<string, string> dictionary, Dictionary<string, string> userDictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            if (userDictionary == null)
            {
                throw new ArgumentNullException("userDictionary");
            }

            if (userDictionary.ContainsKey(fromWord))
            {
                return false;
            }

            userDictionary.Add(fromWord, toWord);
            if (userDoc.DocumentElement == null)
            {
                return true;
            }

            XmlNode wholeWordsNode = userDoc.DocumentElement.SelectSingleNode(replaceListName);
            if (wholeWordsNode != null)
            {
                XmlNode newNode = userDoc.CreateNode(XmlNodeType.Element, elementName, null);
                XmlAttribute aFrom = userDoc.CreateAttribute("from");
                XmlAttribute aTo = userDoc.CreateAttribute("to");
                aTo.InnerText = toWord;
                aFrom.InnerText = fromWord;
                if (newNode.Attributes != null)
                {
                    newNode.Attributes.Append(aFrom);
                    newNode.Attributes.Append(aTo);
                }

                wholeWordsNode.AppendChild(newNode);
                userDoc.Save(ReplaceListXmlFileNameUser);
            }

            return true;
        }

        public void AddToWholeLineList(string fromLine, string toLine)
        {
            try
            {
                var userDocument = LoadXmlReplaceListUserDocument();
                if (!wholeLineReplaceList.ContainsKey(fromLine))
                {
                    wholeLineReplaceList.Add(fromLine, toLine);
                }

                if (userDocument.DocumentElement == null)
                {
                    return;
                }

                XmlNode wholeWordsNode = userDocument.DocumentElement.SelectSingleNode("WholeLines");
                if (wholeWordsNode == null)
                {
                    return;
                }

                XmlNode newNode = userDocument.CreateNode(XmlNodeType.Element, "Line", null);
                XmlAttribute aFrom = userDocument.CreateAttribute("from");
                XmlAttribute aTo = userDocument.CreateAttribute("to");
                aTo.InnerText = toLine;
                aFrom.InnerText = fromLine;
                if (newNode.Attributes != null)
                {
                    newNode.Attributes.Append(aFrom);
                    newNode.Attributes.Append(aTo);
                }

                wholeWordsNode.AppendChild(newNode);
                userDocument.Save(replaceListXmlFileName);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception + Environment.NewLine + exception.StackTrace);
            }
        }

        public static string ReplaceWord(string text, string word, string newWord)
        {
            var sb = new StringBuilder();
            if (word == null || text == null || !text.Contains(word))
            {
                return sb.ToString();
            }

            const string startChars = @" ¡¿<>-""”“()[]'‘`´¶♪¿¡.…—!?,:;/";
            int appendFrom = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text.Substring(i).StartsWith(word) && i >= appendFrom)
                {
                    bool startOk = i == 0;
                    if (!startOk)
                    {
                        startOk = (startChars + Environment.NewLine).Contains(text[i - 1]);
                    }

                    if (!startOk && word.StartsWith(' '))
                    {
                        startOk = true;
                    }

                    if (startOk)
                    {
                        bool endOk = (i + word.Length == text.Length);
                        if (!endOk)
                        {
                            endOk = (startChars + Environment.NewLine).Contains(text[i + word.Length]);
                        }

                        if (!endOk)
                        {
                            endOk = newWord.EndsWith(' ');
                        }

                        if (endOk)
                        {
                            sb.Append(newWord);
                            appendFrom = i + word.Length;
                        }
                    }
                }

                if (i >= appendFrom)
                {
                    sb.Append(text[i]);
                }
            }

            return sb.ToString();
        }
    }
}