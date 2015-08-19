namespace Nikse.SubtitleEdit.Logic.VideoPlayers
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows.Forms;

    public class MPlayer : VideoPlayer, IDisposable
    {
        private Process mplayer;
        private Timer timer;
        private TimeSpan lengthInSeconds;
        private TimeSpan lastLengthInSeconds = TimeSpan.FromDays(0);
        private bool paused;
        private bool loaded = false;
        private bool ended = false;
        private string videoFileName;
        private bool waitForChange = false;
        private double? pausePosition; // Hack to hold precise seeking when paused
        private int pauseCounts;
        private double speed = 1.0;
        private float volume;
        private double timePosition;

        public int Width { get; private set; }
        
        public int Height { get; private set; }
        
        public float FramesPerSecond { get; private set; }
        
        public string VideoFormat { get; private set; }
        
        public string VideoCodec { get; private set; }

        public override string PlayerName
        {
            get { return "MPlayer"; }
        }

        public override int Volume
        {
            get
            {
                return (int)volume;
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    return;
                }

                volume = value;
                SetProperty("volume", value.ToString(), true);
            }
        }

        public override double Duration
        {
            get { return lengthInSeconds.TotalSeconds; }
        }

        public override double CurrentPosition
        {
            get
            {
                if (!paused || pausePosition == null)
                {
                    return timePosition;
                }

                return pausePosition < 0 ? 0 : pausePosition.Value;
            }
            set
            {
                // NOTE: FOR ACCURATE SEARCH USE MPlayer2 - http://www.mplayer2.org/)
                timePosition = value;
                if (IsPaused && value <= Duration)
                {
                    pausePosition = value;
                }

                mplayer.StandardInput.WriteLine(string.Format("pausing_keep seek {0:0.0} 2", value));
            }
        }

        public override double PlayRate
        {
            get
            {
                return speed;
            }
            set
            {
                if (!(value >= 0) || !(value <= 2.0))
                {
                    return;
                }

                speed = value;
                SetProperty("speed", value.ToString(CultureInfo.InvariantCulture), true);
            }
        }

        public override void Play()
        {
            mplayer.StandardInput.WriteLine("pause");
            pauseCounts = 0;
            paused = false;
            pausePosition = null;
        }

        public override void Pause()
        {
            if (!paused)
            {
                mplayer.StandardInput.WriteLine("pause");
            }

            pauseCounts = 0;
            paused = true;
        }

        public override void Stop()
        {
            CurrentPosition = 0;
            Pause();
            mplayer.StandardInput.WriteLine("pausing_keep_force seek 0 2");
            pauseCounts = 0;
            paused = true;
            lastLengthInSeconds = lengthInSeconds;
            pausePosition = null;
        }

        public override bool IsPaused
        {
            get { return paused; }
        }

        public override bool IsPlaying
        {
            get { return !paused; }
        }

        public override void Initialize(Control ownerControl, string videoFileName, EventHandler onVideoLoaded, EventHandler onVideoEnded)
        {
            loaded = false;
            this.videoFileName = videoFileName;
            string mplayerExeName = GetMPlayerFileName;
            if (string.IsNullOrEmpty(mplayerExeName))
            {
                return;
            }

            mplayer = new Process { StartInfo = { FileName = mplayerExeName } };
            //vo options: gl, gl2, directx:noaccel
            if (Configuration.IsRunningOnLinux() || Configuration.IsRunningOnMac())
            {
                mplayer.StartInfo.Arguments = "-nofs -quiet -slave -idle -nosub -noautosub -loop 0 -osdlevel 0 -vsync -wid " + ownerControl.Handle.ToInt32() + " \"" + videoFileName + "\" ";
            }
            else
                mplayer.StartInfo.Arguments = "-nofs -quiet -slave -idle -nosub -noautosub -loop 0 -osdlevel 0 -vo direct3d -wid " + (int)ownerControl.Handle + " \"" + videoFileName + "\" ";

            mplayer.StartInfo.UseShellExecute = false;
            mplayer.StartInfo.RedirectStandardInput = true;
            mplayer.StartInfo.RedirectStandardOutput = true;
            mplayer.StartInfo.CreateNoWindow = true;
            mplayer.OutputDataReceived += MPlayerOutputDataReceived;

            try
            {
                mplayer.Start();
            }
            catch
            {
                MessageBox.Show("Unable to start MPlayer - make sure MPlayer is installed!");
                throw;
            }

            mplayer.StandardInput.NewLine = "\n";
            mplayer.BeginOutputReadLine(); // Async reading of output to prevent deadlock

            // static properties
            GetProperty("width", true);
            GetProperty("height", true);
            GetProperty("fps", true);
            GetProperty("video_format", true);
            GetProperty("video_codec", true);
            GetProperty("length", true);

            // semi static variable
            GetProperty("volume", true);

            // start timer to collect variable properties
            timer = new Timer { Interval = 1000 };
            timer.Tick += timer_Tick;
            timer.Start();

            OnVideoLoaded = onVideoLoaded;
            OnVideoEnded = onVideoEnded;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            // variable properties
            mplayer.StandardInput.WriteLine("pausing_keep_force get_property time_pos");
            mplayer.StandardInput.WriteLine("pausing_keep_force get_property pause");

            if (!ended && OnVideoEnded != null && lengthInSeconds.TotalSeconds == Duration)
            {
                //  _ended = true;
                //  OnVideoEnded.Invoke(this, null);
            }
            else if (lengthInSeconds.TotalSeconds < Duration)
            {
                ended = false;
            }

            if (OnVideoLoaded != null && loaded)
            {
                timer.Stop();
                loaded = false;
                OnVideoLoaded.Invoke(this, null);
                timer.Interval = 100;
                timer.Start();
            }

            if (lengthInSeconds != lastLengthInSeconds)
            {
                paused = false;
            }

            lastLengthInSeconds = lengthInSeconds;
        }

        private void MPlayerOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            Debug.WriteLine("MPlayer: " + e.Data);

            if (e.Data.StartsWith("Playing "))
            {
                loaded = true;
                return;
            }

            if (e.Data.StartsWith("Exiting..."))
            {
                ended = true;
                if (!loaded) return;
                {
                    mplayer.StandardInput.WriteLine("loadfile " + videoFileName);
                }

                if (OnVideoEnded != null)
                {
                    OnVideoEnded.Invoke(this, null);
                }

                return;
            }

            int indexOfEqual = e.Data.IndexOf('=');
            if (indexOfEqual <= 0 || indexOfEqual + 1 >= e.Data.Length || !e.Data.StartsWith("ANS_"))
            {
                return;
            }

            string code = e.Data.Substring(0, indexOfEqual);
            string value = e.Data.Substring(indexOfEqual + 1);

            switch (code)
            {
                // Examples:
                //  ANS_time_pos=8.299958, ANS_width=624, ANS_height=352, ANS_fps=23.976025, ANS_video_format=1145656920, ANS_video_format=1145656920, ANS_video_codec=ffodivx,
                //  ANS_length=1351.600213, ANS_volume=100.000000
                case "ANS_time_pos":
                    timePosition = Convert.ToDouble(value.Replace(",", "."), CultureInfo.InvariantCulture);
                    break;
                case "ANS_width":
                    Width = Convert.ToInt32(value);
                    break;
                case "ANS_height":
                    Height = Convert.ToInt32(value);
                    break;
                case "ANS_fps":
                    double d;
                    if (double.TryParse(value, out d))
                    {
                        FramesPerSecond = (float)Convert.ToDouble(value.Replace(",", "."), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        FramesPerSecond = 25.0f;
                    }

                    break;
                case "ANS_video_format":
                    VideoFormat = value;
                    break;
                case "ANS_video_codec":
                    VideoCodec = value;
                    break;
                case "ANS_length":
                    lengthInSeconds = TimeSpan.FromSeconds(Convert.ToDouble(value.Replace(",", "."), CultureInfo.InvariantCulture));
                    break;
                case "ANS_volume":
                    volume = (float)Convert.ToDouble(value.Replace(",", "."), CultureInfo.InvariantCulture);
                    break;
                case "ANS_pause":
                    if (value == "yes" || value == "1")
                    {
                        pauseCounts++;
                    }
                    else
                    {
                        pauseCounts--;
                    }

                    if (pauseCounts > 3)
                    {
                        paused = true;
                    }
                    else if (pauseCounts < -3)
                    {
                        paused = false;
                        pausePosition = null;
                    }
                    else if (Math.Abs(pauseCounts) > 10)
                    {
                        pauseCounts = 0;
                    }

                    break;
            }

            waitForChange = false;
        }

        public static string GetMPlayerFileName
        {
            get
            {
                if (Configuration.IsRunningOnLinux() || Configuration.IsRunningOnMac())
                {
                    return "mplayer";
                }

                string fileName = Path.Combine(Configuration.BaseDirectory, "mplayer2.exe");
                if (File.Exists(fileName))
                {
                    return fileName;
                }

                fileName = Path.Combine(Configuration.BaseDirectory, "mplayer.exe");
                if (File.Exists(fileName))
                {
                    return fileName;
                }

                fileName = @"C:\Program Files (x86)\SMPlayer\mplayer\mplayer.exe";
                if (File.Exists(fileName))
                {
                    return fileName;
                }

                fileName = @"C:\Program Files (x86)\mplayer\mplayer.exe";
                if (File.Exists(fileName))
                {
                    return fileName;
                }

                fileName = @"C:\Program Files\mplayer\mplayer.exe";
                if (File.Exists(fileName))
                {
                    return fileName;
                }

                fileName = @"C:\Program Files\SMPlayer\mplayer\mplayer.exe";
                return File.Exists(fileName) ? fileName : null;
            }
        }

        public static bool IsInstalled
        {
            get
            {
                return GetMPlayerFileName != null;
            }
        }

        private void GetProperty(string propertyName, bool keepPause)
        {
            if (keepPause)
            {
                mplayer.StandardInput.WriteLine("pausing_keep get_property " + propertyName);
            }
            else
            {
                mplayer.StandardInput.WriteLine("get_property " + propertyName);
            }
        }

        private void SetProperty(string propertyName, string value, bool keepPause)
        {
            if (keepPause)
            {
                mplayer.StandardInput.WriteLine("pausing_keep set_property " + propertyName + " " + value);
            }
            else
            {
                mplayer.StandardInput.WriteLine("set_property " + propertyName + " " + value);
            }

            UglySleep();
        }

        private void UglySleep()
        {
            waitForChange = true;
            int i = 0;

            while (i < 100 && waitForChange)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(2);
                i++;
            }

            waitForChange = false;
        }

        public override void DisposeVideoPlayer()
        {
            timer.Stop();
            if (mplayer == null)
            {
                return;
            }

            mplayer.OutputDataReceived -= MPlayerOutputDataReceived;
            mplayer.StandardInput.WriteLine("quit");
        }

        public override event EventHandler OnVideoLoaded;

        public override event EventHandler OnVideoEnded;

        public void Dispose()
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

            if (mplayer != null)
            {
                mplayer.Dispose();
                mplayer = null;
            }

            if (timer == null)
            {
                return;
            }

            timer.Dispose();
            timer = null;
        }
    }
}