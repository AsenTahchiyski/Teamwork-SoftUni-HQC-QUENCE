namespace Nikse.SubtitleEdit
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Forms;

    using Nikse.SubtitleEdit.Forms;

    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Add the event handler for handling UI thread exceptions to the event.
            Application.ThreadException += ApplicationThreadException;

            // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }

        private static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (!(e.Exception is CultureNotFoundException))
            {
                // To avoid error when changing language (on some computers) - see https://github.com/SubtitleEdit/subtitleedit/issues/719
                ShowThreadExceptionDialog("Unhandled exception in SubtitleEdit.exe", e.Exception);
            }
        }

        // Handle the UI exceptions by showing a dialog box, and asking the user whether
        // or not they wish to abort execution.
        // NOTE: This exception cannot be kept from terminating the application - it can only
        // log the event, and inform the user about it.
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = (Exception)e.ExceptionObject;
                const string ErrorMsg = "An error occurred in Subtitle Edit. Please contact the administrator with the following information:\n\n";

                // Since we can't prevent the app from terminating, log this to the event log.
                if (!EventLog.SourceExists("ThreadException"))
                {
                    EventLog.CreateEventSource("ThreadException", "Application");
                }

                // Create an EventLog instance and assign its source.
                using (var eventLog = new EventLog { Source = "ThreadException" })
                {
                    eventLog.WriteEntry(ErrorMsg + ex.Message + "\n\nStack Trace:\n" + ex.StackTrace);
                }
            }
            catch (Exception exc)
            {
                try
                {
                    MessageBox.Show("Fatal Non-UI Error in Subtitle Edit", "Fatal Non-UI Error. Could not write the error to the event log. Reason: " + exc.Message, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }
        }

        private static void ShowThreadExceptionDialog(string title, Exception e)
        {
            var errorMsg = "An application error occurred. Please contact the administrator with the following information:\n\n" + e.Message + "\n\nStack Trace:\n" + e.StackTrace;
            MessageBox.Show(errorMsg, title, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
        }
    }
}