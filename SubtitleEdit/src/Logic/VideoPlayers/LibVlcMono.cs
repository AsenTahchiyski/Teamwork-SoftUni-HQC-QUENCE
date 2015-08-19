namespace Nikse.SubtitleEdit.Logic.VideoPlayers
{
    using System;
    using System.Text;
    using System.Windows.Forms;

    internal class LibVlcMono : VideoPlayer, IDisposable
    {
        private Timer videoLoadedTimer;
        private Timer videoEndTimer;
        private IntPtr libVlcDll;
        private IntPtr libVlc;
        private IntPtr mediaPlayer;
        private Control ownerControl;
        private Form parentForm;

        public override string PlayerName
        {
            get { return "VLC Lib Mono"; }
        }

        public override int Volume
        {
            get
            {
                return NativeMethods.libvlc_audio_get_volume(mediaPlayer);
            }

            set
            {
                NativeMethods.libvlc_audio_set_volume(mediaPlayer, value);
            }
        }

        public override double Duration
        {
            get
            {
                return NativeMethods.libvlc_media_player_get_length(mediaPlayer) / TimeCode.BaseUnit;
            }
        }

        public override double CurrentPosition
        {
            get
            {
                return NativeMethods.libvlc_media_player_get_time(mediaPlayer) / TimeCode.BaseUnit;
            }

            set
            {
                NativeMethods.libvlc_media_player_set_time(mediaPlayer, (long)(value * TimeCode.BaseUnit));
            }
        }

        public override double PlayRate
        {
            get
            {
                return NativeMethods.libvlc_media_player_get_rate(mediaPlayer);
            }

            set
            {
                if (value >= 0 && value <= 2.0)
                {
                    NativeMethods.libvlc_media_player_set_rate(mediaPlayer, (float)value);
                }
            }
        }

        public override void Play()
        {
            NativeMethods.libvlc_media_player_play(mediaPlayer);
        }

        public override void Pause()
        {
            if (!IsPaused)
            {
                NativeMethods.libvlc_media_player_pause(mediaPlayer);
            }
        }

        public override void Stop()
        {
            NativeMethods.libvlc_media_player_stop(mediaPlayer);
        }

        public override bool IsPaused
        {
            get
            {
                const int Paused = 4;
                int state = NativeMethods.libvlc_media_player_get_state(mediaPlayer);
                return state == Paused;
            }
        }

        public override bool IsPlaying
        {
            get
            {
                const int Playing = 3;
                int state = NativeMethods.libvlc_media_player_get_state(mediaPlayer);
                return state == Playing;
            }
        }

        public int AudioTrackCount
        {
            get
            {
                return NativeMethods.libvlc_audio_get_track_count(mediaPlayer) - 1;
            }
        }

        public int AudioTrackNumber
        {
            get
            {
                return NativeMethods.libvlc_audio_get_track(mediaPlayer) - 1;
            }

            set
            {
                NativeMethods.libvlc_audio_set_track(mediaPlayer, value + 1);
            }
        }

        public LibVlcMono MakeSecondMediaPlayer(Control ownerControlSecond, string videoFileName, EventHandler onVideoLoaded, EventHandler onVideoEnded)
        {
            LibVlcMono newVlc = new LibVlcMono
            {
                libVlc = this.libVlc,
                libVlcDll = this.libVlcDll,
                ownerControl = ownerControlSecond
            };

            if (ownerControlSecond != null)
            {
                newVlc.parentForm = ownerControlSecond.FindForm();
            }

            newVlc.OnVideoLoaded = onVideoLoaded;
            newVlc.OnVideoEnded = onVideoEnded;

            if (string.IsNullOrEmpty(videoFileName))
            {
                return newVlc;
            }

            IntPtr media = NativeMethods.libvlc_media_new_path(libVlc, Encoding.UTF8.GetBytes(videoFileName + "\0"));
            newVlc.mediaPlayer = NativeMethods.libvlc_media_player_new_from_media(media);
            NativeMethods.libvlc_media_release(media);

            //  Linux: libvlc_media_player_set_xdrawable (_mediaPlayer, xdrawable);
            //  Mac: libvlc_media_player_set_nsobject (_mediaPlayer, view);
            var ownerHandle = ownerControlSecond == null ? IntPtr.Zero : ownerControlSecond.Handle;
            NativeMethods.libvlc_media_player_set_hwnd(newVlc.mediaPlayer, ownerHandle); // windows

            if (onVideoEnded != null)
            {
                newVlc.videoEndTimer = new Timer { Interval = 500 };
                newVlc.videoEndTimer.Tick += VideoEndTimerTick;
                newVlc.videoEndTimer.Start();
            }

            NativeMethods.libvlc_media_player_play(newVlc.mediaPlayer);
            newVlc.videoLoadedTimer = new Timer { Interval = 500 };
            newVlc.videoLoadedTimer.Tick += newVlc.VideoLoadedTimer_Tick;
            newVlc.videoLoadedTimer.Start();
            return newVlc;
        }

        private void VideoLoadedTimer_Tick(object sender, EventArgs e)
        {
            int i = 0;
            while (!IsPlaying && i < 50)
            {
                System.Threading.Thread.Sleep(100);
                i++;
            }

            NativeMethods.libvlc_media_player_pause(mediaPlayer);
            videoLoadedTimer.Stop();

            if (OnVideoLoaded != null)
            {
                OnVideoLoaded.Invoke(mediaPlayer, new EventArgs());
            }
        }

        public override void Initialize(Control ownerControl, string videoFileName, EventHandler onVideoLoaded, EventHandler onVideoEnded)
        {
            this.ownerControl = ownerControl;
            if (ownerControl != null)
            {
                parentForm = ownerControl.FindForm();
            }

            OnVideoLoaded = onVideoLoaded;
            OnVideoEnded = onVideoEnded;

            if (string.IsNullOrEmpty(videoFileName))
            {
                return;
            }

            string[] initParameters = { "--no-sub-autodetect-file" }; //, "--no-video-title-show" }; // TODO: Put in options/config file
            libVlc = NativeMethods.libvlc_new(initParameters.Length, initParameters);
            IntPtr media = NativeMethods.libvlc_media_new_path(libVlc, Encoding.UTF8.GetBytes(videoFileName + "\0"));
            mediaPlayer = NativeMethods.libvlc_media_player_new_from_media(media);
            NativeMethods.libvlc_media_release(media);

            //  Linux: libvlc_media_player_set_xdrawable (_mediaPlayer, xdrawable);
            //  Mac: libvlc_media_player_set_nsobject (_mediaPlayer, view);
            var ownerHandle = ownerControl == null ? IntPtr.Zero : ownerControl.Handle;
            NativeMethods.libvlc_media_player_set_hwnd(mediaPlayer, ownerHandle); // windows

            if (onVideoEnded != null)
            {
                videoEndTimer = new Timer { Interval = 500 };
                videoEndTimer.Tick += VideoEndTimerTick;
                videoEndTimer.Start();
            }

            NativeMethods.libvlc_media_player_play(mediaPlayer);
            videoLoadedTimer = new Timer { Interval = 500 };
            videoLoadedTimer.Tick += VideoLoadedTimer_Tick;
            videoLoadedTimer.Start();
        }

        private void VideoEndTimerTick(object sender, EventArgs e)
        {
            const int ended = 6;
            int state = NativeMethods.libvlc_media_player_get_state(mediaPlayer);
            if (state != ended)
            {
                return;
            }
            // hack to make sure VLC is in ready state
            Stop();
            Play();
            Pause();
            if (OnVideoEnded != null)
            {
                OnVideoEnded.Invoke(mediaPlayer, new EventArgs());
            }
        }

        public override void DisposeVideoPlayer()
        {
            if (videoLoadedTimer != null)
            {
                videoLoadedTimer.Stop();
            }

            if (videoEndTimer != null)
            {
                videoEndTimer.Stop();
            }

            System.Threading.ThreadPool.QueueUserWorkItem(DisposeVlc, this);
        }

        private void DisposeVlc(object player)
        {
            ReleaseUnmangedResources();
        }

        public override event EventHandler OnVideoLoaded;

        public override event EventHandler OnVideoEnded;

        ~LibVlcMono()
        {
            Dispose(false);
        }

        private void ReleaseUnmangedResources()
        {
            try
            {
                if (mediaPlayer != IntPtr.Zero)
                {
                    NativeMethods.libvlc_media_player_stop(mediaPlayer);
                    NativeMethods.libvlc_media_list_player_release(mediaPlayer);
                    mediaPlayer = IntPtr.Zero;
                }

                if (libVlc == IntPtr.Zero)
                {
                    return;
                }

                NativeMethods.libvlc_release(libVlc);
                libVlc = IntPtr.Zero;
            }
            catch
            {
            }
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
                if (videoLoadedTimer != null)
                {
                    videoLoadedTimer.Dispose();
                    videoLoadedTimer = null;
                }

                if (videoEndTimer != null)
                {
                    videoEndTimer.Dispose();
                    videoEndTimer = null;
                }
            }

            ReleaseUnmangedResources();
        }
    }
}
