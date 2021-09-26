using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ChronoBot.Common.UserDatas;
using ChronoBot.Interfaces;

namespace ChronoBot.Common.Systems
{
    public class SocialMediaFileSystem : IFileSystem
    {
        public SocialMediaFileSystem()
        {
            PathToSaveFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "Memory Card");
        }

        public string PathToSaveFile { get; }
        public string Category { get; set; }

        public async Task SaveAsync(IUserData userData)
        {
            var socialMediaUserData = (SocialMediaUserData) userData;
            string guildId = socialMediaUserData.GuildId.ToString();
            string name = socialMediaUserData.Name;
            string channelId = socialMediaUserData.ChannelId.ToString();
            string id = socialMediaUserData.Id;
            string socialMedia = socialMediaUserData.SocialMedia;

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

            await xDoc.SaveAsync(GetPathInStream(guildPath), SaveOptions.None, CancellationToken.None);

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Saved {userData.name} in {userData.guildID}.xml"));
        }

        public async Task<IEnumerable<IUserData>> LoadAsync()
        {
            Dictionary<XDocument, ulong> xmls = new Dictionary<XDocument, ulong>();
            DirectoryInfo dirInfo = new DirectoryInfo(PathToSaveFile);
            foreach (FileInfo fi in dirInfo.GetFiles())
            {
                if (!fi.FullName.EndsWith(".xml")) 
                    continue;

                try
                {
                    var load = await XDocument.LoadAsync(GetPathInStream(fi.FullName), LoadOptions.None, CancellationToken.None);
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
                return await Task.FromResult(new List<IUserData>());

            List<SocialMediaUserData> ud = new List<SocialMediaUserData>();
            ud.AddRange(CollectUserDataAsync(xmls, "YouTube").Cast<SocialMediaUserData>());
            ud.AddRange(CollectUserDataAsync(xmls, "Twitter").Cast<SocialMediaUserData>());
            ud.AddRange(CollectUserDataAsync(xmls, "Twitch").Cast<SocialMediaUserData>());
            return await Task.FromResult(ud);
        }

        public IEnumerable<IUserData> CollectUserDataAsync(Dictionary<XDocument, ulong> xmls, string socialMedia)
        {
            List<SocialMediaUserData> ud = new List<SocialMediaUserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Service").Descendants(socialMedia).Descendants("User"))
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

        public async Task UpdateFileAsync(IUserData userData)
        {
            if (userData is SocialMediaUserData socialMediaUserData)
            {
                string guildPath = Path.Combine(PathToSaveFile, socialMediaUserData.GuildId + ".xml");
                if (!File.Exists(guildPath))
                {
                    Console.WriteLine("Unable to update {0}", socialMediaUserData.Name);
                    return;
                }

                var stream = GetPathInStream(guildPath);
                var xml = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                List<SocialMediaUserData> users = new List<SocialMediaUserData>();
                var collection = CollectUserDataAsync(new Dictionary<XDocument, ulong> {{xml, socialMediaUserData.GuildId}},
                    socialMediaUserData.SocialMedia).Cast<SocialMediaUserData>();
                users.AddRange(collection);
                foreach(SocialMediaUserData ud in users)
                {
                    if(ud.Name == userData.Name)
                    {
                        XElement found = xml.Descendants("Service").Descendants(ud.SocialMedia).Descendants("User")
                            .First(x => x.Attributes("Name").First().Value == ud.Name);
                        found.Attributes("Name").First().Value = ud.Name;
                        found.Attributes("ChannelID").First().Value = ud.ChannelId.ToString();
                        found.Attributes("ID").First().Value = ud.Id;
                        break;
                    }
                }
                await xml.SaveAsync(GetPathInStream(guildPath), SaveOptions.None, CancellationToken.None);
            }
        }

        public async Task DeleteInFileAsync(IUserData ud)
        {
            if (ud is SocialMediaUserData socialMediaUserData)
            {
                string guildPath = Path.Combine(PathToSaveFile, socialMediaUserData.GuildId + ".xml");
                {
                    string socialMedia = socialMediaUserData.SocialMedia;
                    if (!File.Exists(guildPath))
                    {
                        Console.WriteLine("Unable to delete {0}", ud.Name);
                        return;
                    }

                    XDocument xml = XDocument.Load(guildPath);
                    List<SocialMediaUserData> users = new List<SocialMediaUserData>();
                    var collection = CollectUserDataAsync(new Dictionary<XDocument, ulong> {{xml, ud.GuildId}}, socialMedia)
                        .Cast<SocialMediaUserData>();
                    users.AddRange(collection);
                    foreach (SocialMediaUserData userData in users)
                    {
                        if (ud.Name != userData.Name) 
                            continue;
                        xml.Descendants("Service").Descendants(socialMedia).Descendants("User").Where(x => x.Attribute("Name")?.Value == ud.Name).Remove();
                        break;
                    }

                    await xml.SaveAsync(GetPathInStream(guildPath), SaveOptions.None, CancellationToken.None);
                }
            }

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Deleted {ud.name} in {ud.guildID}.xml"));
        }

        public Stream GetPathInStream(string path)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(path);
            return new MemoryStream(byteArray);
        }
    }
}
