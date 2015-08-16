namespace Nikse.SubtitleEdit.Logic
{
    using Enums;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;

    public class FindReplaceDialogHelper
    {
        private readonly string findText;
        private readonly string replaceText;
        private readonly Regex regEx;
        private int findTextLenght;

        public bool Success { get; set; }

        public FindType FindType { get; set; }

        public int SelectedIndex { get; set; }

        public int SelectedPosition { get; set; }

        public int WindowPositionLeft { get; set; }

        public int WindowPositionTop { get; set; }

        public int StartLineIndex { get; set; }

        public bool MatchInOriginal { get; set; }

        public FindReplaceDialogHelper(FindType findType, string findText, Regex regEx, string replaceText, int left, int top, int startLineIndex)
        {
            FindType = findType;
            this.findText = findText ?? string.Empty;
            this.replaceText = replaceText ?? string.Empty;
            this.regEx = regEx;
            this.findTextLenght = findText == null ? 0 : findText.Length;
            WindowPositionLeft = left;
            WindowPositionTop = top;
            StartLineIndex = startLineIndex;
        }

        public int FindTextLength
        {
            get
            {
                return findTextLenght;
            }
        }

        public string FindText
        {
            get
            {
                return findText;
            }
        }

        public string ReplaceText
        {
            get
            {
                return replaceText;
            }
        }

        public bool Find(Subtitle subtitle, Subtitle originalSubtitle, int startIndex)
        {
            return FindNext(subtitle, originalSubtitle, startIndex, 0, Configuration.Settings.General.AllowEditOfOriginalSubtitle);
        }

        public bool Find(TextBox textBox, int startIndex)
        {
            return FindNext(textBox, startIndex);
        }

        private int FindPositionInText(string text, int startIndex)
        {
            if (startIndex >= text.Length && 
                !(FindType == FindType.RegEx && 
                startIndex == 0))
            {
                return -1;
            }

            switch (FindType)
            {
                case FindType.Normal:
                    return (text.IndexOf(findText, startIndex, System.StringComparison.OrdinalIgnoreCase));
                case FindType.CaseSensitive:
                    return (text.IndexOf(findText, startIndex, System.StringComparison.Ordinal));
                case FindType.RegEx:
                    {
                        Match match = regEx.Match(text, startIndex);
                        if (match.Success)
                        {
                            string groupName = Utilities.GetRegExGroup(findText);
                            if (groupName != null && match.Groups[groupName] != null && match.Groups[groupName].Success)
                            {
                                findTextLenght = match.Groups[groupName].Length;
                                return match.Groups[groupName].Index;
                            }

                            findTextLenght = match.Length;
                            return match.Index;
                        }

                        return -1;
                    }
            }

            return -1;
        }

        public bool FindNext(Subtitle subtitle, Subtitle originalSubtitle, int startIndex, int position, bool allowEditOfOriginalSubtitle)
        {
            Success = false;
            int index = 0;
            if (position < 0)
            {
                position = 0;
            }

            foreach (Paragraph paragraph in subtitle.Paragraphs)
            {
                if (index >= startIndex)
                {
                    int findPositionInText = 0;
                    if (!MatchInOriginal)
                    {
                        findPositionInText = FindPositionInText(paragraph.Text, position);
                        if (findPositionInText >= 0)
                        {
                            MatchInOriginal = false;
                            SelectedIndex = index;
                            SelectedPosition = findPositionInText;
                            Success = true;
                            return true;
                        }

                        position = 0;
                    }

                    MatchInOriginal = false;

                    if (originalSubtitle != null && allowEditOfOriginalSubtitle)
                    {
                        Paragraph originalParagraph = Utilities.GetOriginalParagraph(index, paragraph, originalSubtitle.Paragraphs);
                        if (originalParagraph != null)
                        {
                            findPositionInText = FindPositionInText(originalParagraph.Text, position);
                            if (findPositionInText >= 0)
                            {
                                MatchInOriginal = true;
                                SelectedIndex = index;
                                SelectedPosition = findPositionInText;
                                Success = true;
                                return true;
                            }
                        }
                    }
                }

                index++;
            }

            return false;
        }

        public static ContextMenu GetRegExContextMenu(TextBox textBox)
        {
            var contextMenu = new ContextMenu();
            var regularExpressionContextMenu = Configuration.Settings.Language.RegularExpressionContextMenu;
            contextMenu.MenuItems.Add(regularExpressionContextMenu.WordBoundary, delegate
            {
                textBox.SelectedText = "\\b";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NonWordBoundary, delegate
            {
                textBox.SelectedText = "\\B";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NewLine, delegate
            {
                textBox.SelectedText = "\\r\\n";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.AnyDigit, delegate
            {
                textBox.SelectedText = "\\d";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NonDigit, delegate
            {
                textBox.SelectedText = "\\D";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.AnyCharacter, delegate
            {
                textBox.SelectedText = ".";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.AnyWhitespace, delegate
            {
                textBox.SelectedText = "\\s";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NonSpaceCharacter, delegate
            {
                textBox.SelectedText = "\\S";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.ZeroOrMore, delegate
            {
                textBox.SelectedText = "*";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.OneOrMore, delegate
            {
                textBox.SelectedText = "+";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.InCharacterGroup, delegate
            {
                textBox.SelectedText = "[test]";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NotInCharacterGroup, delegate
            {
                textBox.SelectedText = "[^test]";
            });

            return contextMenu;
        }

        public static ContextMenu GetRegExContextMenu(ComboBox comboBox)
        {
            var contextMenu = new ContextMenu();
            var regularExpressionContextMenu = Configuration.Settings.Language.RegularExpressionContextMenu;
            contextMenu.MenuItems.Add(regularExpressionContextMenu.WordBoundary, delegate
            {
                comboBox.SelectedText = "\\b";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NonWordBoundary, delegate
            {
                comboBox.SelectedText = "\\B";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NewLine, delegate
            {
                comboBox.SelectedText = "\\r\\n";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.AnyDigit, delegate
            {
                comboBox.SelectedText = "\\d";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NonDigit, delegate
            {
                comboBox.SelectedText = "\\D";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.AnyCharacter, delegate
            {
                comboBox.SelectedText = ".";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.AnyWhitespace, delegate
            {
                comboBox.SelectedText = "\\s";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NonSpaceCharacter, delegate
            {
                comboBox.SelectedText = "\\S";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.ZeroOrMore, delegate
            {
                comboBox.SelectedText = "*";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.OneOrMore, delegate
            {
                comboBox.SelectedText = "+";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.InCharacterGroup, delegate
            {
                comboBox.SelectedText = "[test]";
            });

            contextMenu.MenuItems.Add(regularExpressionContextMenu.NotInCharacterGroup, delegate
            {
                comboBox.SelectedText = "[^test]";
            });

            return contextMenu;
        }

        public static ContextMenu GetReplaceTextContextMenu(TextBox textBox)
        {
            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(Configuration.Settings.Language.RegularExpressionContextMenu.NewLineShort, delegate
            {
                textBox.SelectedText = "\\n";
            });

            return contextMenu;
        }

        public bool FindNext(TextBox textBox, int startIndex)
        {
            Success = false;
            startIndex++;
            if (startIndex < textBox.Text.Length)
            {
                if (FindType == FindType.RegEx)
                {
                    Match match = regEx.Match(textBox.Text, startIndex);
                    if (match.Success)
                    {
                        string groupName = Utilities.GetRegExGroup(findText);
                        if (groupName != null && 
                            match.Groups[groupName] != null && 
                            match.Groups[groupName].Success)
                        {
                            findTextLenght = match.Groups[groupName].Length;
                            SelectedIndex = match.Groups[groupName].Index;
                        }
                        else
                        {
                            findTextLenght = match.Length;
                            SelectedIndex = match.Index;
                        }

                        Success = true;
                    }

                    return match.Success;
                }

                string searchText = textBox.Text.Substring(startIndex);
                int pos = FindPositionInText(searchText, 0);
                if (pos >= 0)
                {
                    SelectedIndex = pos + startIndex;
                    return true;
                }
            }

            return false;
        }
    }
}
