﻿namespace Nikse.SubtitleEdit.Logic.ContainerFormats.Matroska
{
    using System;

    internal class MatroskaSubtitle
    {
        public byte[] Data { get; set; }
     
        public long Start { get; set; }
        
        public long Duration { get; set; }

        public MatroskaSubtitle(byte[] data, long start, long duration)
        {
            Data = data;
            Start = start;
            Duration = duration;
        }

        public MatroskaSubtitle(byte[] data, long start)
            : this(data, start, 0)
        {
        }

        public long End
        {
            get
            {
                return Start + Duration;
            }
        }

        public string Text
        {
            get
            {
                return Data != null ? System.Text.Encoding.UTF8.GetString(Data).Replace("\\N", Environment.NewLine) : string.Empty;
            }
        }
    }
}
