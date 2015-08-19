namespace Nikse.SubtitleEdit.Logic.VideoPlayers
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;
    using Core;

    public class LibVlcDynamic : VideoPlayer, IDisposable
    {
        private Timer videoLoadedTimer;
        private Timer videoEndTimer;
        private Timer mouseTimer;

        private IntPtr libVlcDll;
        private IntPtr libVlc;
        private IntPtr mediaPlayer;
        private Control ownerControl;
        private Form parentForm;
        private double? pausePosition; // Hack to hold precise seeking when paused

        // LibVLC Core - http://www.videolan.org/developers/vlc/doc/doxygen/html/group__libvlc__core.html
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LibvlcNew(int argc, [MarshalAs(UnmanagedType.LPArray)] string[] argv);
        private LibvlcNew libvlcNew;

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate IntPtr libvlc_get_version();
        //private libvlc_get_version _libvlc_get_version;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcRelease(IntPtr libVlc);
        private LibvlcRelease libvlcRelease;

        // LibVLC Media - http://www.videolan.org/developers/vlc/doc/doxygen/html/group__libvlc__media.html
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LibvlcMediaNewPath(IntPtr instance, byte[] input);
        private LibvlcMediaNewPath libvlcMediaNewPath;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcMediaRelease(IntPtr media);
        private LibvlcMediaRelease libvlcMediaRelease;

        // LibVLC Video Controls - http://www.videolan.org/developers/vlc/doc/doxygen/html/group__libvlc__video.html#g8f55326b8b51aecb59d8b8a446c3f118
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcVideoGetSize(IntPtr mediaPlayer, uint number, out uint x, out uint y);
        private LibvlcVideoGetSize libvlcVideoGetSize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcVideoTakeSnapshot(IntPtr mediaPlayer, byte num, byte[] filePath, uint width, uint height);
        private LibvlcVideoTakeSnapshot libvlcVideoTakeSnapshot;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcVideoSetCallbacks(IntPtr playerInstance, LockCallbackDelegate @lock, UnlockCallbackDelegate unlock, DisplayCallbackDelegate display, IntPtr opaque);
        private LibvlcVideoSetCallbacks libvlcVideoSetCallbacks;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcVideoSetFormat(IntPtr mediaPlayer, string chroma, uint width, uint height, uint pitch);
        private LibvlcVideoSetFormat libvlcVideoSetFormat;

        // LibVLC Audio Controls - http://www.videolan.org/developers/vlc/doc/doxygen/html/group__libvlc__audio.html
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcAudioGetVolume(IntPtr mediaPlayer);
        private LibvlcAudioGetVolume libvlcAudioGetVolume;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcAudioSetVolume(IntPtr mediaPlayer, int volume);
        private LibvlcAudioSetVolume libvlcAudioSetVolume;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcAudioGetTrackCount(IntPtr mediaPlayer);
        private LibvlcAudioGetTrackCount libvlcAudioGetTrackCount;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcAudioGetTrack(IntPtr mediaPlayer);
        private LibvlcAudioGetTrack libvlcAudioGetTrack;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcAudioSetTrack(IntPtr mediaPlayer, int trackNumber);
        private LibvlcAudioSetTrack libvlcAudioSetTrack;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Int64 LibvlcAudioGetDelay(IntPtr mediaPlayer);
        private LibvlcAudioGetDelay libvlcAudioGetDelay;

        // LibVLC media player - http://www.videolan.org/developers/vlc/doc/doxygen/html/group__libvlc__media__player.html
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LibvlcMediaPlayerNewFromMedia(IntPtr media);
        private LibvlcMediaPlayerNewFromMedia libvlcMediaPlayerNewFromMedia;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcMediaPlayerPlay(IntPtr mediaPlayer);
        private LibvlcMediaPlayerPlay libvlcMediaPlayerPlay;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcMediaPlayerStop(IntPtr mediaPlayer);
        private LibvlcMediaPlayerStop libvlcMediaPlayerStop;

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate void libvlc_media_player_pause(IntPtr mediaPlayer);
        //private libvlc_media_player_pause _libvlc_media_player_pause;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcMediaPlayerSetHwnd(IntPtr mediaPlayer, IntPtr windowsHandle);
        private LibvlcMediaPlayerSetHwnd libvlcMediaPlayerSetHwnd;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcMediaPlayerIsPlaying(IntPtr mediaPlayer);
        private LibvlcMediaPlayerIsPlaying libvlcMediaPlayerIsPlaying;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcMediaPlayerSetPause(IntPtr mediaPlayer, int doPause);
        private LibvlcMediaPlayerSetPause libvlcMediaPlayerSetPause;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate long LibvlcMediaPlayerGetTime(IntPtr mediaPlayer);
        private LibvlcMediaPlayerGetTime libvlcMediaPlayerGetTime;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcMediaPlayerSetTime(IntPtr mediaPlayer, long position);
        private LibvlcMediaPlayerSetTime libvlcMediaPlayerSetTime;

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //private delegate float libvlc_media_player_get_fps(IntPtr mediaPlayer);
        //private libvlc_media_player_get_fps _libvlc_media_player_get_fps;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte LibvlcMediaPlayerGetState(IntPtr mediaPlayer);
        private LibvlcMediaPlayerGetState libvlcMediaPlayerGetState;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Int64 LibvlcMediaPlayerGetLength(IntPtr mediaPlayer);
        private LibvlcMediaPlayerGetLength libvlcMediaPlayerGetLength;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LibvlcMediaPlayerRelease(IntPtr mediaPlayer);
        private LibvlcMediaPlayerRelease libvlcMediaPlayerRelease;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate float LibvlcMediaPlayerGetRate(IntPtr mediaPlayer);
        private LibvlcMediaPlayerGetRate libvlcMediaPlayerGetRate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcMediaPlayerSetRate(IntPtr mediaPlayer, float rate);
        private LibvlcMediaPlayerSetRate libvlcMediaPlayerSetRate;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcMediaPlayerNextFrame(IntPtr mediaPlayer);
        private LibvlcMediaPlayerNextFrame libvlcMediaPlayerNextFrame;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int LibvlcVideoSetSpu(IntPtr mediaPlayer, int trackNumber);
        private LibvlcVideoSetSpu libvlcVideoSetSpu;


        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //public unsafe delegate void* LockEventHandler(void* opaque, void** plane);

        //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        //public unsafe delegate void UnlockEventHandler(void* opaque, void* picture, void** plane);

        /// <summary>
        /// Callback prototype to allocate and lock a picture buffer. Whenever a new video frame needs to be decoded, the lock callback is invoked. Depending on the video chroma, one or three pixel planes of adequate dimensions must be returned via the second parameter. Those planes must be aligned on 32-bytes boundaries.
        /// </summary>
        /// <param name="opaque">Private pointer as passed to SetCallbacks()</param>
        /// <param name="planes">Planes start address of the pixel planes (LibVLC allocates the array of void pointers, this callback must initialize the array)</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LockCallbackDelegate(IntPtr opaque, ref IntPtr planes);

        /// <summary>
        /// Callback prototype to unlock a picture buffer. When the video frame decoding is complete, the unlock callback is invoked. This callback might not be needed at all. It is only an indication that the application can now read the pixel values if it needs to.
        /// </summary>
        /// <param name="opaque">Private pointer as passed to SetCallbacks()</param>
        /// <param name="picture">Private pointer returned from the LockCallback callback</param>
        /// <param name="planes">Pixel planes as defined by the @ref libvlc_video_lock_cb callback (this parameter is only for convenience)</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UnlockCallbackDelegate(IntPtr opaque, IntPtr picture, ref IntPtr planes);

        /// <summary>
        /// Callback prototype to display a picture. When the video frame needs to be shown, as determined by the media playback clock, the display callback is invoked.
        /// </summary>
        /// <param name="opaque">Private pointer as passed to SetCallbacks()</param>
        /// <param name="picture">Private pointer returned from the LockCallback callback</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DisplayCallbackDelegate(IntPtr opaque, IntPtr picture);

        private object GetDllType(Type type, string name)
        {
            IntPtr address = NativeMethods.GetProcAddress(libVlcDll, name);
            return address != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer(address, type) : null;
        }

        private void LoadLibVlcDynamic()
        {
            libvlcNew = (LibvlcNew)GetDllType(typeof(LibvlcNew), "libvlc_new");
            //_libvlc_get_version = (libvlc_get_version)GetDllType(typeof(libvlc_get_version), "libvlc_get_version");
            libvlcRelease = (LibvlcRelease)GetDllType(typeof(LibvlcRelease), "libvlc_release");

            libvlcMediaNewPath = (LibvlcMediaNewPath)GetDllType(typeof(LibvlcMediaNewPath), "libvlc_media_new_path");
            libvlcMediaPlayerNewFromMedia = (LibvlcMediaPlayerNewFromMedia)GetDllType(typeof(LibvlcMediaPlayerNewFromMedia), "libvlc_media_player_new_from_media");
            libvlcMediaRelease = (LibvlcMediaRelease)GetDllType(typeof(LibvlcMediaRelease), "libvlc_media_release");

            libvlcVideoGetSize = (LibvlcVideoGetSize)GetDllType(typeof(LibvlcVideoGetSize), "libvlc_video_get_size");
            libvlcAudioGetTrackCount = (LibvlcAudioGetTrackCount)GetDllType(typeof(LibvlcAudioGetTrackCount), "libvlc_audio_get_track_count");
            libvlcAudioGetTrack = (LibvlcAudioGetTrack)GetDllType(typeof(LibvlcAudioGetTrack), "libvlc_audio_get_track");
            libvlcAudioSetTrack = (LibvlcAudioSetTrack)GetDllType(typeof(LibvlcAudioSetTrack), "libvlc_audio_set_track");
            libvlcVideoTakeSnapshot = (LibvlcVideoTakeSnapshot)GetDllType(typeof(LibvlcVideoTakeSnapshot), "libvlc_video_take_snapshot");

            libvlcAudioGetVolume = (LibvlcAudioGetVolume)GetDllType(typeof(LibvlcAudioGetVolume), "libvlc_audio_get_volume");
            libvlcAudioSetVolume = (LibvlcAudioSetVolume)GetDllType(typeof(LibvlcAudioSetVolume), "libvlc_audio_set_volume");

            libvlcMediaPlayerPlay = (LibvlcMediaPlayerPlay)GetDllType(typeof(LibvlcMediaPlayerPlay), "libvlc_media_player_play");
            libvlcMediaPlayerStop = (LibvlcMediaPlayerStop)GetDllType(typeof(LibvlcMediaPlayerStop), "libvlc_media_player_stop");
            //_libvlc_media_player_pause = (libvlc_media_player_pause)GetDllType(typeof(libvlc_media_player_pause), "libvlc_media_player_pause");
            libvlcMediaPlayerSetHwnd = (LibvlcMediaPlayerSetHwnd)GetDllType(typeof(LibvlcMediaPlayerSetHwnd), "libvlc_media_player_set_hwnd");
            libvlcMediaPlayerIsPlaying = (LibvlcMediaPlayerIsPlaying)GetDllType(typeof(LibvlcMediaPlayerIsPlaying), "libvlc_media_player_is_playing");
            libvlcMediaPlayerSetPause = (LibvlcMediaPlayerSetPause)GetDllType(typeof(LibvlcMediaPlayerSetPause), "libvlc_media_player_set_pause");
            libvlcMediaPlayerGetTime = (LibvlcMediaPlayerGetTime)GetDllType(typeof(LibvlcMediaPlayerGetTime), "libvlc_media_player_get_time");
            libvlcMediaPlayerSetTime = (LibvlcMediaPlayerSetTime)GetDllType(typeof(LibvlcMediaPlayerSetTime), "libvlc_media_player_set_time");
            //_libvlc_media_player_get_fps = (libvlc_media_player_get_fps)GetDllType(typeof(libvlc_media_player_get_fps), "libvlc_media_player_get_fps");
            libvlcMediaPlayerGetState = (LibvlcMediaPlayerGetState)GetDllType(typeof(LibvlcMediaPlayerGetState), "libvlc_media_player_get_state");
            libvlcMediaPlayerGetLength = (LibvlcMediaPlayerGetLength)GetDllType(typeof(LibvlcMediaPlayerGetLength), "libvlc_media_player_get_length");
            libvlcMediaPlayerRelease = (LibvlcMediaPlayerRelease)GetDllType(typeof(LibvlcMediaPlayerRelease), "libvlc_media_player_release");
            libvlcMediaPlayerGetRate = (LibvlcMediaPlayerGetRate)GetDllType(typeof(LibvlcMediaPlayerGetRate), "libvlc_media_player_get_rate");
            libvlcMediaPlayerSetRate = (LibvlcMediaPlayerSetRate)GetDllType(typeof(LibvlcMediaPlayerSetRate), "libvlc_media_player_set_rate");
            libvlcMediaPlayerNextFrame = (LibvlcMediaPlayerNextFrame)GetDllType(typeof(LibvlcMediaPlayerNextFrame), "libvlc_media_player_next_frame");
            libvlcVideoSetSpu = (LibvlcVideoSetSpu)GetDllType(typeof(LibvlcVideoSetSpu), "libvlc_video_set_spu");
            libvlcVideoSetCallbacks = (LibvlcVideoSetCallbacks)GetDllType(typeof(LibvlcVideoSetCallbacks), "libvlc_video_set_callbacks");
            libvlcVideoSetFormat = (LibvlcVideoSetFormat)GetDllType(typeof(LibvlcVideoSetFormat), "libvlc_video_set_format");
            libvlcAudioGetDelay = (LibvlcAudioGetDelay)GetDllType(typeof(LibvlcAudioGetDelay), "libvlc_audio_get_delay");
        }

        private bool IsAllMethodsLoaded()
        {
            return libvlcNew != null &&
                //_libvlc_get_version != null &&
                   libvlcRelease != null &&
                   libvlcMediaNewPath != null &&
                   libvlcMediaPlayerNewFromMedia != null &&
                   libvlcMediaRelease != null &&
                   libvlcVideoGetSize != null &&
                   libvlcAudioGetVolume != null &&
                   libvlcAudioSetVolume != null &&
                   libvlcMediaPlayerPlay != null &&
                   libvlcMediaPlayerStop != null &&
                //_libvlc_media_player_pause != null &&
                   libvlcMediaPlayerSetHwnd != null &&
                   libvlcMediaPlayerIsPlaying != null &&
                   libvlcMediaPlayerGetTime != null &&
                   libvlcMediaPlayerSetTime != null &&
                //_libvlc_media_player_get_fps != null &&
                   libvlcMediaPlayerGetState != null &&
                   libvlcMediaPlayerGetLength != null &&
                   libvlcMediaPlayerRelease != null &&
                   libvlcMediaPlayerGetRate != null &&
                   libvlcMediaPlayerSetRate != null;
        }

        public static bool IsInstalled
        {
            get
            {
                using (var vlc = new LibVlcDynamic())
                {
                    vlc.Initialize(null, null, null, null);
                    return vlc.IsAllMethodsLoaded();
                }
            }
        }

        public override string PlayerName
        {
            get { return "VLC Lib Dynamic"; }
        }

        public override int Volume
        {
            get
            {
                return libvlcAudioGetVolume(mediaPlayer);
            }

            set
            {
                libvlcAudioSetVolume(mediaPlayer, value);
            }
        }

        public override double Duration
        {
            get
            {
                return libvlcMediaPlayerGetLength(mediaPlayer) / TimeCode.BaseUnit;
            }
        }

        public override double CurrentPosition
        {
            get
            {
                if (pausePosition == null)
                {
                    return libvlcMediaPlayerGetTime(mediaPlayer)/TimeCode.BaseUnit;
                }

                return pausePosition < 0 ? 0 : pausePosition.Value;
            }

            set
            {
                if (IsPaused && value <= Duration)
                {
                    pausePosition = value;
                }

                libvlcMediaPlayerSetTime(mediaPlayer, (long)(value * TimeCode.BaseUnit + 0.5));
            }
        }

        public override double PlayRate
        {
            get
            {
                return libvlcMediaPlayerGetRate(mediaPlayer);
            }

            set
            {
                if (value >= 0 && value <= 2.0)
                {
                    libvlcMediaPlayerSetRate(mediaPlayer, (float)value);
                }
            }
        }

        public void GetNextFrame()
        {
            libvlcMediaPlayerNextFrame(mediaPlayer);
        }

        public int VlcState
        {
            get
            {
                return libvlcMediaPlayerGetState(mediaPlayer);
            }
        }

        public override void Play()
        {
            libvlcMediaPlayerPlay(mediaPlayer);
            pausePosition = null;
        }

        public override void Pause()
        {
            int i = 0;
            libvlcMediaPlayerSetPause(mediaPlayer, 1);
            int state = VlcState;
            while (state != 4 && i < 50)
            {
                System.Threading.Thread.Sleep(10);
                i++;
                state = VlcState;
            }

            libvlcMediaPlayerSetPause(mediaPlayer, 1);
        }

        public override void Stop()
        {
            libvlcMediaPlayerStop(mediaPlayer);
            pausePosition = null;
        }

        public override bool IsPaused
        {
            get
            {
                const int paused = 4;
                int state = libvlcMediaPlayerGetState(mediaPlayer);
                return state == paused;
            }
        }

        public override bool IsPlaying
        {
            get
            {
                const int playing = 3;
                int state = libvlcMediaPlayerGetState(mediaPlayer);
                return state == playing;
            }
        }

        public int AudioTrackCount
        {
            get
            {
                return libvlcAudioGetTrackCount(mediaPlayer) - 1;
            }
        }

        public int AudioTrackNumber
        {
            get
            {
                return libvlcAudioGetTrack(mediaPlayer) - 1;
            }
            set
            {
                libvlcAudioSetTrack(mediaPlayer, value + 1);
            }
        }

        /// <summary>
        /// Audio delay in milliseconds
        /// </summary>
        public long AudioDelay
        {
            get
            {
                return libvlcAudioGetDelay(mediaPlayer) / 1000; // converts microseconds to milliseconds
            }
        }

        public bool TakeSnapshot(string fileName, uint width, uint height)
        {
            if (libvlcVideoTakeSnapshot == null)
            {
                return false;
            }

            return libvlcVideoTakeSnapshot(mediaPlayer, 0, Encoding.UTF8.GetBytes(fileName + "\0"), width, height) == 1;
        }

        public LibVlcDynamic MakeSecondMediaPlayer(Control ownerControl, string videoFileName, EventHandler onVideoLoaded, EventHandler onVideoEnded)
        {
            var newVlc = new LibVlcDynamic { libVlc = libVlc, libVlcDll = libVlcDll, ownerControl = ownerControl };
            if (ownerControl != null)
            {
                newVlc.parentForm = ownerControl.FindForm();
            }

            newVlc.LoadLibVlcDynamic();

            newVlc.OnVideoLoaded = onVideoLoaded;
            newVlc.OnVideoEnded = onVideoEnded;

            if (string.IsNullOrEmpty(videoFileName))
            {
                return newVlc;
            }

            IntPtr media = libvlcMediaNewPath(libVlc, Encoding.UTF8.GetBytes(videoFileName + "\0"));
            newVlc.mediaPlayer = libvlcMediaPlayerNewFromMedia(media);
            libvlcMediaRelease(media);

            //  Linux: libvlc_media_player_set_xdrawable (_mediaPlayer, xdrawable);
            //  Mac: libvlc_media_player_set_nsobject (_mediaPlayer, view);
            if (ownerControl != null)
            {
                libvlcMediaPlayerSetHwnd(newVlc.mediaPlayer, ownerControl.Handle); // windows
            }

            if (onVideoEnded != null)
            {
                newVlc.videoEndTimer = new Timer { Interval = 500 };
                newVlc.videoEndTimer.Tick += VideoEndTimerTick;
                newVlc.videoEndTimer.Start();
            }

            libvlcMediaPlayerPlay(newVlc.mediaPlayer);
            newVlc.videoLoadedTimer = new Timer { Interval = 100 };
            newVlc.videoLoadedTimer.Tick += newVlc.VideoLoadedTimer_Tick;
            newVlc.videoLoadedTimer.Start();

            newVlc.mouseTimer = new Timer { Interval = 25 };
            newVlc.mouseTimer.Tick += newVlc.MouseTimerTick;
            newVlc.mouseTimer.Start();
            return newVlc;
        }

        private void VideoLoadedTimer_Tick(object sender, EventArgs e)
        {
            videoLoadedTimer.Stop();
            int i = 0;
            while (!IsPlaying && i < 50)
            {
                System.Threading.Thread.Sleep(100);
                i++;
            }
            Pause();
            if (libvlcVideoSetSpu != null)
            {
                libvlcVideoSetSpu(mediaPlayer, -1); // turn of embedded subtitles
            }

            if (OnVideoLoaded != null)
            {
                OnVideoLoaded.Invoke(mediaPlayer, new EventArgs());
            }
        }

        public static string GetVlcPath(string fileName)
        {
            if (Configuration.IsRunningOnLinux() || Configuration.IsRunningOnMac())
            {
                return null;
            }

            var path = Path.Combine(Configuration.BaseDirectory, @"VLC\" + fileName);
            if (File.Exists(path))
            {
                return path;
            }

            if (!string.IsNullOrEmpty(Configuration.Settings.General.VlcLocation))
            {
                if (Configuration.Settings.General.VlcLocation.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    Configuration.Settings.General.VlcLocation = Path.GetDirectoryName(Configuration.Settings.General.VlcLocation);
                }

                path = Path.Combine(Configuration.Settings.General.VlcLocation, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            if (!string.IsNullOrEmpty(Configuration.Settings.General.VlcLocationRelative))
            {
                try
                {
                    path = Configuration.Settings.General.VlcLocationRelative;
                    if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        path = Path.GetDirectoryName(path);
                    }

                    if (path != null)
                    {
                        path = Path.Combine(path, fileName);

                        string path2 = Path.GetFullPath(path);
                        if (File.Exists(path2))
                        {
                            return path2;
                        }

                        while (path.StartsWith(".."))
                        {
                            path = path.Remove(0, 3);
                            path2 = Path.GetFullPath(path);
                            if (File.Exists(path2))
                            {
                                return path2;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            // XP via registry path
            path = RegistryUtil.GetValue(@"SOFTWARE\VideoLAN\VLC", "InstallDir");
            if (path != null && Directory.Exists(path))
            {
                path = Path.Combine(path, fileName);
            }

            if (File.Exists(path))
            {
                return path;
            }

            // Winows 7 via registry path
            path = RegistryUtil.GetValue(@"SOFTWARE\Wow6432Node\VideoLAN\VLC", "InstallDir");
            if (path != null && Directory.Exists(path))
            {
                path = Path.Combine(path, fileName);
            }

            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(@"C:\Program Files (x86)\VideoLAN\VLC", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(@"C:\Program Files\VideoLAN\VLC", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(@"C:\Program Files (x86)\VLC", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(@"C:\Program Files\VLC", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"VideoLAN\VLC\" + fileName);
            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"VLC\" + fileName);
            return File.Exists(path) ? path : null;
        }

        public bool InitializeAndStartFrameGrabbing(string videoFileName,
                                                    UInt32 width, UInt32 height,
                                                    LockCallbackDelegate @lock,
                                                    UnlockCallbackDelegate unlock,
                                                    DisplayCallbackDelegate display,
                                                    IntPtr opaque)
        {
            string dllFile = GetVlcPath("libvlc.dll");
            if (!File.Exists(dllFile) || string.IsNullOrEmpty(videoFileName))
            {
                return false;
            }

            if (dllFile != null)
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(dllFile));
                libVlcDll = NativeMethods.LoadLibrary(dllFile);
            }

            LoadLibVlcDynamic();
            string[] initParameters = { "--no-skip-frames" };
            libVlc = libvlcNew(initParameters.Length, initParameters);
            IntPtr media = libvlcMediaNewPath(libVlc, Encoding.UTF8.GetBytes(videoFileName + "\0"));
            mediaPlayer = libvlcMediaPlayerNewFromMedia(media);
            libvlcMediaRelease(media);

            libvlcVideoSetFormat(mediaPlayer, "RV24", width, height, 3 * width);
            //            _libvlc_video_set_format(_mediaPlayer,"RV32", width, height, 4 * width);

            //_libvlc_video_set_callbacks(_mediaPlayer, @lock, unlock, display, opaque);
            libvlcVideoSetCallbacks(mediaPlayer, @lock, unlock, display, opaque);
            libvlcAudioSetVolume(mediaPlayer, 0);
            libvlcMediaPlayerSetRate(mediaPlayer, 9f);
            //_libvlc_media_player_play(_mediaPlayer);
            return true;
        }

        public override void Initialize(Control ownerControl, string videoFileName, EventHandler onVideoLoaded, EventHandler onVideoEnded)
        {
            this.ownerControl = ownerControl;
            if (ownerControl != null)
            {
                parentForm = ownerControl.FindForm();
            }

            string dllFile = GetVlcPath("libvlc.dll");
            if (File.Exists(dllFile))
            {
                if (dllFile != null)
                {
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(dllFile));
                    libVlcDll = NativeMethods.LoadLibrary(dllFile);
                }

                LoadLibVlcDynamic();
            }
            else if (!Directory.Exists(videoFileName))
            {
                return;
            }

            OnVideoLoaded = onVideoLoaded;
            OnVideoEnded = onVideoEnded;

            if (string.IsNullOrEmpty(videoFileName))
            {
                return;
            }

            string[] initParameters = { "--no-sub-autodetect-file" }; // , "--ffmpeg-hw" }; //, "--no-video-title-show" }; // TODO: Put in options/config file
            libVlc = libvlcNew(initParameters.Length, initParameters);
            IntPtr media = libvlcMediaNewPath(libVlc, Encoding.UTF8.GetBytes(videoFileName + "\0"));
            mediaPlayer = libvlcMediaPlayerNewFromMedia(media);
            libvlcMediaRelease(media);

            //  Linux: libvlc_media_player_set_xdrawable (_mediaPlayer, xdrawable);
            //  Mac: libvlc_media_player_set_nsobject (_mediaPlayer, view);

            if (ownerControl != null)
            {
                libvlcMediaPlayerSetHwnd(mediaPlayer, ownerControl.Handle); // windows

                //hack: sometimes vlc opens in it's own windows - this code seems to prevent this
                for (int j = 0; j < 50; j++)
                {
                    System.Threading.Thread.Sleep(10);
                    Application.DoEvents();
                }

                libvlcMediaPlayerSetHwnd(mediaPlayer, ownerControl.Handle); // windows
            }

            if (onVideoEnded != null)
            {
                videoEndTimer = new Timer { Interval = 500 };
                videoEndTimer.Tick += VideoEndTimerTick;
                videoEndTimer.Start();
            }

            libvlcMediaPlayerPlay(mediaPlayer);
            videoLoadedTimer = new Timer { Interval = 100 };
            videoLoadedTimer.Tick += VideoLoadedTimer_Tick;
            videoLoadedTimer.Start();

            mouseTimer = new Timer { Interval = 25 };
            mouseTimer.Tick += MouseTimerTick;
            mouseTimer.Start();
        }

        public static bool IsLeftMouseButtonDown()
        {
            const int keyPressed = 0x8000;
            const int vkLbutton = 0x1;
            return Convert.ToBoolean(NativeMethods.GetKeyState(vkLbutton) & keyPressed);
        }

        private void MouseTimerTick(object sender, EventArgs e)
        {
            mouseTimer.Stop();
            if (parentForm != null && ownerControl != null && ownerControl.Visible && parentForm.ContainsFocus && IsLeftMouseButtonDown())
            {
                var p = ownerControl.PointToClient(Control.MousePosition);
                if (p.X > 0 && p.X < ownerControl.Width && p.Y > 0 && p.Y < ownerControl.Height)
                {
                    if (IsPlaying)
                    {
                        Pause();
                    }
                    else
                    {
                        Play();
                    }

                    int i = 0;
                    while (IsLeftMouseButtonDown() && i < 200)
                    {
                        System.Threading.Thread.Sleep(2);
                        Application.DoEvents();
                        i++;
                    }
                }
            }

            mouseTimer.Start();
        }

        private void VideoEndTimerTick(object sender, EventArgs e)
        {
            const int ended = 6;
            int state = libvlcMediaPlayerGetState(mediaPlayer);
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
            Dispose();
        }

        public override event EventHandler OnVideoLoaded;

        public override event EventHandler OnVideoEnded;

        private void ReleaseUnmangedResources()
        {
            try
            {
                lock (this)
                {
                    if (mediaPlayer != IntPtr.Zero)
                    {
                        libvlcMediaPlayerStop(mediaPlayer);
                        //_libvlc_media_player_release(_mediaPlayer); // CRASHES in visual sync / point sync!
                        mediaPlayer = IntPtr.Zero;
                        //_libvlc_media_list_player_release(_mediaPlayer);
                    }

                    if (libvlcRelease == null || libVlc == IntPtr.Zero)
                    {
                        return;
                    }

                    libvlcRelease(libVlc);
                    libVlc = IntPtr.Zero;

                    //if (_libVlcDLL != IntPtr.Zero)
                    //{
                    //    FreeLibrary(_libVlcDLL);  // CRASHES in visual sync / point sync!
                    //    _libVlcDLL = IntPtr.Zero;
                    //}
                }
            }
            catch
            {
            }
        }

        ~LibVlcDynamic()
        {
            Dispose(false);
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

                if (mouseTimer != null)
                {
                    mouseTimer.Dispose();
                    mouseTimer = null;
                }
            }

            ReleaseUnmangedResources();
        }
    }
}
