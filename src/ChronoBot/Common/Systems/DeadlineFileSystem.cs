using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.Enums;
using ChronoBot.Interfaces;
using ChronoBot.Common.UserDatas;

namespace ChronoBot.Common.Systems
{
    public class DeadlineFileSystem : FileSystem
    {
        public sealed override string PathToSaveFile { get; set; }

        public DeadlineFileSystem(string path = null)
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

            var deadlineUserData = (DeadlineUserData)userData;
            string guildId = deadlineUserData.GuildId.ToString();
            string name = deadlineUserData.Name ?? string.Empty;
            string channelId = deadlineUserData.ChannelId.ToString();
            string id = deadlineUserData.Id ?? string.Empty;
            string deadline = deadlineUserData.Deadline.ToString(CultureInfo.CurrentCulture);
            string userId = deadlineUserData.UserId.ToString();
            string deadlineType = deadlineUserData.DeadlineType.ToString();
            string daysLeft = deadlineUserData.DaysLeft.ToString();

            XElement user = new XElement("User");
            XAttribute newName = new XAttribute("Name", name);
            XAttribute newChannelId = new XAttribute("ChannelID", channelId);
            XAttribute newId = new XAttribute("ID", id);
            XAttribute newDeadline = new XAttribute("Deadline", deadline);
            XAttribute newUserId = new XAttribute("UserID", userId);
            XAttribute newDeadlineType = new XAttribute("DeadlineType", deadlineType);
            XAttribute newDaysLeft = new XAttribute("DaysLeft", daysLeft);

            user.Add(newName, newChannelId, newId, newDeadline, newUserId, newDeadlineType, newDaysLeft);

            XDocument xDoc;
            string guildPath = Path.Combine(PathToSaveFile, guildId + ".xml");
            if (!File.Exists(guildPath))
            {
                xDoc = new XDocument();

                XElement sm = new XElement(deadlineType);
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
                    XElement sm = xDoc.Root.Element(deadlineType);
                    if (sm == null)
                    {
                        sm = new XElement(deadlineType);
                        sm.Add(user);
                        xDoc.Root.Add(sm);
                    }
                    else
                        xDoc.Root.Element(deadlineType)?.Add(user);
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
                return new List<DeadlineUserData>();

            List<DeadlineUserData> ud = new List<DeadlineUserData>();
            ud.AddRange(CollectUserData(xmls, DeadlineEnum.Reminder));
            ud.AddRange(CollectUserData(xmls, DeadlineEnum.Countdown));
            ud.AddRange(CollectUserData(xmls, DeadlineEnum.Repeater));
            return ud;
        }

        private IEnumerable<DeadlineUserData> CollectUserData(Dictionary<XDocument, ulong> xmls, DeadlineEnum deadlineType)
        {
            List<DeadlineUserData> ud = new List<DeadlineUserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Service").Descendants(deadlineType.ToString()).Descendants("User"))
                {
                    DeadlineUserData user = new DeadlineUserData();
                    if (xml.Key.Document != null)
                        user.GuildId = xml.Value;
                    user.Name = e.Attribute("Name")?.Value;
                    user.ChannelId = ulong.Parse(e.Attribute("ChannelID")?.Value ?? string.Empty);
                    user.Id = e.Attribute("ID")?.Value;
                    user.Deadline = DateTime.Parse(e.Attribute("Deadline")?.Value ?? string.Empty);
                    user.UserId = ulong.Parse(e.Attribute("UserID")?.Value ?? string.Empty);
                    user.DeadlineType = deadlineType;
                    try
                    {
                        user.DaysLeft = int.Parse(e.Attribute("DaysLeft")?.Value ?? string.Empty);
                    }
                    catch
                    {
                        //Continue
                    }
                    
                    ud.Add(user);
                }
            }
            return ud;
        }

        public override bool UpdateFile(IUserData userData)
        {
            if (!(userData is DeadlineUserData deadlineUserData))
                return false;

            string guildPath = Path.Combine(PathToSaveFile, deadlineUserData.GuildId + ".xml");
            if (!File.Exists(guildPath))
                return false;

            var xml = XDocument.Load(guildPath);
            List<DeadlineUserData> users = new List<DeadlineUserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, deadlineUserData.GuildId } }, deadlineUserData.DeadlineType));
            bool updated = false;
            foreach (DeadlineUserData ud in users)
            {
                if (ud.Name == deadlineUserData.Name)
                {
                    try
                    {
                        XElement found = xml.Descendants("Service").Descendants(ud.DeadlineType.ToString()).Descendants("User")
                            .First(x => x.Attributes("UserID").First().Value == deadlineUserData.UserId.ToString() &&
                                        x.Attributes("ID").First().Value == deadlineUserData.Id);
                        found.Attributes("Name").First().Value = deadlineUserData.Name;
                        found.Attributes("ChannelID").First().Value = deadlineUserData.ChannelId.ToString();
                        found.Attributes("ID").First().Value = deadlineUserData.Id;
                        found.Attributes("Deadline").First().Value = deadlineUserData.Deadline.ToString(CultureInfo.CurrentCulture);
                        found.Attributes("UserID").First().Value = deadlineUserData.UserId.ToString();
                        found.Attributes("DaysLeft").First().Value = deadlineUserData.DaysLeft.ToString();
                        updated = true;
                        break;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            if (!updated)
                return false;

            xml.Save(guildPath);

            return true;
        }

        public override bool DeleteInFile(IUserData userData)
        {
            if (!(userData is DeadlineUserData deadlineUserData))
                return false;

            string guildPath = Path.Combine(PathToSaveFile, deadlineUserData.GuildId + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine($"Unable to delete {0} \"{1}\"", deadlineUserData.Name, deadlineUserData.Id);
                return false;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<DeadlineUserData> users = new List<DeadlineUserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> {{xml, deadlineUserData.GuildId}},
                deadlineUserData.DeadlineType));
            bool remove = false;
            foreach (var ud in users.Where(ud => ud.Id == deadlineUserData.Id || ud.UserId == deadlineUserData.UserId))
            {
                xml.Descendants("Service")
                    .Descendants(ud.DeadlineType.ToString())
                    .Descendants("User").Where(x =>
                        x.Attribute("ID")?.Value == ud.Id && x.Attribute("UserID")?.Value == ud.UserId.ToString() &&
                        x.Attribute("ChannelID")?.Value == ud.ChannelId.ToString())
                    .Remove();
                remove = true;
                break;
            }

            if (!remove)
                return false;

            xml.Save(guildPath);

            return true;
        }
    }
}
