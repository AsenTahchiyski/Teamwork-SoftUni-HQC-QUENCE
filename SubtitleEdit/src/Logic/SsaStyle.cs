namespace Nikse.SubtitleEdit.Logic
{
    using System;
    using System.Drawing;
    using System.Text;
    using SubtitleFormats;

    public class SsaStyle
    {
        public string Name { get; set; }
        
        public string FontName { get; set; }
        
        public int FontSize { get; set; }
        
        public bool Italic { get; set; }
        
        public bool Bold { get; set; }
        
        public bool Underline { get; set; }
        
        public Color Primary { get; set; }
        
        public Color Secondary { get; set; }
        
        public Color Tertiary { get; set; }
        
        public Color Outline { get; set; }
        
        public Color Background { get; set; }
        
        public int ShadowWidth { get; set; }
        
        public int OutlineWidth { get; set; }
        
        public string Alignment { get; set; }
        
        public int MarginLeft { get; set; }
        
        public int MarginRight { get; set; }
        
        public int MarginVertical { get; set; }
        
        public string BorderStyle { get; set; }
        
        public string RawLine { get; set; }
        
        public bool LoadedFromHeader { get; set; }

        public SsaStyle()
        {
            FontName = Configuration.Settings.SubtitleSettings.SsaFontName;
            FontSize = (int)Configuration.Settings.SubtitleSettings.SsaFontSize;
            Primary = Color.FromArgb(Configuration.Settings.SubtitleSettings.SsaFontColorArgb);
            Secondary = Color.Yellow;
            Outline = Color.Black;
            Background = Color.Black;
            Alignment = "2";
            OutlineWidth = Configuration.Settings.SubtitleSettings.SsaOutline;
            ShadowWidth = Configuration.Settings.SubtitleSettings.SsaShadow;
            MarginLeft = 10;
            MarginRight = 10;
            MarginVertical = 10;
            BorderStyle = "1";
            if (Configuration.Settings.SubtitleSettings.SsaOpaqueBox)
            {
                BorderStyle = "3";
            }

            RawLine = string.Empty;
            LoadedFromHeader = false;
        }

        public SsaStyle(SsaStyle ssaStyle)
        {
            Name = ssaStyle.Name;
            FontName = ssaStyle.FontName;
            FontSize = ssaStyle.FontSize;

            Italic = ssaStyle.Italic;
            Bold = ssaStyle.Bold;
            Underline = ssaStyle.Underline;

            Primary = ssaStyle.Primary;
            Secondary = ssaStyle.Secondary;
            Tertiary = ssaStyle.Tertiary;
            Outline = ssaStyle.Outline;
            Background = ssaStyle.Background;

            ShadowWidth = ssaStyle.ShadowWidth;
            OutlineWidth = ssaStyle.OutlineWidth;

            Alignment = ssaStyle.Alignment;
            MarginLeft = ssaStyle.MarginLeft;
            MarginRight = ssaStyle.MarginRight;
            MarginVertical = ssaStyle.MarginVertical;

            BorderStyle = ssaStyle.BorderStyle;
            RawLine = ssaStyle.RawLine;
            LoadedFromHeader = ssaStyle.LoadedFromHeader;
        }

        internal string ToRawSsa(string styleFormat)
        {
            var sb = new StringBuilder();
            sb.Append("Style: ");
            var format = styleFormat.ToLower().Substring(8).Split(',');
            for (int i = 0; i < format.Length; i++)
            {
                string f = format[i].Trim().ToLower();

                switch (f)
                {
                    case "name":
                        sb.Append(Name);
                        break;
                    case "fontname":
                        sb.Append(FontName);
                        break;
                    case "fontsize":
                        sb.Append(FontSize);
                        break;
                    case "primarycolour":
                        sb.Append(ColorTranslator.ToWin32(Primary));
                        break;
                    case "secondarycolour":
                        sb.Append(ColorTranslator.ToWin32(Secondary));
                        break;
                    case "tertiarycolour":
                        sb.Append(ColorTranslator.ToWin32(Tertiary));
                        break;
                    case "outlinecolour":
                        sb.Append(ColorTranslator.ToWin32(Outline));
                        break;
                    case "backcolour":
                        sb.Append(ColorTranslator.ToWin32(Background));
                        break;
                    case "bold":
                        sb.Append(Convert.ToInt32(Bold));
                        break;
                    case "italic":
                        sb.Append(Convert.ToInt32(Italic));
                        break;
                    case "underline":
                        sb.Append(Convert.ToInt32(Underline));
                        break;
                    case "outline":
                        sb.Append(Outline);
                        break;
                    case "shadow":
                        sb.Append(OutlineWidth);
                        break;
                    // case "shadow": // repeating case, never executed
                    //    sb.Append(ShadowWidth);
                    //    break;
                    case "marginl":
                        sb.Append(MarginLeft);
                        break;
                    case "marginr":
                        sb.Append(MarginRight);
                        break;
                    case "marginv":
                        sb.Append(MarginVertical);
                        break;
                    case "borderstyle":
                        sb.Append(BorderStyle);
                        break;
                    case "encoding":
                        sb.Append('1');
                        break;
                    case "strikeout":
                        sb.Append('0');
                        break;
                    case "scalex":
                        sb.Append("100");
                        break;
                    case "scaley":
                        sb.Append("100");
                        break;
                    case "spacing":
                        sb.Append('0');
                        break;
                    case "angle":
                        sb.Append('0');
                        sb.Append(',');
                        break;
                }
            }

            string s = sb.ToString().Trim();
            return s.Substring(0, s.Length - 1);
        }

        internal string ToRawAss(string styleFormat)
        {
            var sb = new StringBuilder();
            sb.Append("Style: ");
            var format = styleFormat.ToLower().Substring(8).Split(',');
            for (int i = 0; i < format.Length; i++)
            {
                string f = format[i].Trim().ToLower();

                switch (f)
                {
                    case "name":
                        sb.Append(Name);
                        break;
                    case "fontname":
                        sb.Append(FontName);
                        break;
                    case "fontsize":
                        sb.Append(FontSize);
                        break;
                    case "primarycolour":
                        sb.Append(AdvancedSubStationAlpha.GetSsaColorString(Primary));
                        break;
                    case "secondarycolour":
                        sb.Append(AdvancedSubStationAlpha.GetSsaColorString(Secondary));
                        break;
                    case "tertiarycolour":
                        sb.Append(AdvancedSubStationAlpha.GetSsaColorString(Tertiary));
                        break;
                    case "outlinecolour":
                        sb.Append(AdvancedSubStationAlpha.GetSsaColorString(Outline));
                        break;
                    case "backcolour":
                        sb.Append(AdvancedSubStationAlpha.GetSsaColorString(Background));
                        break;
                    case "bold":
                        sb.Append(Convert.ToInt32(Bold));
                        break;
                    case "italic":
                        sb.Append(Convert.ToInt32(Italic));
                        break;
                    case "underline":
                        sb.Append(Convert.ToInt32(Underline));
                        break;
                    case "outline":
                        sb.Append(OutlineWidth);
                        break;
                    case "shadow":
                        sb.Append(ShadowWidth);
                        break;
                    case "alignment":
                        sb.Append(Alignment);
                        break;
                    case "marginl":
                        sb.Append(MarginLeft);
                        break;
                    case "marginr":
                        sb.Append(MarginRight);
                        break;
                    case "marginv":
                        sb.Append(MarginVertical);
                        break;
                    case "borderstyle":
                        sb.Append(BorderStyle);
                        break;
                    case "encoding":
                        sb.Append('1');
                        break;
                    case "strikeout":
                        sb.Append('0');
                        break;
                    case "scalex":
                        sb.Append("100");
                        break;
                    case "scaley":
                        sb.Append("100");
                        break;
                    case "spacing":
                        sb.Append('0');
                        break;
                    case "angle":
                        sb.Append('0');
                        break;
                }

                sb.Append(',');
            }

            string s = sb.ToString().Trim();
            return s.Substring(0, s.Length - 1);
        }
    }
}