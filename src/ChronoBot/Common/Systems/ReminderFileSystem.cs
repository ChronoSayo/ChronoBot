using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.Common.UserDatas;
using ChronoBot.Interfaces;

namespace ChronoBot.Common.Systems
{
    public class ReminderFileSystem : FileSystem
    {
        private const string ElementRoot = "Reminder";

        public sealed override string PathToSaveFile { get; set; }

        public ReminderFileSystem(string path = null)
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

            var reminderUserData = (ReminderUserData)userData;
            string guildId = reminderUserData.GuildId.ToString();
            string name = reminderUserData.Name ?? string.Empty;
            string channelId = reminderUserData.ChannelId.ToString();
            string id = reminderUserData.Id ?? string.Empty;
            string deadline = reminderUserData.Deadline.ToString();
            string remindee = reminderUserData.Remindee.ToString();

            XElement user = new XElement("User");
            XAttribute newName = new XAttribute("Name", name);
            XAttribute newChannelId = new XAttribute("ChannelID", channelId);
            XAttribute newId = new XAttribute("ID", id);
            XAttribute newDeadline = new XAttribute("Deadline", deadline);
            XAttribute newRemindee = new XAttribute("Remindee", remindee);
            user.Add(newName, newChannelId, newId, newDeadline, newRemindee);

            XDocument xDoc;
            string guildPath = Path.Combine(PathToSaveFile, guildId + ".xml");
            if (!File.Exists(guildPath))
            {
                xDoc = new XDocument();

                XElement sm = new XElement(ElementRoot);
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
                    XElement sm = xDoc.Root.Element(ElementRoot);
                    if (sm == null)
                    {
                        sm = new XElement(ElementRoot);
                        sm.Add(user);
                        xDoc.Root.Add(sm);
                    }
                    else
                        xDoc.Root.Element(ElementRoot)?.Add(user);
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
                    }
                }
            }

            if (xmls.Count == 0)
                return new List<ReminderUserData>();

            List<ReminderUserData> ud = new List<ReminderUserData>();
            ud.AddRange(CollectUserData(xmls));
            return ud;
        }

        private IEnumerable<ReminderUserData> CollectUserData(Dictionary<XDocument, ulong> xmls)
        {
            List<ReminderUserData> ud = new List<ReminderUserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Service").Descendants(ElementRoot).Descendants("User"))
                {
                    ReminderUserData user = new ReminderUserData();
                    if (xml.Key.Document != null)
                        user.GuildId = xml.Value;
                    user.Name = e.Attribute("Name")?.Value;
                    user.ChannelId = ulong.Parse(e.Attribute("ChannelID")?.Value ?? string.Empty);
                    user.Id = e.Attribute("ID")?.Value;
                    user.Deadline = DateTime.Parse(e.Attribute("Deadline")?.Value ?? string.Empty);
                    user.Remindee = ulong.Parse(e.Attribute("Remindee")?.Value ?? string.Empty);
                    ud.Add(user);
                }
            }
            return ud;
        }

        public override bool UpdateFile(IUserData userData)
        {
            if (!(userData is ReminderUserData reminderUserData))
                return false;

            string guildPath = Path.Combine(PathToSaveFile, reminderUserData.GuildId + ".xml");
            if (!File.Exists(guildPath))
                return false;

            var xml = XDocument.Load(guildPath);
            List<ReminderUserData> users = new List<ReminderUserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, reminderUserData.GuildId } }));
            bool updated = false;
            foreach (ReminderUserData ud in users)
            {
                if (ud.Name == reminderUserData.Name)
                {
                    XElement found = xml.Descendants("Service").Descendants(ElementRoot).Descendants("User")
                        .First(x => x.Attributes("Name").First().Value == reminderUserData.Name);
                    found.Attributes("Name").First().Value = reminderUserData.Name;
                    found.Attributes("ChannelID").First().Value = reminderUserData.ChannelId.ToString();
                    found.Attributes("ID").First().Value = reminderUserData.Id;
                    found.Attributes("Deadline").First().Value = reminderUserData.Deadline.ToShortDateString();
                    found.Attributes("Remindee").First().Value = reminderUserData.Remindee.ToString();
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
            if (!(userData is ReminderUserData reminderUserData))
                return false;

            string guildPath = Path.Combine(PathToSaveFile, reminderUserData.GuildId + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine($"Unable to delete {0} \"{1}\"", reminderUserData.Name, reminderUserData.Id);
                return false;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<ReminderUserData> users = new List<ReminderUserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, reminderUserData.GuildId } }));
            bool remove = true;
            foreach (ReminderUserData ud in users)
            {
                if (ud.Id != reminderUserData.Id && ud.Name == ud.Name)
                    continue;
                xml.Descendants("Service").
                    Descendants(ElementRoot).
                    Descendants("User").
                    Where(x => x.Attribute("ID")?.Value == ud.Id && x.Attribute("Name")?.Value == ud.Name).
                    Remove();
                remove = false;
                break;
            }

            if (remove)
                return false;

            xml.Save(guildPath);

            return true;
        }

    }
}
