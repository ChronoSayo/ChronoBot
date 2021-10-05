using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Interfaces;

namespace ChronoBot.Common.Systems
{
    public class SocialMediaFileSystem : FileSystem
    {
        public SocialMediaFileSystem(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                PathToSaveFile =
                    Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty,
                        @"Resources\Memory Card");
            else
                PathToSaveFile = path;

            if (!Directory.Exists(PathToSaveFile))
                Directory.CreateDirectory(PathToSaveFile);
        }

        public sealed override string PathToSaveFile { get; }

        public override bool Save(IUserData userData)
        {
            if (userData.GuildId == 0)
                return false;

            var socialMediaUserData = (SocialMediaUserData) userData;
            string guildId = socialMediaUserData.GuildId.ToString();
            string name = socialMediaUserData.Name ?? string.Empty;
            string channelId = socialMediaUserData.ChannelId.ToString();
            string id = socialMediaUserData.Id ?? string.Empty;
            string socialMedia = socialMediaUserData.SocialMedia.ToString();

            XElement user = new XElement("User");
            XAttribute newName = new XAttribute("Name", name);
            XAttribute newChannelId = new XAttribute("ChannelID", channelId);
            XAttribute newId = new XAttribute("ID", id);
            user.Add(newName, newChannelId, newId);

            XDocument xDoc;
            string guildPath = Path.Combine(PathToSaveFile, guildId + ".xml");
            if (!File.Exists(guildPath))
            {
                xDoc = new XDocument();

                XElement sm = new XElement(socialMedia);
                sm.Add(user);

                XElement root = new XElement("Service");
                root.Add(sm);

                xDoc.Add(root);
            }
            else
            {
                xDoc = XDocument.Load(guildPath);

                if (xDoc.Root != null)
                {
                    XElement sm = xDoc.Root.Element(socialMedia);
                    if (sm == null)
                    {
                        sm = new XElement(socialMedia);
                        sm.Add(user);
                        xDoc.Root.Add(sm);
                    }
                    else
                        xDoc.Root.Element(socialMedia)?.Add(user);
                }
            }

            xDoc.Save(guildPath);

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Saved {userData.name} in {userData.guildID}.xml"));

            return true;
        }

        public override IEnumerable<IUserData> Load()
        {
            Dictionary<XDocument, ulong> xmls = new Dictionary<XDocument, ulong>();
            DirectoryInfo dirInfo = new DirectoryInfo(PathToSaveFile);
            foreach (FileInfo fi in dirInfo.GetFiles())
            {
                if (!fi.FullName.EndsWith(".xml")) 
                    continue;

                try
                {
                    var load = XDocument.Load(fi.FullName);
                    xmls.Add(load, ulong.Parse(Path.GetFileNameWithoutExtension(fi.Name)));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    StackTrace st = new StackTrace();
                    MethodBase mb = st.GetFrame(1).GetMethod();
                    //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, "Unable to load users."));
                }
            }

            if (xmls.Count == 0)
                return new List<IUserData>();

            List<SocialMediaUserData> ud = new List<SocialMediaUserData>();
            ud.AddRange(CollectUserData(xmls, SocialMediaEnum.YouTube).Cast<SocialMediaUserData>());
            ud.AddRange(CollectUserData(xmls, SocialMediaEnum.Twitter).Cast<SocialMediaUserData>());
            ud.AddRange(CollectUserData(xmls, SocialMediaEnum.Twitch).Cast<SocialMediaUserData>());
            return ud;
        }

        private IEnumerable<SocialMediaUserData> CollectUserData(Dictionary<XDocument, ulong> xmls, SocialMediaEnum socialMedia)
        {
            List<SocialMediaUserData> ud = new List<SocialMediaUserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Service").Descendants(socialMedia.ToString()).Descendants("User"))
                {
                    SocialMediaUserData user = new SocialMediaUserData();
                    if (xml.Key.Document != null)
                        user.GuildId = xml.Value;                    
                    user.Name = e.Attribute("Name")?.Value;
                    user.ChannelId = ulong.Parse(e.Attribute("ChannelID")?.Value ?? string.Empty);
                    user.Id = e.Attribute("ID")?.Value;
                    user.SocialMedia = socialMedia;
                    ud.Add(user);
                }
            }
            return ud;
        }

        public override bool UpdateFile(IUserData userData)
        {
            if (!(userData is SocialMediaUserData socialMediaUserData)) 
                return false;

            string guildPath = Path.Combine(PathToSaveFile, socialMediaUserData.GuildId + ".xml");
            if (!File.Exists(guildPath))
                return false;

            var xml = XDocument.Load(guildPath);
            List<SocialMediaUserData> users = new List<SocialMediaUserData>();
            var collection = CollectUserData(new Dictionary<XDocument, ulong> {{xml, socialMediaUserData.GuildId}}, socialMediaUserData.SocialMedia);
            users.AddRange(collection);
            bool updated = false;
            foreach (SocialMediaUserData ud in users)
            {
                if (ud.Name == socialMediaUserData.Name)
                {
                    XElement found = xml.Descendants("Service").Descendants(socialMediaUserData.SocialMedia.ToString()).Descendants("User")
                        .First(x => x.Attributes("Name").First().Value == socialMediaUserData.Name);
                    found.Attributes("Name").First().Value = socialMediaUserData.Name;
                    found.Attributes("ChannelID").First().Value = socialMediaUserData.ChannelId.ToString();
                    found.Attributes("ID").First().Value = socialMediaUserData.Id;
                    updated = true;
                    break;
                }
            }

            if (!updated)
                return false;

            xml.Save(guildPath);

            return true;
        }

        public override bool DeleteInFile(IUserData userData)
        {
            if (!(userData is SocialMediaUserData socialMediaUserData))
                return false;
            {
                string guildPath = Path.Combine(PathToSaveFile, socialMediaUserData.GuildId + ".xml");
                {
                    if (!File.Exists(guildPath))
                    {
                        Console.WriteLine("Unable to delete {0}", userData.Name);
                        return false;
                    }

                    XDocument xml = XDocument.Load(guildPath);
                    List<SocialMediaUserData> users = new List<SocialMediaUserData>();
                    var collection = CollectUserData(new Dictionary<XDocument, ulong> {{xml, userData.GuildId}}, socialMediaUserData.SocialMedia);
                    users.AddRange(collection);
                    bool removed = false;
                    foreach (SocialMediaUserData ud in users)
                    {
                        if (ud.Name != userData.Name) 
                            continue;
                        xml.Descendants("Service").Descendants(socialMediaUserData.SocialMedia.ToString()).Descendants("User").Where(x => x.Attribute("Name")?.Value == ud.Name).Remove();
                        removed = true;
                        break;
                    }

                    if (removed)
                        xml.Save(guildPath);
                    else
                        return false;
                }
            }

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Deleted {ud.name} in {ud.guildID}.xml"));

            return true;
        }
    }
}
