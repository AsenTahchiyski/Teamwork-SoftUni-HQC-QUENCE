namespace Nikse.SubtitleEdit.Logic
{
    using System;
    using SubtitleFormats;

    public class TimeCode
    {
        public static readonly TimeCode MaxTime = new TimeCode(99, 59, 59, 999);

        public const double BaseUnit = 1000.0; // Base unit of time

        public bool IsMaxTime
        {
            get
            {
                return Math.Abs(TotalMilliseconds - MaxTime.TotalMilliseconds) < 0.01;
            }
        }

        public static TimeCode FromSeconds(double seconds)
        {
            return new TimeCode(seconds * BaseUnit);
        }

        public static double ParseToMilliseconds(string text)
        {
            string[] parts = text.Split(new[] { ':', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4)
            {
                int hours;
                int minutes;
                int seconds;
                int milliseconds;
                if (int.TryParse(parts[0], out hours) && 
                    int.TryParse(parts[1], out minutes) && 
                    int.TryParse(parts[2], out seconds) && 
                    int.TryParse(parts[3], out milliseconds))
                {
                    return new TimeSpan(0, hours, minutes, seconds, milliseconds).TotalMilliseconds;
                }
            }

            return 0;
        }

        public static double ParseHHMMSSFFToMilliseconds(string text)
        {
            string[] parts = text.Split(new[] { ':', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4)
            {
                int hours;
                int minutes;
                int seconds;
                int frames;
                if (int.TryParse(parts[0], out hours) && int.TryParse(parts[1], out minutes) && int.TryParse(parts[2], out seconds) && int.TryParse(parts[3], out frames))
                {
                    return new TimeCode(hours, minutes, seconds, SubtitleFormat.FramesToMillisecondsMax999(frames)).TotalMilliseconds;
                }
            }

            return 0;
        }

        public TimeCode(TimeSpan timeSpan)
        {
            TotalMilliseconds = timeSpan.TotalMilliseconds;
        }

        public TimeCode(double totalMilliseconds)
        {
            this.TotalMilliseconds = totalMilliseconds;
        }

        public TimeCode(int hour, int minute, int seconds, int milliseconds)
        {
            TotalMilliseconds = hour * 60 * 60 * BaseUnit + minute * 60 * BaseUnit + seconds * BaseUnit + milliseconds;
        }

        public int Hours
        {
            get
            {
                var ts = TimeSpan;
                return ts.Hours + ts.Days * 24;
            }
            set
            {
                var ts = TimeSpan;
                TotalMilliseconds = new TimeSpan(0, value, ts.Minutes, ts.Seconds, ts.Milliseconds).TotalMilliseconds;
            }
        }

        public int Minutes
        {
            get
            {
                return TimeSpan.Minutes;
            }
            set
            {
                var ts = TimeSpan;
                TotalMilliseconds = new TimeSpan(0, ts.Hours, value, ts.Seconds, ts.Milliseconds).TotalMilliseconds;
            }
        }

        public int Seconds
        {
            get
            {
                return TimeSpan.Seconds;
            }
            set
            {
                var ts = TimeSpan;
                TotalMilliseconds = new TimeSpan(0, ts.Hours, ts.Minutes, value, ts.Milliseconds).TotalMilliseconds;
            }
        }

        public int Milliseconds
        {
            get
            {
                return TimeSpan.Milliseconds;
            }
            set
            {
                var ts = TimeSpan;
                TotalMilliseconds = new TimeSpan(0, ts.Hours, ts.Minutes, ts.Seconds, value).TotalMilliseconds;
            }
        }

        public double TotalMilliseconds { get; set; }

        public double TotalSeconds
        {
            get
            {
                return TotalMilliseconds / BaseUnit;
            }

            set
            {
                TotalMilliseconds = value * BaseUnit;
            }
        }

        public TimeSpan TimeSpan
        {
            get
            {
                return TimeSpan.FromMilliseconds(TotalMilliseconds);
            }

            set
            {
                TotalMilliseconds = value.TotalMilliseconds;
            }
        }

        public void AddTime(int hour, int minutes, int seconds, int milliseconds)
        {
            Hours += hour;
            Minutes += minutes;
            Seconds += seconds;
            Milliseconds += milliseconds;
        }

        public void AddTime(long milliseconds)
        {
            TotalMilliseconds += milliseconds;
        }

        public void AddTime(TimeSpan timeSpan)
        {
            TotalMilliseconds += timeSpan.TotalMilliseconds;
        }

        public void AddTime(double milliseconds)
        {
            TotalMilliseconds += milliseconds;
        }

        public override string ToString()
        {
            var ts = TimeSpan;
            string s = string.Format("{0:00}:{1:00}:{2:00},{3:000}", ts.Hours + ts.Days * 24, ts.Minutes, ts.Seconds, ts.Milliseconds);

            if (TotalMilliseconds >= 0)
            {
                return s;
            }

            return "-" + s.Replace("-", string.Empty);
        }

        public string ToShortString()
        {
            var ts = TimeSpan;
            string s;
            if (ts.Minutes == 0 && ts.Hours == 0 && ts.Days == 0)
            {
                s = string.Format("{0:0},{1:000}", ts.Seconds, ts.Milliseconds);
            }
            else if (ts.Hours == 0 && ts.Days == 0)
            {
                s = string.Format("{0:0}:{1:00},{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds);
            }
            else
            {
                s = string.Format("{0:0}:{1:00}:{2:00},{3:000}", ts.Hours + ts.Days * 24, ts.Minutes, ts.Seconds, ts.Milliseconds);
            }

            if (TotalMilliseconds >= 0)
            {
                return s;
            }

            return "-" + s.Replace("-", string.Empty);
        }

        public string ToShortStringHHMMSSFF()
        {
            var ts = TimeSpan;
            if (ts.Minutes == 0 && ts.Hours == 0)
            {
                return string.Format("{0:00}:{1:00}", ts.Seconds, SubtitleFormat.MillisecondsToFramesMaxFrameRate(ts.Milliseconds));
            }

            if (ts.Hours == 0)
            {
                return string.Format("{0:00}:{1:00}:{2:00}", ts.Minutes, ts.Seconds, SubtitleFormat.MillisecondsToFramesMaxFrameRate(ts.Milliseconds));
            }

            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds, SubtitleFormat.MillisecondsToFramesMaxFrameRate(ts.Milliseconds));
        }

        public string ToHHMMSSFF()
        {
            var ts = TimeSpan;
            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds, SubtitleFormat.MillisecondsToFramesMaxFrameRate(ts.Milliseconds));
        }

        public string ToHHMMSSPeriodFF()
        {
            var ts = TimeSpan;
            return string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, SubtitleFormat.MillisecondsToFramesMaxFrameRate(ts.Milliseconds));
        }
    }
}