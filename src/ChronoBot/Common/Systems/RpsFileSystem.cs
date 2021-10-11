using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Interfaces;
using ChronoBot.Utilities.Games;

namespace ChronoBot.Common.Systems
{
    public class RpsFileSystem : FileSystem
    {
        private const string ElementRoot = "RockPaperScissors";
        private readonly IEnumerable<string> _attributeNames;

        public sealed override string PathToSaveFile { get; }

        public RpsFileSystem(string path = null)
        {
            if (string.IsNullOrEmpty(path))
                PathToSaveFile =
                    Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty,
                        @"Resources\Memory Card");
            else
                PathToSaveFile = path;

            if (!Directory.Exists(PathToSaveFile))
                Directory.CreateDirectory(PathToSaveFile);

            _attributeNames = new[]
            {
                "UserID", "UserIDOpponent", "ChannelID", "Plays", "TotalPlays", "Wins", "Losses", "Draws", "Ratio",
                "CurrentStreak", "BestStreak", "Resets", "Rocks", "Papers", "Scissors", "Coins", "Actor", "Deadline"
            };
        }

        public override bool Save(IUserData userData)
        {
            if (userData.GuildId == 0)
                return false;

            var rpsUserData = (RpsUserData)userData;
            string userId = rpsUserData.UserId.ToString();
            string userIdVs = rpsUserData.UserIdVs.ToString();
            string guildId = rpsUserData.GuildId.ToString();
            string channelId = rpsUserData.ChannelId.ToString();
            string actor = rpsUserData.Actor.ToString();
            string dateVs = rpsUserData.DateVs.ToString(CultureInfo.InvariantCulture);

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
                else if (name == "Actor")
                    value = actor;
                else if(name == "Deadline")
                    value = dateVs;
                attributes.Add(new XAttribute(name, value));
            }
            user.Add(attributes);

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

            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Saved {ud.UserId} in {ud.GuildId}.xml"));

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
                        //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, "Unable to load users."));
                    }
                }
            }

            if (xmls.Count == 0)
                return new List<RpsUserData>();

            List<RpsUserData> ud = new List<RpsUserData>();
            ud.AddRange(CollectUserData(xmls));
            return ud;
        }

        private IEnumerable<RpsUserData> CollectUserData(Dictionary<XDocument, ulong> xmls)
        {
            List<RpsUserData> ud = new List<RpsUserData>();
            foreach (KeyValuePair<XDocument, ulong> xml in xmls)
            {
                foreach (XElement e in xml.Key.Descendants("Service").Descendants(ElementRoot).Descendants("User"))
                {
                    RpsUserData user = new RpsUserData();
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
                    user.DateVs = GetAttributeValueDateTime(_attributeNames.ElementAt(i) ?? DateTime.Now.ToString(CultureInfo.InvariantCulture), e);
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

        private RpsActors GetAttributeValueActor(string name, XElement e)
        {
            return (RpsActors)Enum.Parse(typeof(RpsActors), e.Attribute(name)?.Value ?? RpsActors.Max.ToString());
        }

        private DateTime GetAttributeValueDateTime(string name, XElement e)
        {
            return DateTime.Parse(e.Attribute(name)?.Value ?? DateTime.Now.ToString(CultureInfo.InvariantCulture));
        }

        public override bool UpdateFile(IUserData userData)
        {
            if (!(userData is RpsUserData rpsUserData))
                return true;

            string guildPath = Path.Combine(PathToSaveFile, userData.GuildId + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to update {0}", rpsUserData.UserId);
                return false;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<RpsUserData> users = new List<RpsUserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, rpsUserData.GuildId } }));
            foreach (RpsUserData ud in users)
            {
                if (ud.UserId == rpsUserData.UserId)
                {
                    XElement found = xml.Descendants("Service").Descendants(ElementRoot).Descendants("User")
                        .First(x => x.Attributes(_attributeNames.ElementAt(0)).First().Value == ud.UserId.ToString());
                    
                    int i = 0;
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.UserId.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.UserIdVs.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.ChannelId.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Plays.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.TotalPlays.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Wins.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Losses.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Draws.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Ratio.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.CurrentStreak.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.BestStreak.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Resets.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.RockChosen.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.PaperChosen.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.ScissorsChosen.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Coins.ToString();
                    GetAttribute(_attributeNames.ElementAt(i++), found).Value = rpsUserData.Actor.ToString();
                    GetAttribute(_attributeNames.ElementAt(i), found).Value = rpsUserData.DateVs.ToString(CultureInfo.InvariantCulture);
                    break;
                }
            }
            xml.Save(guildPath);
            return true;
        }

        private XAttribute GetAttribute(string name, XElement found)
        {
            if (found.Attribute(name) == null)
                found.SetAttributeValue(name, 0);
            return found.Attribute(name);
        }

        public override bool DeleteInFile(IUserData userData)
        {
            if (!(userData is RpsUserData rpsUserData))
                return false;

            string guildPath = Path.Combine(PathToSaveFile, rpsUserData.GuildId + ".xml");
            if (!File.Exists(guildPath))
            {
                Console.WriteLine("Unable to delete {0}", rpsUserData.UserId);
                return false;
            }

            XDocument xml = XDocument.Load(guildPath);
            List<RpsUserData> users = new List<RpsUserData>();
            users.AddRange(CollectUserData(new Dictionary<XDocument, ulong> { { xml, rpsUserData.GuildId } }));
            foreach (RpsUserData ud in users)
            {
                if (ud.UserId != rpsUserData.UserId)
                    continue;
                xml.Descendants("Service").Descendants(ElementRoot).Descendants("User").Where(x =>
                    x.Attribute(_attributeNames.ElementAt(0))?.Value == ud.UserId.ToString()).Remove();
                break;
            }
            xml.Save(guildPath);

            Console.WriteLine("Deleted {0} in {1}.xml", rpsUserData.UserId, rpsUserData.GuildId);
            StackTrace st = new StackTrace();
            MethodBase mb = st.GetFrame(1).GetMethod();
            //Program.Logger(new LogMessage(LogSeverity.Info, mb.ReflectedType + "." + mb, $"Deleted {ud.UserId} in {ud.GuildId}.xml"));

            return true;
        }
    }
}
