namespace Nikse.SubtitleEdit.Logic.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;

    public class NikseWebServiceSession : IDisposable
    {
        public class ChatEntry
        {
            public SeNetworkService.SeUser User { get; set; }
           
            public string Message { get; set; }
        }

        private System.Windows.Forms.Timer timerWebService;
        private SeNetworkService.SeService seWs;
        private DateTime seWsLastUpdate = DateTime.Now.AddYears(-1);
        private string fileName;
        private string userName;
        private readonly StringBuilder log;

        public event EventHandler OnUpdateTimerTick;
        public event EventHandler OnUpdateUserLogEntries;
        public List<UpdateLogEntry> UpdateLog = new List<UpdateLogEntry>();
        public List<ChatEntry> ChatLog = new List<ChatEntry>(); 
       
        public Subtitle LastSubtitle { get; set; }
     
        public Subtitle Subtitle { get; set; }
        
        public Subtitle OriginalSubtitle { get; set; }
        
        public string SessionId { get; set; }

        public List<SeNetworkService.SeUser> Users { get; set; }
        
        public SeNetworkService.SeUser CurrentUser { get; set; }

        public string WebServiceUrl
        {
            get
            {
                return seWs.Url;
            }
        }

        public NikseWebServiceSession(Subtitle subtitle, Subtitle originalSubtitle, EventHandler onUpdateTimerTick, EventHandler onUpdateUserLogEntries)
        {
            Subtitle = subtitle;
            OriginalSubtitle = originalSubtitle;
            timerWebService = new System.Windows.Forms.Timer();
            if (Configuration.Settings.NetworkSettings.PollIntervalSeconds < 1)
            {
                Configuration.Settings.NetworkSettings.PollIntervalSeconds = 1;
            }

            timerWebService.Interval = Configuration.Settings.NetworkSettings.PollIntervalSeconds * 1000;
            timerWebService.Tick += TimerWebServiceTick;
            log = new StringBuilder();
            OnUpdateTimerTick = onUpdateTimerTick;
            OnUpdateUserLogEntries = onUpdateUserLogEntries;
        }

        public void StartServer(string webServiceUrl, string sessionKey, string userName, string fileName, out string message)
        {
            SessionId = sessionKey;
            this.userName = userName;
            this.fileName = fileName;
            var list = new List<SeNetworkService.SeSequence>();
            foreach (Paragraph p in Subtitle.Paragraphs)
            {
                list.Add(new SeNetworkService.SeSequence
                {
                    StartMilliseconds = (int)p.StartTime.TotalMilliseconds,
                    EndMilliseconds = (int)p.EndTime.TotalMilliseconds,
                    Text = WebUtility.HtmlEncode(p.Text.Replace(Environment.NewLine, "<br />"))
                });
            }

            var originalSubtitle = new List<SeNetworkService.SeSequence>();
            if (OriginalSubtitle != null)
            {
                foreach (Paragraph p in OriginalSubtitle.Paragraphs)
                {
                    originalSubtitle.Add(new SeNetworkService.SeSequence
                    {
                        StartMilliseconds = (int)p.StartTime.TotalMilliseconds,
                        EndMilliseconds = (int)p.EndTime.TotalMilliseconds,
                        Text = WebUtility.HtmlEncode(p.Text.Replace(Environment.NewLine, "<br />"))
                    });
                }
            }

            seWs = new SeNetworkService.SeService();
            seWs.Url = webServiceUrl;
            seWs.Proxy = Utilities.GetProxy();
            SeNetworkService.SeUser user = seWs.Start(sessionKey, userName, list.ToArray(), originalSubtitle.ToArray(), fileName, out message);
            CurrentUser = user;
            Users = new List<SeNetworkService.SeUser>();
            Users.Add(user);
            if (message == "OK")
            {
                timerWebService.Start();
            }
        }

        public bool Join(string webServiceUrl, string userName, string sessionKey, out string message)
        {
            SessionId = sessionKey;
            seWs = new SeNetworkService.SeService
            {
                Url = webServiceUrl,
                Proxy = Utilities.GetProxy()
            };

            Users = new List<SeNetworkService.SeUser>();
            var users = seWs.Join(sessionKey, userName, out message);
            if (message != "OK")
            {
                return false;
            }

            string tempFileName;
            DateTime updateTime;
            Subtitle = new Subtitle();
            foreach (var sequence in seWs.GetSubtitle(sessionKey, out tempFileName, out updateTime))
            {
                Subtitle.Paragraphs.Add(new Paragraph(WebUtility.HtmlDecode(sequence.Text).Replace("<br />", Environment.NewLine), sequence.StartMilliseconds, sequence.EndMilliseconds));
            }

            fileName = tempFileName;

            OriginalSubtitle = new Subtitle();
            var sequences = seWs.GetOriginalSubtitle(sessionKey);
            if (sequences != null)
            {
                foreach (var sequence in sequences)
                {
                    OriginalSubtitle.Paragraphs.Add(new Paragraph(WebUtility.HtmlDecode(sequence.Text).Replace("<br />", Environment.NewLine), sequence.StartMilliseconds, sequence.EndMilliseconds));
                }
            }

            SessionId = sessionKey;
            CurrentUser = users[users.Length - 1]; // me
            foreach (var user in users)
            {
                Users.Add(user);
            }

            ReloadFromWs();
            timerWebService.Start();
            return true;
        }

        private void TimerWebServiceTick(object sender, EventArgs e)
        {
            if (OnUpdateTimerTick != null)
            {
                OnUpdateTimerTick.Invoke(sender, e);
            }
        }

        public void TimerStop()
        {
            timerWebService.Stop();
        }

        public void TimerStart()
        {
            timerWebService.Start();
        }

        public List<SeNetworkService.SeUpdate> GetUpdates(out string message, out int numberOfLines)
        {
            List<SeNetworkService.SeUpdate> list = new List<SeNetworkService.SeUpdate>();
            DateTime newUpdateTime;
            var updates = seWs.GetUpdates(SessionId, CurrentUser.UserName, seWsLastUpdate, out message, out newUpdateTime, out numberOfLines);
            if (updates != null)
            {
                list.AddRange(updates);
            }

            seWsLastUpdate = newUpdateTime;
            return list;
        }

        public Subtitle ReloadSubtitle()
        {
            Subtitle.Paragraphs.Clear();
            string tempFileName;
            DateTime updateTime;
            var sequences = seWs.GetSubtitle(SessionId, out tempFileName, out updateTime);
            fileName = tempFileName;
            seWsLastUpdate = updateTime;
            if (sequences == null)
            {
                return Subtitle;
            }

            foreach (var sequence in sequences)
            {
                Subtitle.Paragraphs.Add(new Paragraph(WebUtility.HtmlDecode(sequence.Text).Replace("<br />", Environment.NewLine), sequence.StartMilliseconds, sequence.EndMilliseconds));
            }

            return Subtitle;
        }

        private void ReloadFromWs()
        {
            if (seWs == null)
            {
                return;
            }

            Subtitle = new Subtitle();
            var sequences = seWs.GetSubtitle(SessionId, out fileName, out seWsLastUpdate);
            foreach (var sequence in sequences)
            {
                Paragraph paragraph = new Paragraph(WebUtility.HtmlDecode(sequence.Text).Replace("<br />", Environment.NewLine), 
                    sequence.StartMilliseconds, sequence.EndMilliseconds);
                Subtitle.Paragraphs.Add(paragraph);
            }

            Subtitle.Renumber();
            LastSubtitle = new Subtitle(Subtitle);
        }

        public void AppendToLog(string text)
        {
            string timestamp = DateTime.Now.ToLongTimeString();
            log.AppendLine(timestamp + ": " + text.TrimEnd().Replace(Environment.NewLine, Configuration.Settings.General.ListViewLineSeparatorString));
        }

        public string GetLog()
        {
            return log.ToString();
        }

        public void SendChatMessage(string message)
        {
            seWs.SendMessage(SessionId, WebUtility.HtmlEncode(message.Replace(Environment.NewLine, "<br />")), CurrentUser);
        }

        internal void UpdateLine(int index, Paragraph paragraph)
        {
            seWs.UpdateLine(SessionId, index, new SeNetworkService.SeSequence
            {
                StartMilliseconds = (int)paragraph.StartTime.TotalMilliseconds,
                EndMilliseconds = (int)paragraph.EndTime.TotalMilliseconds,
                Text = WebUtility.HtmlEncode(paragraph.Text.Replace(Environment.NewLine, "<br />"))
            }, CurrentUser);
            AddToWsUserLog(CurrentUser, index, "UPD", true);
        }

        public void CheckForAndSubmitUpdates()
        {
            // check for changes in text/time codes (not insert/delete)
            if (LastSubtitle == null)
            {
                return;
            }

            for (int i = 0; i < Subtitle.Paragraphs.Count; i++)
            {
                Paragraph last = LastSubtitle.GetParagraphOrDefault(i);
                Paragraph current = Subtitle.GetParagraphOrDefault(i);
                if (last == null || current == null)
                {
                    continue;
                }

                if (last.StartTime.TotalMilliseconds != current.StartTime.TotalMilliseconds ||
                    last.EndTime.TotalMilliseconds != current.EndTime.TotalMilliseconds ||
                    last.Text != current.Text)
                {
                    UpdateLine(i, current);
                }
            }
        }

        public void AddToWsUserLog(SeNetworkService.SeUser user, int pos, string action, bool updateUI)
        {
            for (int i = 0; i < UpdateLog.Count; i++)
            {
                if (UpdateLog[i].Index != pos)
                {
                    continue;
                }

                UpdateLog.RemoveAt(i);
                break;
            }

            UpdateLog.Add(new UpdateLogEntry(0, user.UserName, pos, action));
            if (updateUI && OnUpdateUserLogEntries != null)
            {
                OnUpdateUserLogEntries.Invoke(null, null);
            }
        }

        internal void Leave()
        {
            try
            {
                seWs.Leave(SessionId, CurrentUser.UserName);
            }
            catch
            {
            }
        }

        internal void DeleteLines(List<int> indices)
        {
            seWs.DeleteLines(SessionId, indices.ToArray(), CurrentUser);
            StringBuilder sb = new StringBuilder();
            foreach (int index in indices)
            {
                sb.Append(index + ", ");
                AdjustUpdateLogToDelete(index);
                AppendToLog(string.Format(Configuration.Settings.Language.Main.NetworkDelete, CurrentUser.UserName, CurrentUser.Ip, index));
            }
        }

        internal void InsertLine(int index, Paragraph newParagraph)
        {
            seWs.InsertLine(SessionId, index, (int)newParagraph.StartTime.TotalMilliseconds, (int)newParagraph.EndTime.TotalMilliseconds, newParagraph.Text, CurrentUser);
            AppendToLog(string.Format(Configuration.Settings.Language.Main.NetworkInsert, CurrentUser.UserName, CurrentUser.Ip, index, newParagraph.Text.Replace(Environment.NewLine, Configuration.Settings.General.ListViewLineSeparatorString)));
        }

        internal void AdjustUpdateLogToInsert(int index)
        {
            foreach (var logEntry in UpdateLog)
            {
                if (logEntry.Index >= index)
                {
                    logEntry.Index++;
                }
            }
        }

        internal void AdjustUpdateLogToDelete(int index)
        {
            UpdateLogEntry removeThis = null;
            foreach (var logEntry in UpdateLog)
            {
                if (logEntry.Index == index)
                {
                    removeThis = logEntry;
                }
                else if (logEntry.Index > index)
                {
                    logEntry.Index--;
                }
            }

            if (removeThis != null)
            {
                UpdateLog.Remove(removeThis);
            }
        }

        internal string Restart()
        {
            string message = string.Empty;
            int retries = 0;
            const int maxRetries = 10;
            while (retries < maxRetries)
            {
                try
                {
                    System.Threading.Thread.Sleep(200);
                    StartServer(seWs.Url, SessionId, userName, fileName, out message);
                    retries = maxRetries;
                }
                catch
                {
                    System.Threading.Thread.Sleep(200);
                    retries++;
                }
            }

            return message == "Session is already running" ? ReJoin() : message;
        }

        internal string ReJoin()
        {
            string message = string.Empty;
            int retries = 0;
            const int maxRetries = 10;
            while (retries < maxRetries)
            {
                try
                {
                    System.Threading.Thread.Sleep(200);
                    if (Join(seWs.Url, userName, SessionId, out message))
                    {
                        message = "Reload";
                    }

                    retries = maxRetries;
                }
                catch
                {
                    System.Threading.Thread.Sleep(200);
                    retries++;
                }
            }

            return message;
        }

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

            if (timerWebService != null)
            {
                timerWebService.Dispose();
                timerWebService = null;
            }

            if (seWs == null)
            {
                return;
            }

            seWs.Dispose();
            seWs = null;
        }
    }
}
