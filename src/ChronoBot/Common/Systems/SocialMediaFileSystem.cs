using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Interfaces;
using ChronoBot.Utilities.SocialMedias;

namespace ChronoBot.Common.Systems
{
    public class SocialMediaFileSystem : FileSystem
    {
        public sealed override string PathToSaveFile { get; set; }

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
            string options = socialMediaUserData.Options ?? string.Empty;
            string live = socialMediaUserData.Live ? "1" : "0";

            XElement user = new XElement("User");
            XAttribute newName = new XAttribute("Name", name);
            XAttribute newChannelId = new XAttribute("ChannelID", channelId);
            XAttribute newId = new XAttribute("ID", id);
            XAttribute newOptions = new XAttribute("Options", options);
            XAttribute newLive = new XAttribute("Live", live);

            user.Add(newName, newChannelId, newId, newOptions, newLive);

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
                }
            }

            if (xmls.Count == 0)
                return new List<IUserData>();

            List<SocialMediaUserData> ud = new List<SocialMediaUserData>();
            ud.AddRange(CollectUserData(xmls, SocialMediaEnum.YouTube));
            ud.AddRange(CollectUserData(xmls, SocialMediaEnum.Twitter));
            ud.AddRange(CollectUserData(xmls, SocialMediaEnum.Twitch));
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
                    user.Options = e.Attribute("Options")?.Value;
                    user.Live = e.Attribute("Live")?.Value == "1";
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
                    if(!string.IsNullOrEmpty(socialMediaUserData.Options))
                        found.Attributes("Options").First().Value = socialMediaUserData.Options;
                    ud.Live = found.Attribute("Live")?.Value == "1";
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
                        Console.WriteLine($"Unable to delete {0}", userData.Name);
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
            
            return true;
        }
    }
}
