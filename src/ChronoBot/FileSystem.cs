
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using TwitchLib.Api.Models.v5.Users;

namespace ChronoBot
{
    class FileSystem
    {
        private string _path;

        public FileSystem()
        {
            _path = Path.Combine(System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty, "MemoryCards");
        }

        public void Save(SocialMedia.UserData userData)
        {
            string guildId = userData.guildID.ToString();
            string name = userData.name;
            string channelId = userData.channelID.ToString();
            string id = userData.id;
            string socialMedia = userData.socialMedia;

            XElement user = new XElement("User");
            user.Add(name, channelId, id);

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

            Console.WriteLine($"Saved {0} in {1}.xml", userData.name, userData.guildID);
        }

        public List<SocialMedia.UserData> Load()
        {
            List<XDocument> xmls = new List<XDocument>();
            DirectoryInfo dirInfo = new DirectoryInfo(_path);
            foreach (FileInfo fi in dirInfo.GetFiles())
                xmls.Add(XDocument.Load(fi.FullName));

            if (xmls.Count == 0)
                return new List<SocialMedia.UserData>();

            List<SocialMedia.UserData> ud = new List<SocialMedia.UserData>();
            ud.AddRange(CollectUserData(xmls, "YouTube"));
            ud.AddRange(CollectUserData(xmls, "Twitter"));
            ud.AddRange(CollectUserData(xmls, "Twitch"));
            return ud;
        }

        private List<SocialMedia.UserData> CollectUserData(List<XDocument> xmls, string socialMedia)
        {
            List<SocialMedia.UserData> ud = new List<SocialMedia.UserData>();
            foreach (XDocument xml in xmls)
            {
                foreach (XElement e in xml.Descendants("Users").Descendants(socialMedia).Descendants("User"))
                {
                    SocialMedia.UserData user = new SocialMedia.UserData();
                    user.name = e.Attribute("Name")?.Value;
                    user.channelID = ulong.Parse(e.Attribute("ChannelID")?.Value ?? string.Empty);
                    user.id = e.Attribute("ID")?.Value;
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
                Console.WriteLine($"Unable to update {0}", ud.name);
                return;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<SocialMedia.UserData> users = new List<SocialMedia.UserData>();
            users.AddRange(CollectUserData(new List<XDocument> { xml }, ud.socialMedia));
            foreach(SocialMedia.UserData userData in users)
            {
                if(ud.name == userData.name)
                {
                    xml.Descendants("Users").Descendants(ud.socialMedia).Descendants("User").Attributes("Name").First().Value = ud.name;
                    xml.Descendants("Users").Descendants(ud.socialMedia).Descendants("User").Attributes("ChannelID").First().Value = ud.channelID.ToString();
                    xml.Descendants("Users").Descendants(ud.socialMedia).Descendants("User").Attributes("ID").First().Value = ud.id;
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
            users.AddRange(CollectUserData(new List<XDocument> { xml }, socialMedia));
            foreach (SocialMedia.UserData userData in users)
            {
                if (ud.name != userData.name) 
                    continue;
                xml.Descendants("Users").Descendants(socialMedia).Descendants("User").Where(x => x.Attribute("Name")?.Value == ud.name).Remove();
                break;
            }
            xml.Save(guildPath);
            Console.WriteLine("Deleted {0} in {1}.xml", ud.name, ud.guildID);
        }
    }
}
