namespace Nikse.SubtitleEdit.Logic.Dictionaries
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Core;

    public class NamesList
    {
        private readonly string dictionaryFolder;
        private readonly string languageName;
        private readonly HashSet<string> namesList;
        private readonly HashSet<string> namesMultiList;

        public NamesList(string dictionaryFolder, string languageName, bool useOnlineNamesEtc, string namesEtcUrl)
        {
            this.dictionaryFolder = dictionaryFolder;
            this.languageName = languageName;

            this.namesList = new HashSet<string>();
            this.namesMultiList = new HashSet<string>();

            if (useOnlineNamesEtc && !string.IsNullOrEmpty(namesEtcUrl))
            {
                try
                {
                    var xml = Utilities.DownloadString(Configuration.Settings.WordLists.NamesEtcUrl);
                    var namesDoc = new XmlDocument();
                    namesDoc.LoadXml(xml);
                    LoadNames(this.namesList, this.namesMultiList, namesDoc);
                }
                catch (Exception exception)
                {
                    LoadNamesList(Path.Combine(this.dictionaryFolder, "names_etc.xml"), this.namesList, this.namesMultiList);
                    Debug.WriteLine(exception.Message);
                }
            }
            else
            {
                LoadNamesList(Path.Combine(this.dictionaryFolder, "names_etc.xml"), this.namesList, this.namesMultiList);
            }

            LoadNamesList(this.GetLocalNamesFileName(), this.namesList, this.namesMultiList);

            var userFile = this.GetLocalNamesUserFileName();
            LoadNamesList(userFile, this.namesList, this.namesMultiList);
            this.UnloadRemovedNames(userFile);
        }

        public List<string> GetAllNames()
        {
            var list = this.namesList.ToList();
            list.AddRange(this.namesMultiList);

            return list;
        }

        public HashSet<string> GetNames()
        {
            return this.namesList;
        }

        public HashSet<string> GetMultiNames()
        {
            return this.namesMultiList;
        }

        private void UnloadRemovedNames(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                return;
            }

            var namesDoc = new XmlDocument();
            namesDoc.Load(fileName);
            if (namesDoc.DocumentElement == null)
            {
                return;
            }

            var xmlNodeList = namesDoc.DocumentElement.SelectNodes("removed_name");
            if (xmlNodeList == null)
            {
                return;
            }

            foreach (XmlNode node in xmlNodeList)
            {
                string s = node.InnerText.Trim();
                if (s.Contains(' '))
                {
                    if (this.namesMultiList.Contains(s))
                    {
                        this.namesMultiList.Remove(s);
                    }
                }
                else if (this.namesList.Contains(s))
                {
                    this.namesList.Remove(s);
                }
            }
        }

        private string GetLocalNamesFileName()
        {
            if (this.languageName.Length != 2)
            {
                return Path.Combine(this.dictionaryFolder, this.languageName + "_names_etc.xml");
            }

            string[] files = Directory.GetFiles(this.dictionaryFolder, this.languageName + "_??_names_etc.xml");
            return files.Length > 0 ? files[0] : Path.Combine(this.dictionaryFolder, this.languageName + "_names_etc.xml");
        }

        private string GetLocalNamesUserFileName()
        {
            if (this.languageName.Length != 2)
            {
                return Path.Combine(this.dictionaryFolder, this.languageName + "_names_etc_user.xml");
            }

            string[] files = Directory.GetFiles(this.dictionaryFolder, this.languageName + "_??_names_etc_user.xml");
            return files.Length > 0 ? files[0] : Path.Combine(this.dictionaryFolder, this.languageName + "_names_etc_user.xml");
        }

        private static void LoadNamesList(string fileName, HashSet<string> namesList, HashSet<string> namesMultiList)
        {
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                return;
            }

            var namesDoc = new XmlDocument();
            namesDoc.Load(fileName);
            if (namesDoc.DocumentElement == null)
            {
                return;
            }

            LoadNames(namesList, namesMultiList, namesDoc);
        }

        private static void LoadNames(HashSet<string> namesList, HashSet<string> namesMultiList, XmlDocument namesDoc)
        {
            if (namesDoc.DocumentElement == null)
            {
                return;
            }

            var xmlNodeList = namesDoc.DocumentElement.SelectNodes("name");
            if (xmlNodeList == null)
            {
                return;
            }

            foreach (XmlNode node in xmlNodeList)
            {
                string s = node.InnerText.Trim();
                if (s.Contains(' ') && !namesMultiList.Contains(s))
                {
                    namesMultiList.Add(s);
                }
                else if (!namesList.Contains(s))
                {
                    namesList.Add(s);
                }
            }
        }

        public bool Remove(string name)
        {
            name = name.Trim();
            if ((name.Length <= 1 || !this.namesList.Contains(name)) && !this.namesMultiList.Contains(name))
            {
                return false;
            }

            if (this.namesList.Contains(name))
            {
                this.namesList.Remove(name);
            }

            if (this.namesMultiList.Contains(name))
            {
                this.namesMultiList.Remove(name);
            }

            var userList = new HashSet<string>();
            var fileName = this.GetLocalNamesUserFileName();
            LoadNamesList(fileName, userList, userList);

            var namesDoc = new XmlDocument();
            if (File.Exists(fileName))
            {
                namesDoc.Load(fileName);
            }
            else
            {
                namesDoc.LoadXml("<ignore_words />");
            }

            if (userList.Contains(name))
            {
                userList.Remove(name);
                XmlNode nodeToRemove = null;
                if (namesDoc.DocumentElement != null)
                {
                    var xmlNodeList = namesDoc.DocumentElement.SelectNodes("name");
                    if (xmlNodeList != null)
                    {
                        foreach (XmlNode node in xmlNodeList)
                        {
                            if (node.InnerText != name)
                            {
                                continue;
                            }

                            nodeToRemove = node;
                            break;
                        }
                    }

                    if (nodeToRemove != null)
                    {
                        namesDoc.DocumentElement.RemoveChild(nodeToRemove);
                    }
                }
            }
            else
            {
                XmlNode node = namesDoc.CreateElement("removed_name");
                node.InnerText = name;
                if (namesDoc.DocumentElement != null)
                {
                    namesDoc.DocumentElement.AppendChild(node);
                }
            }

            namesDoc.Save(fileName);
            return true;
        }

        public bool Add(string name)
        {
            name = name.Trim();
            if (name.Length <= 1 || !name.ContainsLetter())
            {
                return false;
            }

            if (name.Contains(' '))
            {
                if (!this.namesMultiList.Contains(name))
                {
                    this.namesMultiList.Add(name);
                }
            }
            else if (!this.namesList.Contains(name))
            {
                this.namesList.Add(name);
            }

            var fileName = this.GetLocalNamesUserFileName();
            var namesEtcDoc = new XmlDocument();
            if (File.Exists(fileName))
            {
                namesEtcDoc.Load(fileName);
            }
            else
            {
                namesEtcDoc.LoadXml("<ignore_words />");
            }

            XmlNode de = namesEtcDoc.DocumentElement;
            if (de == null)
            {
                return true;
            }

            XmlNode node = namesEtcDoc.CreateElement("name");
            node.InnerText = name;
            de.AppendChild(node);
            namesEtcDoc.Save(fileName);

            return true;
        }

        public bool IsInNamesEtcMultiWordList(string text, string word)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            text = text.Replace(Environment.NewLine, " ");
            text = text.FixExtraSpaces();

            foreach (string s in this.namesMultiList)
            {
                if (!s.Contains(word) || !text.Contains(s))
                {
                    continue;
                }

                if (s.StartsWith(word + " ", StringComparison.Ordinal) || s.EndsWith(" " + word, StringComparison.Ordinal) || s.Contains(" " + word + " "))
                {
                    return true;
                }

                if (word == s)
                {
                    return true;
                }
            }

            return false;
        }
    }
}