using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord;
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
            _users = (List<RpsUserData>) _fileSystem.Load();
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

        public Embed Play(RpsPlayData authorData, RpsPlayData? mentionData, RpsActors botActor = RpsActors.Max)
        {
            RpsActors rpsActor = ConvertInputIntoActor(authorData.Input);
            if (rpsActor == RpsActors.Max)
                return new EmbedBuilder().WithDescription("Wrong input. \nType either rock(r), paper(p), or scissors(s) to play.").Build();

            if (mentionData.HasValue)
                return VsPlayer(authorData, mentionData.Value);

            return VsBot(authorData, botActor);
        }

        public Embed Options(RpsPlayData playData)
        {
            switch (playData.Input)
            {
                case "s":
                case "stats":
                    return ShowStats(playData);
                case "r":
                case "reset":
                    return ResetStats(playData);
                default:
                    return new EmbedBuilder().WithAuthor(TitleBuilder(playData))
                        .WithDescription("Wrong input. \nType stats/s to show your statistics.\nType reset/r to reset the statistics.").Build();
            }
        }

        private Embed ResetStats(RpsPlayData playData)
        {
            if (!Exists(playData.UserId, out RpsUserData ud))
                ud = CreateUser(playData);

            ud.Plays = ud.Wins = ud.Losses = ud.Draws = ud.Ratio = ud.CurrentStreak =
                ud.BestStreak = ud.RockChosen = ud.PaperChosen = ud.ScissorsChosen = ud.Coins = 0;
            ud.Resets++;

            UpdateUsers(ud);
            _fileSystem.UpdateFile(ud);

            var embed = new EmbedBuilder()
                .WithAuthor(TitleBuilder(playData))
                .WithTitle($"Stats for {playData.Username} has been reset.").Build();
            return embed;
        }

        private Embed ShowStats(RpsPlayData playData)
        {
            if (!Exists(playData.UserId, out RpsUserData ud))
                ud = CreateUser(playData);

            string plural = ud.Coins == 1 ? string.Empty : "s";
            var embed = new EmbedBuilder()
                .WithAuthor(TitleBuilder(playData))
                .WithTitle($"Statistics for {playData.Username}")
                .WithFields(new EmbedFieldBuilder { Name = "Plays", Value = ud.Plays, IsInline = true },
                    new EmbedFieldBuilder { Name = "Total Plays", Value = ud.TotalPlays, IsInline = true },
                    new EmbedFieldBuilder { Name = "Wins", Value = ud.Wins, IsInline = true },
                    new EmbedFieldBuilder { Name = "Losses", Value = ud.Losses, IsInline = true },
                    new EmbedFieldBuilder { Name = "Draws", Value = ud.Draws, IsInline = true },
                    new EmbedFieldBuilder { Name = "Win Ratio", Value = ud.Ratio, IsInline = true },
                    new EmbedFieldBuilder { Name = "Current Streak", Value = ud.CurrentStreak, IsInline = true },
                    new EmbedFieldBuilder { Name = "Best Streak", Value = ud.BestStreak, IsInline = true },
                    new EmbedFieldBuilder { Name = "Resets", Value = ud.Resets, IsInline = true },
                    new EmbedFieldBuilder
                    { Name = ConvertActorToEmoji(RpsActors.Rock), Value = ud.RockChosen, IsInline = true },
                    new EmbedFieldBuilder
                    { Name = ConvertActorToEmoji(RpsActors.Paper), Value = ud.PaperChosen, IsInline = true },
                    new EmbedFieldBuilder
                    { Name = ConvertActorToEmoji(RpsActors.Scissors), Value = ud.ScissorsChosen, IsInline = true },
                    new EmbedFieldBuilder { Name = $"Ring{plural}", Value = ud.Coins });

            return embed.Build();
        }

        private Embed VsPlayer(RpsPlayData authorPlayData, RpsPlayData mentionPlayData)
        {
            ulong author = authorPlayData.UserId;
            ulong mention = mentionPlayData.UserId;

            if (author == mention)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription($"{authorPlayData.Mention} " +
                                     "If you have two hands, you can play against yourself that way.");
                return embed.Build();
            }

            if (!Exists(author, out RpsUserData authorUd))
                authorUd = CreateUser(authorPlayData);
            if (!Exists(mention, out RpsUserData mentionUd))
                mentionUd = CreateUser(mentionPlayData);

            if (authorUd.UserIdVs != mention && mentionUd.UserIdVs == 0)
                return Challenging(ConvertInputIntoActor(authorPlayData.Input), authorUd, mentionUd, authorPlayData, mentionPlayData);

            if (authorUd.UserIdVs == mention && mentionUd.Actor != RpsActors.Max)
                return Responding(ConvertInputIntoActor(authorPlayData.Input), authorUd, mentionUd, authorPlayData, mentionPlayData);

            return new EmbedBuilder()
                .WithAuthor(TitleBuilder(authorPlayData))
                .WithColor(Color.Red)
                .WithTitle($"{mentionPlayData.Username} is already in battle.")
                .Build();
        }

        private Embed Challenging(RpsActors playerActor, RpsUserData authorUd, RpsUserData mentionUd, RpsPlayData authorPlayData, RpsPlayData mentionPlayData)
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

            _usersActiveVs.Add(authorUd);
            _usersActiveVs.Add(mentionUd);

            if (!_timerVs.Enabled)
                _timerVs.Start();

            string rps =
                $"{ConvertActorToEmoji(RpsActors.Rock)}{ConvertActorToEmoji(RpsActors.Paper)}{ConvertActorToEmoji(RpsActors.Scissors)}";

            return new EmbedBuilder()
                .WithTitle($"{rps}*GET READY FOR THE NEXT BATTLE*{rps}")
                .WithAuthor(TitleBuilder(authorPlayData))
                .WithDescription($"{authorPlayData.Mention} " +
                                 "**VS** " +
                                 $"{mentionPlayData.Mention}\n\n" +
                                 $"{authorPlayData.Username} has already made a move.")
                .WithFields(new EmbedFieldBuilder { IsInline = true, Name = "Ends", Value = authorUd.DateVs })
                .WithColor(Color.DarkOrange).Build();
        }

        private Embed Responding(RpsActors authorActor, RpsUserData authorUd, RpsUserData mentionUd, RpsPlayData authorPlayData, RpsPlayData mentionPlayData)
        {
            authorUd.Actor = authorActor;
            int mentionActor = (int)mentionUd.Actor;
            string authorMention = authorPlayData.Mention;
            string mentionedMention = mentionPlayData.Mention;
            string result =
                $"{mentionedMention} chose {ConvertActorToEmoji(mentionUd.Actor)}\n" +
                $"{authorMention} chose {ConvertActorToEmoji(authorUd.Actor)}\n\n";

            GameState mentionState, authorState;
            string thumbnailWinner = string.Empty;
            //Responding wins
            if ((mentionActor + 1) % (int)RpsActors.Max == (int)authorActor)
            {
                result += $"{authorMention} wins! {CoinsInKeyText}";
                authorState = GameState.Win;
                mentionState = GameState.Lose;

                thumbnailWinner = authorPlayData.ThumbnailIconUrl;
            }
            //Instigator wins
            else if (((int)authorActor + 1) % (int)RpsActors.Max == mentionActor)
            {
                result += $"{mentionedMention} wins! {CoinsInKeyText}";
                mentionState = GameState.Win;
                authorState = GameState.Lose;
                thumbnailWinner = mentionPlayData.ThumbnailIconUrl;
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

            if (_usersActiveVs.Count == 0)
                _timerVs.Stop();

            return new EmbedBuilder()
                .WithAuthor(TitleBuilder(authorPlayData))
                .WithTitle("*GAME*")
                .WithThumbnailUrl($"attachment://{thumbnailWinner}")
                .WithColor(Color.DarkOrange)
                .WithDescription(result).Build();
        }

        private Embed VsBot(RpsPlayData playData, RpsActors botActor)
        {
            string userMention = playData.Mention;

            Random random = new Random();
            int bot = botActor == RpsActors.Max ? random.Next(0, (int)RpsActors.Max) : (int)botActor;
            RpsActors playerActor = ConvertInputIntoActor(playData.Input);
            int player = (int)playerActor;
            string competition =
                $"{userMention} threw {ConvertActorToEmoji(playerActor)}\nBot threw {ConvertActorToEmoji((RpsActors)bot)}\n\n";

            if (!Exists(playData.UserId, out RpsUserData ud))
                ud = CreateUser(playData);

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

            ud.Actor = playerActor;
            ProcessResults(ud, state, processed, userMention, out processed);

            Color color = new Color(0, 0, 0);
            EmbedFieldBuilder field = null;
            string icon = "";
            switch (state)
            {
                case GameState.Win:
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
                .WithAuthor(TitleBuilder(playData))
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
                        resultText += $"{mentionUser} -0 Rings (empty wallet)";
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

        private RpsUserData CreateUser(RpsPlayData playData)
        {
            RpsUserData newData = new RpsUserData
            {
                Name = playData.Username,
                UserId = playData.UserId,
                GuildId = playData.GuildId,
                ChannelId = playData.ChannelId,
                Actor = ConvertInputIntoActor(playData.Input),
                DateVs = DateTime.Now

            };
            _users.Add(newData);

            _fileSystem.Save(newData);

            return newData;

            //LogToFile(LogSeverity.Info, $"Saved user: RockPaperScissors {temp.UserId} [{socketMessage.Author.Username}] {temp.GuildId} {temp.ChannelId}");
        }

        private string ConvertActorToEmoji(RpsActors actor)
        {
            switch (actor)
            {
                case RpsActors.Rock:
                    return ":rock:";
                case RpsActors.Paper:
                    return ":roll_of_paper:";
                case RpsActors.Scissors:
                    return ":scissors:";
            }

            return string.Empty;
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
        private bool Exists(ulong id, out RpsUserData userData)
        {
            int i = FindIndex(id);
            bool exists = i > -1;
            if (exists)
            {
                userData = _users[i];
                return true;
            }

            userData = null;
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
            Exists(ud.UserId, out int i);
            _users[i] = ud;
        }

        private EmbedAuthorBuilder TitleBuilder(RpsPlayData playData)
        {
            return new EmbedAuthorBuilder().WithName(Title).WithIconUrl($"attachment://{playData.ThumbnailIconUrl}");
        }

        private void LogToFile(LogSeverity severity, string message, Exception e = null, [CallerMemberName] string caller = null)
        {
            StackTrace st = new StackTrace();
            //Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        }
    }
}