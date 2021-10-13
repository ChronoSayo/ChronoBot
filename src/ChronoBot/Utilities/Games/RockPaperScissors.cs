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
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Color = Discord.Color;

namespace ChronoBot.Utilities.Games
{
    public class RockPaperScissors
    {
        private readonly IConfiguration _config;
        private readonly RpsFileSystem _fileSystem;
        private Timer _timerVs;
        private readonly List<RpsUserData> _users;
        private readonly List<RpsUserData> _usersActiveVs;
        private const string CoinsInKeyText = "&c";
        private const string Title = "ROCK-PAPER-SCISSORS";

        public enum GameState
        {
            Win, Lose, Draw, None
        }

        public RockPaperScissors(IConfiguration config, RpsFileSystem fileSystem)
        {
            _config = config;
            _fileSystem = fileSystem;
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

        public Embed Play(RpsUserData authorData, RpsUserData mentionData, RpsActors botActor = RpsActors.Max)
        {
            RpsActors rpsActor = ConvertInputIntoActor(authorData.Id);
            if (rpsActor == RpsActors.Max)
                return new EmbedBuilder().WithDescription("Wrong input. \nType either rock(r), paper(p), or scissors(s) to play.").Build();

            if (mentionData != null)
                return VsPlayer(authorData, mentionData);

            return VsBot(authorData, botActor);
        }

        public Embed Options(RpsUserData user)
        {
            switch (user.Id)
            {
                case "s":
                case "stats":
                    return ShowStats(user);
                case "r":
                case "reset":
                    return ResetStats(user);
                default:
                    return new EmbedBuilder().WithAuthor(TitleBuilder(user.ThumbnailIconUrl))
                        .WithDescription("Wrong input. \nType stats/s to show your statistics.\nType reset/r to reset the statistics.").Build();
            }
        }

        private Embed ResetStats(RpsUserData user)
        {
            if (!Exists(user.UserId, ref user))
                RegisterNewUser(user);
            
            user.Plays = user.Wins = user.Losses = user.Draws = user.Ratio = user.CurrentStreak =
                user.BestStreak = user.RockChosen = user.PaperChosen = user.ScissorsChosen = user.Coins = 0;
            user.Resets++;

            UpdateUsers(user);
            _fileSystem.UpdateFile(user);

            var embed = new EmbedBuilder()
                .WithAuthor(TitleBuilder(user.ThumbnailIconUrl))
                .WithTitle($"Stats for {user.Id} has been reset.").Build();
            return embed;
        }

        private Embed ShowStats(RpsUserData user)
        {
            if (!Exists(user.UserId, ref user))
                RegisterNewUser(user);
            
            string plural = user.Coins == 1 ? string.Empty : "s";
            var embed = new EmbedBuilder()
                .WithAuthor(TitleBuilder(user.ThumbnailIconUrl))
                .WithTitle($"Statistics for {user.Name}")
                .WithFields(new EmbedFieldBuilder {Name = "Plays", Value = user.Plays, IsInline = true},
                    new EmbedFieldBuilder {Name = "Total Plays", Value = user.TotalPlays, IsInline = true},
                    new EmbedFieldBuilder {Name = "Wins", Value = user.Wins, IsInline = true},
                    new EmbedFieldBuilder {Name = "Losses", Value = user.Losses, IsInline = true},
                    new EmbedFieldBuilder {Name = "Draws", Value = user.Draws, IsInline = true},
                    new EmbedFieldBuilder {Name = "Win Ratio", Value = user.Ratio, IsInline = true},
                    new EmbedFieldBuilder {Name = "Current Streak", Value = user.CurrentStreak, IsInline = true},
                    new EmbedFieldBuilder {Name = "Best Streak", Value = user.BestStreak, IsInline = true},
                    new EmbedFieldBuilder {Name = "Resets", Value = user.Resets, IsInline = true},
                    new EmbedFieldBuilder
                        {Name = ConvertActorToEmoji(RpsActors.Rock), Value = user.RockChosen, IsInline = true},
                    new EmbedFieldBuilder
                        {Name = ConvertActorToEmoji(RpsActors.Paper), Value = user.PaperChosen, IsInline = true},
                    new EmbedFieldBuilder
                        {Name = ConvertActorToEmoji(RpsActors.Scissors), Value = user.ScissorsChosen, IsInline = true},
                    new EmbedFieldBuilder {Name = $"Ring{plural}", Value = user.Coins});
            
            return embed.Build();
        }

        private Embed VsPlayer(RpsUserData authorUd, RpsUserData mentionUd)
        {
            ulong author = authorUd.UserId;
            ulong mention = mentionUd.UserId;

            if (author == mention)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription($"{authorUd.Mention} " +
                                     "If you have two hands, you can play against yourself.");
                return embed.Build();
            }

            if(!Exists(author, ref authorUd))
                RegisterNewUser(authorUd);
            if (!Exists(mention, ref mentionUd))
                RegisterNewUser(mentionUd);

            if (authorUd.UserIdVs != mention && mentionUd.UserIdVs == 0)
                return Challenging(authorUd, mentionUd);

            if (authorUd.UserIdVs == mention && mentionUd.Actor != RpsActors.Max)
                return Responding(authorUd, mentionUd);

            return new EmbedBuilder()
                .WithAuthor(TitleBuilder(authorUd.ThumbnailIconUrl))
                .WithColor(Color.Red)
                .WithTitle($"{mentionUd.Name} is already in battle.")
                .Build();
        }

        private Embed Challenging(RpsUserData authorUd, RpsUserData mentionUd)
        {
            authorUd.UserIdVs = mentionUd.UserId;
            authorUd.Actor = ConvertInputIntoActor(authorUd.Id);
            authorUd.DateVs = DateTime.Now.AddDays(1);
            UpdateUsers(authorUd);

            mentionUd.UserIdVs = authorUd.UserId;
            mentionUd.Actor = RpsActors.Max;
            mentionUd.DateVs = authorUd.DateVs;
            UpdateUsers(mentionUd);

            _usersActiveVs.Add(authorUd);
            _usersActiveVs.Add(mentionUd);

            if(!_timerVs.Enabled)
                _timerVs.Start();

            string rps =
                $"{ConvertActorToEmoji(RpsActors.Rock)}{ConvertActorToEmoji(RpsActors.Paper)}{ConvertActorToEmoji(RpsActors.Scissors)}";

            return new EmbedBuilder()
                .WithTitle($"{rps}*GET READY FOR THE NEXT BATTLE*{rps}")
                .WithAuthor(TitleBuilder(authorUd.ThumbnailIconUrl))
                .WithDescription($"{authorUd.Mention} " +
                                 "**VS** " +
                                 $"{mentionUd.Mention}\n\n" +
                                 $"{authorUd.Name} has already made a move.")
                .WithFields(new EmbedFieldBuilder { IsInline = true, Name = "Ends", Value = authorUd.DateVs })
                .WithColor(Color.DarkOrange).Build();
        }

        private Embed Responding(RpsUserData authorUd, RpsUserData mentionUd)
        {
            authorUd.Actor = ConvertInputIntoActor(authorUd.Id);
            int mentionActor = (int)mentionUd.Actor;
            string authorMention = authorUd.Mention;
            string mentionedMention = mentionUd.Mention;
            string result =
                $"{mentionedMention} chose {ConvertActorToEmoji(mentionUd.Actor)}\n" +
                $"{authorMention} chose {ConvertActorToEmoji(authorUd.Actor)}\n\n";

            GameState mentionState, authorState;
            string thumbnailWinner = string.Empty;
            //Responding wins
            if ((mentionActor + 1) % (int) RpsActors.Max == (int) authorUd.Actor)
            {
                result += $"{authorMention} wins! {CoinsInKeyText}";
                authorState = GameState.Win;
                mentionState = GameState.Lose;
                
                thumbnailWinner = authorUd.ThumbnailIconUrl;
            }
            //Instigator wins
            else if (((int)authorUd.Actor + 1) % (int) RpsActors.Max == mentionActor)
            {
                result += $"{mentionedMention} wins! {CoinsInKeyText}";
                mentionState = GameState.Win;
                authorState = GameState.Lose;
                thumbnailWinner = mentionUd.ThumbnailIconUrl;
            }
            //Draw
            else
            {
                result += "Draw game.";
                mentionState = authorState = GameState.Draw;
            }
            
            ProcessResults(mentionUd, mentionState, result, mentionedMention, out result);
            ProcessResults(authorUd, authorState, result, authorMention, out result);

            _usersActiveVs.Remove(authorUd);
            _usersActiveVs.Remove(mentionUd);
            UpdateUsers(authorUd);
            UpdateUsers(mentionUd);

            if (_usersActiveVs.Count == 0)
                _timerVs.Stop();

            return new EmbedBuilder()
                .WithAuthor(TitleBuilder(authorUd.ThumbnailIconUrl))
                .WithTitle("*GAME*")
                .WithThumbnailUrl(thumbnailWinner)
                .WithColor(Color.DarkOrange)
                .WithDescription(result).Build();
        }

        private Embed VsBot(RpsUserData user, RpsActors botActor)
        {
            string userMention = user.Mention;

            Random random = new Random();
            int bot = botActor == RpsActors.Max ? random.Next(0, (int)RpsActors.Max) : (int)botActor;
            RpsActors playerActor = ConvertInputIntoActor(user.Id);
            int player = (int)playerActor;
            string competition =
                $"{userMention} threw {ConvertActorToEmoji(playerActor)}\nBot threw {ConvertActorToEmoji((RpsActors)bot)}\n\n";

            if (!Exists(user.UserId, ref user))
                RegisterNewUser(user);

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
            
            user.Actor = playerActor;
            ProcessResults(user, state, processed, userMention, out processed);
            
            Color color = new Color(0, 0, 0);
            EmbedFieldBuilder field = null;
            string icon = "";
            switch (state)
            {
                case GameState.Win:
                    color = Color.Green;
                    processed = processed.Replace(userMention ?? string.Empty, "");
                    field = new EmbedFieldBuilder { IsInline = true, Name = "You won", Value = processed };
                    icon = _config[Statics.RpsLoseImage];
                    break;
                case GameState.Lose:
                    color = Color.Red;
                    processed = processed.Replace(userMention ?? string.Empty, "");
                    field = new EmbedFieldBuilder { IsInline = true, Name = "You lost", Value = processed };
                    icon = _config[Statics.RpsWinImage];
                    break;
                case GameState.Draw:
                    color = Color.Gold;
                    field = new EmbedFieldBuilder { IsInline = true, Name = "Draw game", Value = "+0 Rings" };
                    icon = _config[Statics.RpsDrawImage];
                    break;
            }

            return new EmbedBuilder()
                .WithDescription(competition)
                .WithAuthor(TitleBuilder(user.ThumbnailIconUrl))
                .WithTitle("ARCADE MODE")
                .WithColor(color)
                .WithFields(field)
                .WithThumbnailUrl(icon)
                .Build();
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
                    {
                        ud.Coins = 0;
                        resultText += "-0 Rings (empty wallet)";
                    }
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

        public void RegisterNewUser(RpsUserData user)
        {
            _users.Add(user);
            _fileSystem.Save(user);

            //LogToFile(LogSeverity.Info, $"Saved user: RockPaperScissors {temp.UserId} [{socketMessage.Author.Username}] {temp.GuildId} {temp.ChannelId}");
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

        private RpsActors ConvertInputIntoActor(string action)
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
        private bool Exists(ulong id, ref RpsUserData userData)
        {
            int i = FindIndex(id);
            bool exists = i > -1;
            if (exists)
            {
                userData = _users[i];
                return true;
            }
            
            return false;
        }
        private bool Exists(ulong id, out int i)
        {
            i = FindIndex(id);
            bool exists = i > -1;
            return exists;
        }

        private void UpdateUsers(RpsUserData ud)
        {
            if(Exists(ud.UserId, out int i))
                _users[i] = ud;
        }

        private EmbedAuthorBuilder TitleBuilder(string thumbnailIconUrl)
        {
            return new EmbedAuthorBuilder().WithName(Title).WithIconUrl(thumbnailIconUrl);
        }

        private void LogToFile(LogSeverity severity, string message, Exception e = null, [CallerMemberName] string caller = null)
        {
            StackTrace st = new StackTrace();
            //Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        }
    }
}
