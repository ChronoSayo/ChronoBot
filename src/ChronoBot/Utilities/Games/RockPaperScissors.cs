using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord;
using Color = Discord.Color;

namespace ChronoBot.Utilities.Games
{
    public class RockPaperScissors
    {
        private readonly DiscordSocketClient _client;
        private readonly RpsFileSystem _fileSystem;
        private Timer _timerVs;
        private readonly List<RpsUserData> _users;
        private readonly List<RpsUserData> _usersActiveVs;
        private const string ImagePath = "Images/RPS/";
        private const string CoinsInKeyText = "&c";

        public enum GameState
        {
            Win, Lose, Draw, None
        }

        public RockPaperScissors(DiscordSocketClient client)
        {
            _client = client;
            _fileSystem = new RpsFileSystem();
            _users = (List<RpsUserData>)_fileSystem.Load();
            _usersActiveVs = new List<RpsUserData>();
            VersusTimer();
        }

        //Timer to check the dates of last player response. 
        private void VersusTimer()
        {
            _timerVs = new Timer(1000)
            {
                AutoReset = true, 
                Enabled = false
            };
            _timerVs.Elapsed += CheckVsTimers;
        }

        private void CheckVsTimers(object sender, ElapsedEventArgs e)
        {
            foreach (RpsUserData ud in _usersActiveVs.Where(data => DateTime.Now > data.DateVs))
            {
                RpsUserData temp = ud;
                temp.DateVs = DateTime.Now;
                temp.UserIdVs = 0;
                int i = FindIndex(temp);
                _users[i] = temp;
                _fileSystem.UpdateFile(temp);
            }
        }

        public Embed Play(string actor, ulong author, ulong mention, ulong channel, ulong guild)
        {
            RpsActors rpsActor = ConvertActionIntoActor(actor);
            if (rpsActor == RpsActors.Max)
                return new EmbedBuilder()
                    .WithDescription("Wrong input. \nType either rock(r), paper(p), or scissors(s) to play.").Build();

            if (mention != 0)
                return VsPlayer(rpsActor, author, mention, channel, guild);

            return VsBot(rpsActor, author, channel, guild);
        }

        public string Options(string action, ulong user, ulong channel, ulong guild)
        {
            string result;
            switch (action)
            {
                case "s":
                case "stats":
                case "statistics":
                    result = ShowStats(user, channel, guild);
                    break;
                case "r":
                case "reset":
                    result = ResetStats(user, channel, guild);
                    break;
                default:
                    result = "Wrong input. \nType stats/s to show your statistics.\nType reset/r to reset the statistics.";
                    break;
            }
            return result;
        }

        private string ResetStats(ulong user, ulong channel, ulong guild)
        {
            if (!Exists(user))
                CreateUser(user, channel, guild);

            int i = FindIndex(user);
            RpsUserData ud = _users[i];

            ud.Plays = ud.Wins = ud.Losses = ud.Draws = ud.Ratio = ud.CurrentStreak =
                ud.BestStreak = ud.RockChosen = ud.PaperChosen = ud.ScissorsChosen = ud.Coins = 0;
            ud.Resets++;

            _users[i] = ud;
            _fileSystem.UpdateFile(ud);

            return $"Stats for {GetMentionId(guild, channel, user)} has been reset.";
        }

        private string ShowStats(ulong user, ulong channel, ulong guild)
        {
            if (!Exists(user))
                CreateUser(user, channel, guild);

            RpsUserData ud = Find(user);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Stats for {GetMentionId(guild, channel, user)}");
            sb.AppendLine($"**Plays:** {ud.Plays}");
            sb.AppendLine($"**Total Plays:** {ud.TotalPlays}");
            sb.AppendLine($"**Wins:** {ud.Wins}");
            sb.AppendLine($"**Losses:** {ud.Losses}");
            sb.AppendLine($"**Draws:** {ud.Draws}");
            sb.AppendLine($"**Win Ratio:** {ud.Ratio}%");
            sb.AppendLine($"**Current Streak:** {ud.CurrentStreak}");
            sb.AppendLine($"**Best Streak:** {ud.BestStreak}");
            sb.AppendLine($"**Resets:** {ud.Resets}");
            sb.AppendLine($"**Rocks Chosen:** {ud.RockChosen}");
            sb.AppendLine($"**Papers Chosen:** {ud.PaperChosen}");
            sb.AppendLine($"**Scissors Chosen:** {ud.ScissorsChosen}");
            string plural = ud.Coins == 1 ? string.Empty : "s";
            sb.AppendLine($"**Ring{plural}:** {ud.Coins}");

            return sb.ToString();
        }

        private Embed VsPlayer(RpsActors playerActor, ulong author, ulong mention, ulong channel, ulong guild)
        {
            if (author == mention)
            {
                var embed = new EmbedBuilder()
                    .WithDescription($"{GetMentionId(guild, channel, author)} " +
                                     "If you have two hands, you can play against yourself that way.");
                return embed.Build();
            }

            if(!Exists(author))
                CreateUser(author, channel, guild);
            if (!Exists(mention))
                CreateUser(mention, channel, guild);

            RpsUserData authorUd = Find(author);
            RpsUserData mentionUd = Find(mention);
            if (authorUd.UserIdVs != mention && mentionUd.UserIdVs == 0)
                return Challenging(playerActor, authorUd, mentionUd);

            if (authorUd.UserIdVs == mention && mentionUd.Actor != RpsActors.Max)
                return Responding(authorUd, mentionUd, playerActor);

            return new EmbedBuilder()
                .WithDescription($"{_client.GetGuild(guild).GetTextChannel(channel).GetUser(mention).Username} is already in battle.")
                .Build();
        }

        private Embed Challenging(RpsActors playerActor, RpsUserData authorUd, RpsUserData mentionUd)
        {
            authorUd.UserIdVs = mentionUd.UserId;
            authorUd.Actor = playerActor;
            authorUd.DateVs = DateTime.Now.AddDays(1);
            int i = FindIndex(authorUd.UserId);
            _users[i] = authorUd;
            _fileSystem.UpdateFile(authorUd);

            mentionUd.UserIdVs = authorUd.UserId;
            mentionUd.Actor = RpsActors.Max;
            mentionUd.DateVs = authorUd.DateVs;
            i = FindIndex(mentionUd.UserId);
            _users[i] = mentionUd;
            _fileSystem.UpdateFile(mentionUd);

            string authorMention = GetMentionId(authorUd.GuildId, authorUd.ChannelId, authorUd.UserId);
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription($"{authorMention} is challenging " +
                                  $"{GetMentionId(authorUd.GuildId, authorUd.ChannelId, authorUd.UserIdVs)} in Rock-Paper-Scissors!\n" +
                                  $"{authorMention} has already made a move.");
            embed.WithFields(new EmbedFieldBuilder { IsInline = true, Name = "Ends", Value = authorUd.DateVs });
            embed.WithColor(Color.Green);

            _usersActiveVs.Add(authorUd);
            _usersActiveVs.Add(mentionUd);

            if(!_timerVs.Enabled)
                _timerVs.Start();

            return embed.Build();
        }

        private Embed Responding(RpsUserData authorUd, RpsUserData mentionUd, RpsActors authorActor)
        {
            authorUd.Actor = authorActor;
            int mentionActor = (int)mentionUd.Actor;
            string mention = GetMentionId(mentionUd.GuildId, mentionUd.ChannelId, mentionUd.UserId);
            string author = GetMentionId(authorUd.GuildId, authorUd.ChannelId, authorUd.UserId);
            string result =
                $"{mention} chose {ConvertActorToEmoji(mentionUd.Actor)}\n" +
                $"{author} chose {ConvertActorToEmoji(authorUd.Actor)}\n\n";

            GameState mentionState, authorState;
            //Responding wins
            if ((mentionActor + 1) % (int) RpsActors.Max == (int) authorActor)
            {
                result += $"{author} wins! {CoinsInKeyText}";
                authorState = GameState.Win;
                mentionState = GameState.Lose;
            }
            //Instigator wins
            else if (((int) authorActor + 1) % (int) RpsActors.Max == mentionActor)
            {
                result += $"{mention} wins! {CoinsInKeyText}";
                mentionState = GameState.Win;
                authorState = GameState.Lose;
            }
            //Draw
            else
            {
                result += "Draw game.";
                mentionState = authorState = GameState.Draw;
            }

            ProcessResults(mentionUd, mentionState, result, mention, out result);
            ProcessResults(authorUd, authorState, result, mention, out result);
            
            _usersActiveVs.Remove(authorUd);
            _usersActiveVs.Remove(mentionUd);
            
            if(_usersActiveVs.Count == 0)
                _timerVs.Stop();

            return new EmbedBuilder().WithDescription(result).Build();
        }

        private Embed VsBot(RpsActors playerActor, ulong user, ulong channel, ulong guild)
        {
            string userMention = GetMentionId(guild, channel, user);

            Random random = new Random();
            int bot = random.Next(0, (int)RpsActors.Max);
            int player = (int)playerActor;
            string competition =
                $"{userMention} threw {ConvertActorToEmoji(playerActor)}\nBot threw {ConvertActorToEmoji((RpsActors)bot)}\n\n";
            
            if (!Exists(user))
                CreateUser(user, channel, guild);

            GameState state;
            string processed = string.Empty;
            if ((bot + 1) % (int)RpsActors.Max == player)
            {
                state = GameState.Win;
                processed = CoinsInKeyText;
            }
            else if ((player + 1) % (int)RpsActors.Max == bot)
                state = GameState.Lose;
            else
                state = GameState.Draw;

            RpsUserData ud = Find(user);
            ud.Actor = playerActor;
            ProcessResults(ud, state, processed, userMention, out processed);

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(competition);
            switch (state)
            {
                case GameState.Win:
                    embed.WithColor(Color.Green);
                    processed = processed.Replace(userMention ?? string.Empty, "");
                    embed.WithFields(new EmbedFieldBuilder { IsInline = true, Name = "You won", Value = processed });
                    embed.WithThumbnailUrl(
                        "https://cdn.discordapp.com/attachments/891627208089698384/891628032207495178/Lost.png");
                    break;
                case GameState.Lose:
                    embed.WithColor(Color.Red);
                    processed = processed.Replace(userMention ?? string.Empty, "");
                    embed.WithFields(new EmbedFieldBuilder { IsInline = true, Name = "You lost", Value = processed });
                    embed.WithThumbnailUrl(
                        "https://cdn.discordapp.com/attachments/891627208089698384/891628034296279070/Win.png");
                    break;
                case GameState.Draw:
                    embed.WithColor(Color.Gold);
                    embed.WithFields(new EmbedFieldBuilder { IsInline = true, Name = "Draw game", Value = "+0 Rings" });
                    embed.WithThumbnailUrl(
                        "https://cdn.discordapp.com/attachments/891627208089698384/891628029741252648/Draw.png");
                    break;
            }

            return embed.Build();
        }

        private void ProcessResults(RpsUserData ud, GameState state, string result, string mentionUser, out string resultText)
        {
            resultText = result;
            ud.Plays++;
            ud.TotalPlays++;
            switch (ud.Actor)
            {
                case RpsActors.Rock:
                    ud.RockChosen++;
                    break;
                case RpsActors.Paper:
                    ud.PaperChosen++;
                    break;
                case RpsActors.Scissors:
                    ud.ScissorsChosen++;
                    break;
                case RpsActors.Max:
                    LogToFile(LogSeverity.Error, "Wrong actor was given.");
                    break;
                default:
                    LogToFile(LogSeverity.Error, "No actor was given.");
                    break;
            }

            switch (state)
            {
                case GameState.Win:
                    ud.Wins++; 
                    ud.CurrentStreak++;

                    int bonus = CalculateStreakBonus(ud.CurrentStreak, ud.Plays);
                    ud.Coins += bonus;

                    string newRecord = ud.CurrentStreak > ud.BestStreak ? "New streak record!!!" : string.Empty;
                    string streak = ud.CurrentStreak > 1 ? ud.CurrentStreak + $" win streak! {newRecord}" : string.Empty;
                    string plural = bonus == 1 ? string.Empty : "s";
                    resultText = resultText.Replace(CoinsInKeyText, $"+{bonus} Ring{plural}. {streak}\n");
                    break;
                case GameState.Lose:
                    ud.Losses++;

                    ud.Coins--;
                    bool emptyWallet = ud.Coins <= 0;
                    if (emptyWallet)
                        ud.Coins = 0;
                    else
                        resultText += $"{mentionUser} -1 Ring";

                    resultText += "\n";
                    if (ud.CurrentStreak > ud.BestStreak)
                        ud.BestStreak = ud.CurrentStreak;
                    ud.CurrentStreak = 0;
                    break;
                case GameState.Draw:
                    ud.Draws++;
                    break;
                case GameState.None:
                    LogToFile(LogSeverity.Error, "Wrong game state was given.");
                    break;
                default:
                    LogToFile(LogSeverity.Error, "No game state was given.");
                    break;
            }

            ud.Actor = RpsActors.Max;
            ud.UserIdVs = 0;
            float ratio = (float)ud.Wins / ud.Plays;
            ud.Ratio = (int)(ratio * 100);

            int i = FindIndex(ud);
            _users[i] = ud;
            _fileSystem.UpdateFile(ud);
        }

        private int CalculateStreakBonus(int streak, int plays)
        {
            int bonus = 1;
            if (streak % 10 == 0)
                bonus = streak + plays;
            return (int)Math.Ceiling(bonus * 0.5f);
        }

        private void CreateUser(ulong user, ulong channel, ulong guild)
        {
            RpsUserData temp = new RpsUserData
            {
                UserId = user,
                GuildId = guild,
                ChannelId = channel,
                Actor = RpsActors.Max,
                DateVs = DateTime.Now

            };
            _users.Add(temp);

            _fileSystem.Save(temp);

            //LogToFile(LogSeverity.Info, $"Saved user: RockPaperScissors {temp.UserId} [{socketMessage.Author.Username}] {temp.GuildId} {temp.ChannelId}");
        }

        private string GetMentionId(ulong guildId, ulong channelId, ulong userId)
        {
            return _client.GetGuild(guildId).GetTextChannel(channelId).GetUser(userId).Mention;
        }

        private string ConvertActorToEmoji(RpsActors a)
        {
            string s = string.Empty;
            switch (a)
            {
                case RpsActors.Rock:
                    return ":rock:";
                case RpsActors.Paper:
                    return ":roll_of_paper:";
                case RpsActors.Scissors:
                    return ":scissors:";
            }
            return s;
        }

        private RpsActors ConvertActionIntoActor(string action)
        {
            switch (action.ToLowerInvariant())
            {
                case "rock":
                case "r":
                    return RpsActors.Rock;
                case "paper":
                case "p":
                    return RpsActors.Paper;
                case "scissors":
                case "s":
                    return RpsActors.Scissors;
                default:
                    return RpsActors.Max;
            }
        }

        private RpsUserData Find(ulong id)
        {
            return _users.Find(x => x.UserId == id);
        }
        private int FindIndex(RpsUserData ud)
        {
            return _users.FindIndex(x => x.UserId == ud.UserId);
        }
        private int FindIndex(ulong id)
        {
            return _users.FindIndex(x => x.UserId == id);
        }
        private bool Exists(ulong id)
        {
            return _users.Exists(x => x.UserId == id);
        }

        private void LogToFile(LogSeverity severity, string message, Exception e = null, [CallerMemberName] string caller = null)
        {
            StackTrace st = new StackTrace();
            //Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        }
    }
}
