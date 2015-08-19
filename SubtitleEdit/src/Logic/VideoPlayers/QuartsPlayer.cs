//http://msdn.microsoft.com/en-us/library/dd375454%28VS.85%29.aspx
//http://msdn.microsoft.com/en-us/library/dd387928%28v=vs.85%29.aspx

namespace Nikse.SubtitleEdit.Logic.VideoPlayers
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using QuartzTypeLib;
    using System.ComponentModel;

    public class QuartsPlayer : VideoPlayer, IDisposable
    {
        public override event EventHandler OnVideoLoaded;
        public override event EventHandler OnVideoEnded;

        private IVideoWindow quartzVideo;
        private FilgraphManager quartzFilgraphManager;
        private IMediaPosition mediaPosition;
        private bool isPaused;
        private Control owner;
        private Timer videoEndTimer;
        private BackgroundWorker videoLoader;
        private int sourceWidth;
        private int sourceHeight;

        public override string PlayerName { get { return "DirectShow"; } }

        /// <summary>
        /// In DirectX -10000 is silent and 0 is full volume.
        /// Also, -3500 to 0 seems to be all you can hear! Not much use for -3500 to -9999...
        /// </summary>
        public override int Volume
        {
            get
            {
                try
                {
                    return ((quartzFilgraphManager as IBasicAudio).Volume / 35) + 100;
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                try
                {
                    if (value == 0)
                    {
                        var basicAudio = quartzFilgraphManager as IBasicAudio;
                        if (basicAudio != null) basicAudio.Volume = -10000;
                    }
                    else
                    {
                        var basicAudio = quartzFilgraphManager as IBasicAudio;
                        if (basicAudio != null) basicAudio.Volume = (value - 100) * 35;
                    }
                }
                catch
                {
                }
            }
        }

        public override double Duration
        {
            get
            {
                try
                {
                    return mediaPosition.Duration;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public override double CurrentPosition
        {
            get
            {
                try
                {
                    return mediaPosition.CurrentPosition;
                }
                catch
                {
                    return 0;
                }
            }

            set
            {
                if (value >= 0 && value <= Duration)
                {
                    mediaPosition.CurrentPosition = value;
                }
            }
        }

        public override double PlayRate
        {
            get { return mediaPosition.Rate; }
            set
            {
                if (value >= 0 && value <= 2.0)
                {
                    mediaPosition.Rate = value;
                }
            }
        }

        public override void Play()
        {
            quartzFilgraphManager.Run();
            isPaused = false;
        }

        public override void Pause()
        {
            quartzFilgraphManager.Pause();
            isPaused = true;
        }

        public override void Stop()
        {
            quartzFilgraphManager.Stop();
            isPaused = true;
        }

        public override bool IsPaused
        {
            get
            {
                return isPaused;
            }
        }

        public override bool IsPlaying
        {
            get
            {
                return !IsPaused;
            }
        }

        public override void Initialize(Control ownerControl, string videoFileName, EventHandler onVideoLoaded, EventHandler onVideoEnded)
        {
            const int wsChild = 0x40000000;

            var extension = System.IO.Path.GetExtension(videoFileName);
            if (extension == null)
            {
                return;
            }

            string ext = extension.ToLower();
            bool isAudio = ext == ".mp3" || ext == ".wav" || ext == ".wma" || ext == ".m4a";

            OnVideoLoaded = onVideoLoaded;
            OnVideoEnded = onVideoEnded;

            VideoFileName = videoFileName;
            owner = ownerControl;
            quartzFilgraphManager = new FilgraphManager();
            quartzFilgraphManager.RenderFile(VideoFileName);

            if (!isAudio)
            {
                quartzVideo = quartzFilgraphManager as IVideoWindow;
                if (quartzVideo != null)
                {
                    quartzVideo.Owner = (int)ownerControl.Handle;
                    quartzVideo.SetWindowPosition(0, 0, ownerControl.Width, ownerControl.Height);
                    quartzVideo.WindowStyle = wsChild;
                }
            }
            //Play();

            if (!isAudio)
            {
                var basicVideo = quartzFilgraphManager as IBasicVideo;
                if (basicVideo != null)
                {
                    basicVideo.GetVideoSize(out sourceWidth, out sourceHeight);
                }
            }

            owner.Resize += OwnerControlResize;
            mediaPosition = (IMediaPosition)quartzFilgraphManager;
            if (OnVideoLoaded != null)
            {
                videoLoader = new BackgroundWorker();
                videoLoader.RunWorkerCompleted += VideoLoaderRunWorkerCompleted;
                videoLoader.DoWork += VideoLoaderDoWork;
                videoLoader.RunWorkerAsync();
            }

            OwnerControlResize(this, null);
            videoEndTimer = new Timer { Interval = 500 };
            videoEndTimer.Tick += VideoEndTimerTick;
            videoEndTimer.Start();

            if (isAudio)
            {
                return;
            }

            if (quartzVideo != null)
            {
                quartzVideo.MessageDrain = (int)ownerControl.Handle;
            }
        }

        public static VideoInfo GetVideoInfo(string videoFileName)
        {
            var info = new VideoInfo { Success = false };

            try
            {
                var quartzFilgraphManager = new FilgraphManager();
                quartzFilgraphManager.RenderFile(videoFileName);
                int width;
                int height;
                (quartzFilgraphManager as IBasicVideo).GetVideoSize(out width, out height);

                info.Width = width;
                info.Height = height;
                var basicVideo2 = (quartzFilgraphManager as IBasicVideo2);
                if (basicVideo2 != null && basicVideo2.AvgTimePerFrame > 0)
                {
                    info.FramesPerSecond = 1 / basicVideo2.AvgTimePerFrame;
                }

                info.Success = true;
                var iMediaPosition = (quartzFilgraphManager as IMediaPosition);
                if (iMediaPosition != null)
                {
                    info.TotalMilliseconds = iMediaPosition.Duration * 1000;
                    info.TotalSeconds = iMediaPosition.Duration;
                }

                info.TotalFrames = info.TotalSeconds * info.FramesPerSecond;
                info.VideoCodec = string.Empty; // TODO: Get real codec names from quartzFilgraphManager.FilterCollection;

                Marshal.ReleaseComObject(quartzFilgraphManager);
            }
            catch
            {
            }

            return info;
        }

        private static void VideoLoaderDoWork(object sender, DoWorkEventArgs e)
        {
            //int i = 0;
            //while (CurrentPosition < 1 && i < 100)
            //{
            Application.DoEvents();
            //    System.Threading.Thread.Sleep(5);
            //    i++;
            //}
        }

        private void VideoLoaderRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (OnVideoLoaded != null)
            {
                try
                {
                    OnVideoLoaded.Invoke(quartzFilgraphManager, new EventArgs());
                }
                catch
                {
                }
            }
            videoEndTimer = null;
        }

        private void VideoEndTimerTick(object sender, EventArgs e)
        {
            if (quartzFilgraphManager == null || !(CurrentPosition >= Duration) || isPaused != false)
            {
                return;
            }

            isPaused = true;
            if (OnVideoEnded != null && quartzFilgraphManager != null)
            {
                OnVideoEnded.Invoke(quartzFilgraphManager, new EventArgs());
            }
        }

        private void OwnerControlResize(object sender, EventArgs e)
        {
            if (quartzVideo == null)
            {
                return;
            }

            // calc new scaled size with correct aspect ratio
            float factorX = owner.Width / (float)sourceWidth;
            float factorY = owner.Height / (float)sourceHeight;

            if (factorX > factorY)
            {
                quartzVideo.Width = (int)(sourceWidth * factorY);
                quartzVideo.Height = (int)(sourceHeight * factorY);
            }
            else
            {
                quartzVideo.Width = (int)(sourceWidth * factorX);
                quartzVideo.Height = (int)(sourceHeight * factorX);
            }

            quartzVideo.Left = (owner.Width - quartzVideo.Width) / 2;
            quartzVideo.Top = (owner.Height - quartzVideo.Height) / 2;
        }

        public override void DisposeVideoPlayer()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(DisposeQuarts, quartzFilgraphManager);
        }

        private void DisposeQuarts(object player)
        {
            Dispose();
        }

        private void ReleaseUnmangedResources()
        {
            try
            {
                if (quartzVideo != null)
                {
                    quartzVideo.Owner = -1;
                }
            }
            catch
            {
            }

            if (quartzFilgraphManager != null)
            {
                try
                {
                    quartzFilgraphManager.Stop();
                    Marshal.ReleaseComObject(quartzFilgraphManager);
                    quartzFilgraphManager = null;
                }
                catch
                {
                }
            }

            quartzVideo = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (videoEndTimer != null)
                {
                    videoEndTimer.Dispose();
                    videoEndTimer = null;
                }

                if (videoLoader != null)
                {
                    videoLoader.Dispose();
                    videoLoader = null;
                }
            }

            ReleaseUnmangedResources();
        }
    }
}