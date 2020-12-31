using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public RpsFileSystem()
        {
            _path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "Memory Card");
        }

        public void Save(RockPaperScissors.UserData ud)
        {
            string userId = ud.UserId.ToString();
            string guildId = ud.GuildId.ToString();
            string channelId = ud.ChannelId.ToString();

            XElement user = new XElement("User");
            XAttribute newUserId = new XAttribute("UserID", userId);
            XAttribute newChannelId = new XAttribute("ChannelID", channelId);
            XAttribute plays = new XAttribute("Plays", 0);
            XAttribute totalPlays = new XAttribute("TotalPlays", 0);
            XAttribute wins = new XAttribute("Wins", 0);
            XAttribute losses = new XAttribute("Losses", 0);
            XAttribute draws = new XAttribute("Draws", 0);
            XAttribute ratio = new XAttribute("Ratio", 0);
            XAttribute currentStreak = new XAttribute("CurrentStreak", 0);
            XAttribute bestStreak = new XAttribute("BestStreak", 0);
            XAttribute resets = new XAttribute("Resets", 0);
            XAttribute rocks = new XAttribute("Rocks", 0);
            XAttribute papers = new XAttribute("Papers", 0);
            XAttribute scissors = new XAttribute("Scissors", 0);
            XAttribute coins = new XAttribute("Coins", 0);
            user.Add(newUserId, newChannelId, plays, totalPlays, wins, losses, draws, ratio, currentStreak, bestStreak, resets, rocks, papers, scissors, coins);

            XDocument xDoc;
            string guildPath = Path.Combine(_path, guildId + ".xml");
            if (!File.Exists(guildPath))
            {
                xDoc = new XDocument();

                XElement sm = new XElement(ElementRoot);
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
                foreach (XElement e in xml.Key.Descendants("Users").Descendants(ElementRoot).Descendants("User"))
                {
                    RockPaperScissors.UserData user = new RockPaperScissors.UserData();
                    if (xml.Key.Document != null)
                        user.GuildId = xml.Value;
                    user.UserId = ulong.Parse(e.Attribute("UserID")?.Value ?? string.Empty);
                    user.ChannelId = ulong.Parse(e.Attribute("ChannelID")?.Value ?? string.Empty);
                    user.Plays = int.Parse((e.Attribute("Plays")?.Value) ?? "0");
                    user.TotalPlays = int.Parse((e.Attribute("TotalPlays")?.Value) ?? "0");
                    user.Wins = int.Parse((e.Attribute("Wins")?.Value) ?? "0");
                    user.Losses = int.Parse((e.Attribute("Losses")?.Value) ?? "0");
                    user.Draws = int.Parse((e.Attribute("Draws")?.Value) ?? "0");
                    user.Ratio = int.Parse((e.Attribute("Ratio")?.Value) ?? "0");
                    user.CurrentStreak = int.Parse((e.Attribute("CurrentStreak")?.Value) ?? "0");
                    user.BestStreak = int.Parse((e.Attribute("BestStreak")?.Value) ?? "0");
                    user.Resets = int.Parse((e.Attribute("Resets")?.Value) ?? "0");
                    user.RockChosen = int.Parse((e.Attribute("Rocks")?.Value) ?? "0");
                    user.PaperChosen = int.Parse((e.Attribute("Papers")?.Value) ?? "0");
                    user.ScissorsChosen = int.Parse((e.Attribute("Scissors")?.Value) ?? "0");
                    user.Coins = int.Parse((e.Attribute("Coins")?.Value) ?? "0");
                    ud.Add(user);
                }
            }
            return ud;
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
                    XElement found = xml.Descendants("Users").Descendants(ElementRoot).Descendants("User")
                        .First(x => x.Attributes("UserID").First().Value == ud.UserId.ToString());
                    if(found.Attributes("UserID").First() != null)
                        found.Attributes("UserID").First().Value = ud.UserId.ToString();
                    else
                        found.SetAttributeValue("UserID", 0);

                    if (found.Attributes("ChannelID").First() != null)
                        found.Attributes("ChannelID").First().Value = ud.ChannelId.ToString();
                    else
                        found.SetAttributeValue("ChannelID", 0);

                    if (found.Attribute("Plays") Attributes("Plays") != null)
                        found.Attributes("Plays").First().Value = ud.Plays.ToString();
                    else
                        found.SetAttributeValue("Plays", 0);

                    if (found.Attributes("TotalPlays").First() != null)
                        found.Attributes("TotalPlays").First().Value = ud.TotalPlays.ToString();
                    else
                        found.SetAttributeValue("TotalPlays", 0);

                    if (found.Attributes("Wins").First() != null)
                        found.Attributes("Wins").First().Value = ud.Wins.ToString();
                    else
                        found.SetAttributeValue("Wins", 0);

                    if (found.Attributes("Losses").First() != null)
                        found.Attributes("Losses").First().Value = ud.Losses.ToString();
                    else
                        found.SetAttributeValue("Losses", 0);

                    if (found.Attributes("Draws").First() != null)
                        found.Attributes("Draws").First().Value = ud.Draws.ToString();
                    else
                        found.SetAttributeValue("Draws", 0);

                    if (found.Attributes("Ratio").First() != null)
                        found.Attributes("Ratio").First().Value = ud.Ratio.ToString();
                    else
                        found.SetAttributeValue("Ratio", 0);

                    if (found.Attributes("CurrentStreak").First() != null)
                        found.Attributes("CurrentStreak").First().Value = ud.CurrentStreak.ToString();
                    else
                        found.SetAttributeValue("CurrentStreak", 0);

                    if (found.Attributes("BestStreak").First() != null)
                        found.Attributes("BestStreak").First().Value = ud.BestStreak.ToString();
                    else
                        found.SetAttributeValue("BestStreak", 0);

                    if (found.Attributes("Resets").First() != null)
                        found.Attributes("Resets").First().Value = ud.Resets.ToString();
                    else
                        found.SetAttributeValue("Resets", 0);

                    if (found.Attributes("Rocks").First() != null)
                        found.Attributes("Rocks").First().Value = ud.RockChosen.ToString();
                    else
                        found.SetAttributeValue("Rocks", 0);

                    if (found.Attributes("Papers").First() != null)
                        found.Attributes("Papers").First().Value = ud.PaperChosen.ToString();
                    else
                        found.SetAttributeValue("Papers", 0);

                    if (found.Attributes("Scissors").First() != null)
                        found.Attributes("Scissors").First().Value = ud.ScissorsChosen.ToString();
                    else
                        found.SetAttributeValue("Scissors", 0);

                    if (found.Attributes("Coins").First() != null)
                        found.Attributes("Coins").First().Value = ud.Coins.ToString();
                    else
                        found.SetAttributeValue("Coins", 0);
                    break;
                }
            }
            xml.Save(guildPath);
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
                xml.Descendants("Users").Descendants(ElementRoot).Descendants("User").Where(x => x.Attribute("UserID")?.Value == ud.UserId.ToString()).Remove();
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
