using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.Interfaces;
using ChronoBot.UserDatas;
using Discord;

namespace ChronoBot.Systems
{
    class MusicFileSystem : IFileSystem
    {
        public string PathToSaveFile { get; }
        public string Category { get; set; }

        public MusicFileSystem()
        {
            PathToSaveFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "Memory Card");
            Category = "Music";
        }

        public void Save(IUserData userData)
        {
            MusicUserData ud = (MusicUserData) userData;
            string name = ud.Name;
            string guildId = ud.GuildId.ToString();
            string channelId = ud.ChannelId.ToString();
            string id = ud.Id;

            XElement player = new XElement("Music");
            XAttribute newTitle = new XAttribute("Title", name);
            XAttribute newChannelId = new XAttribute("ChannelID", channelId);
            XAttribute newId = new XAttribute("ID", id);
            player.Add(newTitle, newChannelId, newId);

            XDocument xDoc;
            string guildPath = Path.Combine(PathToSaveFile, guildId + ".xml");
            if (!File.Exists(guildPath))
            {
                xDoc = new XDocument();

                XElement sm = new XElement(Category);
                sm.Add(player);

                XElement root = new XElement("Service");
                root.Add(sm);

                xDoc.Add(root);
            }
            else
            {
                xDoc = XDocument.Load(guildPath);

                if (xDoc.Root != null)
                {
                    XElement sm = xDoc.Root.Element(Category);
                    if (sm == null)
                    {
                        sm = new XElement(Category);
                        sm.Add(player);
                        xDoc.Root.Add(sm);
                    }
                    else
                        xDoc.Root.Element(Category)?.Add(player);
                }
            }

            xDoc.Save(guildPath);

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb,
                $"Saved {ud.ChannelId} in {ud.GuildId}.xml with song {ud.Id} ({ud.Name})"));
        }

        public List<IUserData> Load()
        {
            Dictionary<XDocument, ulong> xmls = new Dictionary<XDocument, ulong>();
            DirectoryInfo dirInfo = new DirectoryInfo(PathToSaveFile);
            StackTrace st;
            MethodBase mb;
            foreach (FileInfo fi in dirInfo.GetFiles())
            {
                if (fi.FullName.EndsWith(".xml"))
                {
                    try
                    {
                        xmls.Add(XDocument.Load(fi.FullName), ulong.Parse(Path.GetFileNameWithoutExtension(fi.Name)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        st = new StackTrace();
                        mb = st.GetFrame(1).GetMethod();
                        Program.Logger(new LogMessage(LogSeverity.Warning, mb.ReflectedType + "." + mb, "Unable to load music."));
                    }
                }
            }

            if (xmls.Count == 0)
                return new List<IUserData>();

            List<IUserData> ud = new List<IUserData>();
            ud.AddRange(CollectUserData(xmls, Category));

            st = new StackTrace();
            mb = st.GetFrame(1).GetMethod();
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, "Music loaded."));
            return ud;
        }

        public List<IUserData> CollectUserData(Dictionary<XDocument, ulong> xmls, string category = "")
        {
            List<IUserData> ud = new List<IUserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Service").Descendants(category).Descendants("Music"))
                {
                    MusicUserData user = new MusicUserData();
                    if (xml.Key.Document != null)
                        user.GuildId = xml.Value;
                    user.Name = e.Attribute("Title")?.Value;
                    user.ChannelId = ulong.Parse(e.Attribute("ChannelID")?.Value ?? string.Empty);
                    user.Id = e.Attribute("ID")?.Value;
                    ud.Add(user);
                }
            }
            return ud;
        }

        public void UpdateFile(IUserData userData)
        {
            string guildPath = Path.Combine(PathToSaveFile, userData.GuildId + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to update {0}", userData.Name);
                return;
            }

            XDocument xml = XDocument.Load(guildPath);
            XElement found = xml.Descendants("Service").Descendants(Category).Descendants("Music").First();
            found.Attribute("Title").Value = userData.Name;
            found.Attribute("ChannelID").Value = userData.ChannelId.ToString();
            found.Attribute("ID").Value = userData.Id;

            xml.Save(guildPath);

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Updated {userData.ChannelId} in {userData.GuildId}.xml"));
        }

        public void DeleteInFile(IUserData userData)
        {
            string guildPath = Path.Combine(PathToSaveFile, userData.GuildId+ ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to delete {0}", userData.Name);
                return;
            }

            XDocument xml = XDocument.Load(guildPath);
            xml.Descendants("Service").Descendants(Category).Descendants("Music").Where(x => x.Attribute("Title")?.Value == userData.Name).Remove();
            xml.Save(guildPath);

            Console.WriteLine("Deleted {0} in {1}.xml", userData.Name, userData.GuildId);
            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Deleted {userData.Name} in {userData.GuildId}.xml"));
        }
    }
}
