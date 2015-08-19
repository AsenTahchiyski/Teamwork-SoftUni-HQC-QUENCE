namespace Nikse.SubtitleEdit.Logic.VideoPlayers.MpcHC
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;
    using Core;

    public class MpcHc : VideoPlayer, IDisposable
    {

        private const string ModePlay = "0";
        private const string ModePause = "1";
        private string playMode = string.Empty;
        //private const string StateLoaded = "2";
        private int loaded;
        private IntPtr mpcHandle = IntPtr.Zero;
        private IntPtr videoHandle = IntPtr.Zero;
        private IntPtr videoPanelHandle = IntPtr.Zero;
        private ProcessStartInfo startInfo;
        private Process process;
        private IntPtr messageHandlerHandle = IntPtr.Zero;
        private string videoFileName;
        private Timer positionTimer;
        private double positionInSeconds;
        private double durationInSeconds;
        private MessageHandlerWindow form;
        private int initialWidth;
        private int initialHeight;
        private const int volume = 75;

        public override event EventHandler OnVideoLoaded;
        public override event EventHandler OnVideoEnded;

        public override string PlayerName
        {
            get { return "MPC-HC"; }
        }

        public override int Volume
        {
            get
            {
                return volume;
            }
            set
            {
                // MPC-HC moves from 0-100 in steps of 5
                for (int i = 0; i < 100; i += 5)
                {
                    SendMpcMessage(MpcHcCommand.DecreaseVolume);
                }

                for (int i = 0; i < value; i += 5)
                {
                    SendMpcMessage(MpcHcCommand.IncreaseVolume);
                }
            }
        }

        public override double Duration
        {
            get
            {
                return durationInSeconds;
            }
        }

        public override double CurrentPosition
        {
            get
            {
                return positionInSeconds;
            }

            set
            {
                SendMpcMessage(MpcHcCommand.SetPosition, string.Format(CultureInfo.InvariantCulture, "{0:0.000}", value));
            }
        }

        public override void Play()
        {
            playMode = ModePlay;
            SendMpcMessage(MpcHcCommand.Play);
        }

        public override void Pause()
        {
            playMode = ModePause;
            SendMpcMessage(MpcHcCommand.Pause);
        }

        public override void Stop()
        {
            SendMpcMessage(MpcHcCommand.Stop);
        }

        public override bool IsPaused
        {
            get { return playMode == ModePause; }
        }

        public override bool IsPlaying
        {
            get { return playMode == ModePlay; }
        }

        public override void Initialize(Control ownerControl, string videoFileName, EventHandler onVideoLoaded, EventHandler onVideoEnded)
        {
            if (ownerControl == null)
            {
                return;
            }

            VideoFileName = videoFileName;
            OnVideoLoaded = onVideoLoaded;
            OnVideoEnded = onVideoEnded;

            initialWidth = ownerControl.Width;
            initialHeight = ownerControl.Height;
            form = new MessageHandlerWindow();
            form.OnCopyData += OnCopyData;
            form.Show();
            form.Hide();
            videoPanelHandle = ownerControl.Handle;
            messageHandlerHandle = form.Handle;
            this.videoFileName = videoFileName;
            startInfo = new ProcessStartInfo
            {
                FileName = GetMpcHcFileName(),
                Arguments = "/new /minimized /slave " + messageHandlerHandle
            };

            process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForInputIdle();
            }

            positionTimer = new Timer { Interval = 100 };
            positionTimer.Tick += PositionTimerTick;
        }

        private void PositionTimerTick(object sender, EventArgs e)
        {
            SendMpcMessage(MpcHcCommand.GetCurrentPosition);
        }

        private void OnCopyData(object sender, EventArgs e)
        {
            var message = (Message)sender;
            var cds = (NativeMethods.CopyDataStruct)Marshal.PtrToStructure(message.LParam, typeof(NativeMethods.CopyDataStruct));
            //var command = cds.dwData.ToUInt32();
            var param = Marshal.PtrToStringAuto(cds.LpData);
            if (param == null)
            {
                return;
            }

            var multiParam = param.Split('|');

            switch (cds.DwData.ToUInt32())
            {
                case MpcHcCommand.Connect:
                    positionTimer.Stop();
                    mpcHandle = (IntPtr)Convert.ToInt64(Marshal.PtrToStringAuto(cds.LpData));
                    SendMpcMessage(MpcHcCommand.OpenFile, videoFileName);
                    positionTimer.Start();
                    break;
                case MpcHcCommand.PlayMode:
                    playMode = param;
                    if (param == ModePlay && loaded == 0)
                    {
                        loaded = 1;
                        if (!HijackMpcHc())
                        {
                            Application.DoEvents();
                            HijackMpcHc();
                        }
                    }

                    break;
                case MpcHcCommand.NowPlaying:
                    if (loaded == 1)
                    {
                        loaded = 2;
                        durationInSeconds = double.Parse(multiParam[4], CultureInfo.InvariantCulture);
                        Pause();
                        Resize(initialWidth, initialHeight);
                        if (OnVideoLoaded != null)
                        {
                            OnVideoLoaded.Invoke(this, new EventArgs());
                        }

                        SendMpcMessage(MpcHcCommand.SetSubtitleTrack, "-1");
                    }

                    break;
                case MpcHcCommand.NotifyEndOfStream:
                    if (OnVideoEnded != null)
                    {
                        OnVideoEnded.Invoke(this, new EventArgs());
                    }

                    break;
                case MpcHcCommand.CurrentPosition:
                    positionInSeconds = double.Parse(param, CultureInfo.InvariantCulture);
                    break;
            }
        }

        internal static bool GetWindowHandle(IntPtr windowHandle, ArrayList windowHandles)
        {
            windowHandles.Add(windowHandle);
            return true;
        }

        private ArrayList GetChildWindows()
        {
            var windowHandles = new ArrayList();
            NativeMethods.EnumedWindow callBackPtr = GetWindowHandle;
            NativeMethods.EnumChildWindows(process.MainWindowHandle, callBackPtr, windowHandles);
            return windowHandles;
        }

        private static bool IsWindowMpcHcVideo(IntPtr hWnd)
        {
            var className = new StringBuilder(256);
            int returnCode = NativeMethods.GetClassName(hWnd, className, className.Capacity); // Get the window class name
            return returnCode != 0 && (className.ToString().EndsWith(":b:0000000000010003:0000000000000006:0000000000000000"));
        }

        private bool HijackMpcHc()
        {
            IntPtr handle = process.MainWindowHandle;
            var handles = GetChildWindows();
            foreach (var h in handles)
            {
                if (!IsWindowMpcHcVideo((IntPtr)h))
                {
                    continue;
                }

                videoHandle = (IntPtr)h;
                NativeMethods.SetParent((IntPtr)h, videoPanelHandle);
                NativeMethods.SetWindowPos(handle, (IntPtr)NativeMethods.SpecialWindowHandles.HwndTop, -9999, -9999, 0, 0, NativeMethods.SetWindowPosFlags.SwpNoactivate);
                return true;
            }

            return false;
        }

        public override void Resize(int width, int height)
        {
            NativeMethods.ShowWindow(process.MainWindowHandle, NativeMethods.ShowWindowCommands.ShowNoActivate);
            NativeMethods.SetWindowPos(videoHandle, (IntPtr)NativeMethods.SpecialWindowHandles.HwndTop, 0, 0, width, height, NativeMethods.SetWindowPosFlags.SwpNoreposition);
            NativeMethods.ShowWindow(process.MainWindowHandle, NativeMethods.ShowWindowCommands.Hide);
        }

        public static string GetMpcHcFileName()
        {
            string path;

            if (IntPtr.Size == 8) // 64-bit
            {
                path = Path.Combine(Configuration.BaseDirectory, @"MPC-HC\mpc-hc64.exe");
                if (File.Exists(path))
                {
                    return path;
                }

                if (!string.IsNullOrEmpty(Configuration.Settings.General.MpcHcLocation))
                {
                    path = Path.GetDirectoryName(Configuration.Settings.General.MpcHcLocation);
                    if (path != null && (File.Exists(path) && path.EndsWith("mpc-hc64.exe", StringComparison.OrdinalIgnoreCase)))
                    {
                        return path;
                    }

                    if (Directory.Exists(Configuration.Settings.General.MpcHcLocation))
                    {
                        path = Path.Combine(Configuration.Settings.General.MpcHcLocation, @"MPC-HC\mpc-hc64.exe");
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }

                path = RegistryUtil.GetValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{2ACBF1FA-F5C3-4B19-A774-B22A31F231B9}_is1", "InstallLocation");
                if (path != null)
                {
                    path = Path.Combine(path, "mpc-hc64.exe");
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"MPC-HC\mpc-hc64.exe");
                if (File.Exists(path))
                {
                    return path;
                }

                path = @"C:\Program Files\MPC-HC\mpc-hc64.exe";
                if (File.Exists(path))
                {
                    return path;
                }

                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"K-Lite Codec Pack\MPC-HC\mpc-hc64.exe");
                if (File.Exists(path))
                {
                    return path;
                }

                path = @"C:\Program Files (x86)\K-Lite Codec Pack\MPC-HC64\mpc-hc64.exe";
                if (File.Exists(path))
                {
                    return path;
                }

                path = @"C:\Program Files (x86)\MPC-HC\mpc-hc64.exe";
                if (File.Exists(path))
                {
                    return path;
                }
            }
            else
            {
                path = Path.Combine(Configuration.BaseDirectory, @"MPC-HC\mpc-hc.exe");
                if (File.Exists(path))
                {
                    return path;
                }

                if (!string.IsNullOrEmpty(Configuration.Settings.General.MpcHcLocation))
                {
                    path = Path.GetDirectoryName(Configuration.Settings.General.MpcHcLocation);
                    if (path != null && (File.Exists(path) && path.EndsWith("mpc-hc.exe", StringComparison.OrdinalIgnoreCase)))
                    {
                        return path;
                    }

                    if (Directory.Exists(Configuration.Settings.General.MpcHcLocation))
                    {
                        path = Path.Combine(Configuration.Settings.General.MpcHcLocation, @"MPC-HC\mpc-hc.exe");
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }

                path = RegistryUtil.GetValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{2624B969-7135-4EB1-B0F6-2D8C397B45F7}_is1", "InstallLocation");
                if (path != null)
                {
                    path = Path.Combine(path, "mpc-hc.exe");
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"MPC-HC\mpc-hc.exe");
                if (File.Exists(path))
                {
                    return path;
                }

                path = @"C:\Program Files (x86)\MPC-HC\mpc-hc.exe";
                if (File.Exists(path))
                {
                    return path;
                }

                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"K-Lite Codec Pack\MPC-HC\mpc-hc.exe");
                if (File.Exists(path))
                {
                    return path;
                }

                path = @"C:\Program Files\MPC-HC\mpc-hc.exe";
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        public static bool IsInstalled
        {
            get { return true; }
        }

        public override void DisposeVideoPlayer()
        {
            Dispose();
        }

        private void ReleaseUnmangedResources()
        {
            try
            {
                lock (this)
                {
                    if (mpcHandle == IntPtr.Zero)
                    {
                        return;
                    }

                    SendMpcMessage(MpcHcCommand.CloseApplication);
                    mpcHandle = IntPtr.Zero;
                }
            }
            catch
            {
            }
        }

        ~MpcHc()
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
            try
            {
                if (disposing)
                {
                    // release managed resources
                    if (positionTimer != null)
                    {
                        positionTimer.Stop();
                        positionTimer.Dispose();
                        positionTimer = null;
                    }

                    if (form != null)
                    {
                        form.OnCopyData -= OnCopyData;
                        //_form.Dispose(); this gives an error when doing File -> Exit...
                        form = null;
                    }

                    if (process != null)
                    {
                        process.Dispose();
                        process = null;
                    }

                    startInfo = null;
                }

                ReleaseUnmangedResources();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void SendMpcMessage(uint command)
        {
            SendMpcMessage(command, string.Empty);
        }

        private void SendMpcMessage(uint command, string parameter)
        {
            if (mpcHandle == IntPtr.Zero || messageHandlerHandle == IntPtr.Zero)
            {
                return;
            }

            parameter += (char)0;
            NativeMethods.CopyDataStruct cds;
            cds.DwData = (UIntPtr)command;
            cds.CbData = parameter.Length * Marshal.SystemDefaultCharSize;
            cds.LpData = Marshal.StringToCoTaskMemAuto(parameter);
            NativeMethods.SendMessage(mpcHandle, NativeMethods.WindowsMessageCopyData, messageHandlerHandle, ref cds);
        }
    }
}
