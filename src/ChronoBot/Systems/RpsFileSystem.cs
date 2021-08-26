using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.Games;
using Discord;

namespace ChronoBot.Systems
{
    class RpsFileSystem
    {
        private readonly string _path;
        private const string ElementRoot = "RockPaperScissors";
        private readonly IEnumerable<string> _attributeNames;

        public RpsFileSystem()
        {
            _path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "Memory Card");
            _attributeNames = new[]
            {
                "UserID", "UserIDOpponent", "ChannelID", "Plays", "TotalPlays", "Wins", "Losses", "Draws", "Ratio",
                "CurrentStreak", "BestStreak", "Resets", "Rocks", "Papers", "Scissors", "Coins", "Actor", "Deadline"
            };
        }

        public void Save(RockPaperScissors.UserData ud)
        {
            string userId = ud.UserId.ToString();
            string userIdVs = ud.UserIdVs.ToString();
            string guildId = ud.GuildId.ToString();
            string channelId = ud.ChannelId.ToString();

            XElement user = new XElement("User");
            List<XAttribute> attributes = new List<XAttribute>();
            foreach (string name in _attributeNames)
            {
                object value = 0;
                if(name == _attributeNames.ElementAt(0))
                    value = userId;
                else if (name == _attributeNames.ElementAt(1))
                    value = userIdVs;
                else if (name == _attributeNames.ElementAt(2))
                    value = channelId;
                attributes.Add(new XAttribute(name, value));
            }
            user.Add(attributes);

            XDocument xDoc;
            string guildPath = Path.Combine(_path, guildId + ".xml");
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

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Saved {ud.UserId} in {ud.GuildId}.xml"));
        }

        public List<RockPaperScissors.UserData> Load()
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
                return new List<RockPaperScissors.UserData>();

            List<RockPaperScissors.UserData> ud = new List<RockPaperScissors.UserData>();
            ud.AddRange(CollectUserData(xmls));
            return ud;
        }

        private List<RockPaperScissors.UserData> CollectUserData(Dictionary<XDocument, ulong> xmls)
        {
            List<RockPaperScissors.UserData> ud = new List<RockPaperScissors.UserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Service").Descendants(ElementRoot).Descendants("User"))
                {
                    RockPaperScissors.UserData user = new RockPaperScissors.UserData();
                    if (xml.Key.Document != null)
                        user.GuildId = xml.Value;

                    int i = 0;
                    user.UserId = GetAttributeValueLong(_attributeNames.ElementAt(i++), e);
                    user.UserIdVs = GetAttributeValueLong(_attributeNames.ElementAt(i++), e);
                    user.ChannelId = GetAttributeValueLong(_attributeNames.ElementAt(i++), e);
                    user.Plays = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.TotalPlays = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.Wins = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.Losses = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.Draws = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.Ratio = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.CurrentStreak = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.BestStreak = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.Resets = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.RockChosen = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.PaperChosen = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.ScissorsChosen = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.Coins = GetAttributeValueInt(_attributeNames.ElementAt(i++), e);
                    user.Actor = GetAttributeValueActor(_attributeNames.ElementAt(i++), e);
                    user.DateVs = GetAttributeValueDateTime(_attributeNames.ElementAt(i), e);
                    ud.Add(user);
                }
            }
            return ud;
        }

        private ulong GetAttributeValueLong(string name, XElement e)
        {
            return ulong.Parse(e.Attribute(name)?.Value ?? "0");   
        }

        private int GetAttributeValueInt(string name, XElement e)
        {
            return int.Parse(e.Attribute(name)?.Value ?? "0");
        }

        private RockPaperScissors.Actor GetAttributeValueActor(string name, XElement e)
        {
            return (RockPaperScissors.Actor)Enum.Parse(typeof(RockPaperScissors.Actor), e.Attribute(name)?.Value ?? RockPaperScissors.Actor.Max.ToString());
        }

        private DateTime GetAttributeValueDateTime(string name, XElement e)
        {
            return DateTime.Parse(e.Attribute(name)?.Value ?? DateTime.Now.ToString(CultureInfo.InvariantCulture));
        }

        public void UpdateFile(RockPaperScissors.UserData ud)
        {
            string guildPath = Path.Combine(_path, ud.GuildId + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to update {0}", ud.UserId);
                return;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<RockPaperScissors.UserData> users = new List<RockPaperScissors.UserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, ud.GuildId } }));
            foreach (RockPaperScissors.UserData userData in users)
            {
                if (ud.UserId == userData.UserId)
                {
                    XElement found = xml.Descendants("Service").Descendants(ElementRoot).Descendants("User")
                        .First(x => x.Attributes(_attributeNames.ElementAt(0)).First().Value == ud.UserId.ToString());


                    int i = 0;
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.UserId.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.UserIdVs.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.ChannelId.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Plays.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.TotalPlays.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Wins.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Losses.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Draws.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Ratio.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.CurrentStreak.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.BestStreak.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Resets.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.RockChosen.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.PaperChosen.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.ScissorsChosen.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Coins.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = ud.Actor.ToString();
                    GetAttribute(_attributeNames.ElementAt(i), found).Value = ud.DateVs.ToString(CultureInfo.InvariantCulture);
                    break;
                }
            }
            xml.Save(guildPath);
        }

        private XAttribute GetAttribute(string name, XElement found)
        {
            if (found.Attribute(name) == null)
                found.SetAttributeValue(name, 0);
            return found.Attribute(name);
        }

        public void DeleteInFile(RockPaperScissors.UserData ud)
        {
            string guildPath = Path.Combine(_path, ud.GuildId + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to delete {0}", ud.UserId);
                return;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<RockPaperScissors.UserData> users = new List<RockPaperScissors.UserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, ud.GuildId } }));
            foreach (RockPaperScissors.UserData userData in users)
            {
                if (ud.UserId != userData.UserId)
                    continue;
                xml.Descendants("Service").Descendants(ElementRoot).Descendants("User").Where(x =>
                    x.Attribute(_attributeNames.ElementAt(0))?.Value == ud.UserId.ToString()).Remove();
                break;
            }
            xml.Save(guildPath);

            Console.WriteLine("Deleted {0} in {1}.xml", ud.UserId, ud.GuildId);
            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Deleted {ud.UserId} in {ud.GuildId}.xml"));
        }
    }
}
