using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.SocialMedias;
using Discord;

namespace ChronoBot.Systems
{
    class SocialMediaFileSystem
    {
        private readonly string _path;

        public SocialMediaFileSystem()
        {
            _path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "Memory Card");
        }

        public void Save(SocialMedia.UserData userData)
        {
            string guildId = userData.guildID.ToString();
            string name = userData.name;
            string channelId = userData.channelID.ToString();
            string id = userData.id;
            string socialMedia = userData.socialMedia;

            XElement user = new XElement("User");
            XAttribute newName = new XAttribute("Name", name);
            XAttribute newChannelId = new XAttribute("ChannelID", channelId);
            XAttribute newId = new XAttribute("ID", id);
            user.Add(newName, newChannelId, newId);

            XDocument xDoc;
            string guildPath = Path.Combine(_path, guildId + ".xml");
            if (!File.Exists(guildPath))
            {
                xDoc = new XDocument();

                XElement sm = new XElement(socialMedia);
                sm.Add(user);

                XElement root = new XElement("Users");
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
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Saved {userData.name} in {userData.guildID}.xml"));
        }

        public List<SocialMedia.UserData> Load()
        {
            Dictionary<XDocument, ulong> xmls = new Dictionary<XDocument, ulong>();
            DirectoryInfo dirInfo = new DirectoryInfo(_path);
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
                        StackTrace st = new StackTrace();
                        MethodBase mb = st.GetFrame(1).GetMethod();
                        Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, "Unable to load users."));
                    }
                }
            }

            if (xmls.Count == 0)
                return new List<SocialMedia.UserData>();

            List<SocialMedia.UserData> ud = new List<SocialMedia.UserData>();
            ud.AddRange(CollectUserData(xmls, "YouTube"));
            ud.AddRange(CollectUserData(xmls, "Twitter"));
            ud.AddRange(CollectUserData(xmls, "Twitch"));
            return ud;
        }

        private List<SocialMedia.UserData> CollectUserData(Dictionary<XDocument, ulong> xmls, string socialMedia)
        {
            List<SocialMedia.UserData> ud = new List<SocialMedia.UserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Users").Descendants(socialMedia).Descendants("User"))
                {
                    SocialMedia.UserData user = new SocialMedia.UserData();
                    if (xml.Key.Document != null)
                        user.guildID = xml.Value;                    
                    user.name = e.Attribute("Name")?.Value;
                    user.channelID = ulong.Parse(e.Attribute("ChannelID")?.Value ?? string.Empty);
                    user.id = e.Attribute("ID")?.Value;
                    user.socialMedia = socialMedia;
                    ud.Add(user);
                }
            }
            return ud;
        }

        public void UpdateFile(SocialMedia.UserData ud)
        {
            string guildPath = Path.Combine(_path, ud.guildID + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to update {0}", ud.name);
                return;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<SocialMedia.UserData> users = new List<SocialMedia.UserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, ud.guildID } }, ud.socialMedia));
            foreach(SocialMedia.UserData userData in users)
            {
                if(ud.name == userData.name)
                {
                    XElement found = xml.Descendants("Users").Descendants(ud.socialMedia).Descendants("User")
                        .First(x => x.Attributes("Name").First().Value == ud.name);
                    found.Attributes("Name").First().Value = ud.name;
                    found.Attributes("ChannelID").First().Value = ud.channelID.ToString();
                    found.Attributes("ID").First().Value = ud.id;
                    break;
                }
            }
            xml.Save(guildPath);
        }

        public void DeleteInFile(SocialMedia.UserData ud)
        {
            string guildPath = Path.Combine(_path, ud.guildID + ".xml");
            string socialMedia = ud.socialMedia;
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to update {0}", ud.name);
                return;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<SocialMedia.UserData> users = new List<SocialMedia.UserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, ud.guildID } }, socialMedia));
            foreach (SocialMedia.UserData userData in users)
            {
                if (ud.name != userData.name) 
                    continue;
                xml.Descendants("Users").Descendants(socialMedia).Descendants("User").Where(x => x.Attribute("Name")?.Value == ud.name).Remove();
                break;
            }
            xml.Save(guildPath);

            Console.WriteLine("Deleted {0} in {1}.xml", ud.name, ud.guildID);
            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Deleted {ud.name} in {ud.guildID}.xml"));
        }
    }
}
