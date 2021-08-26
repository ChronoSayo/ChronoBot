using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Discord.WebSocket;
using System.Timers;
using ChronoBot.Systems;
using Discord;
using Google.Apis.YouTube.v3.Data;
using TwitchLib.Api.Core.Models.Undocumented.CSStreams;

namespace ChronoBot.Games
{
    class RockPaperScissors
    {
        private readonly RpsFileSystem _fileSystem;
        private Timer _timerVs;
        private readonly List<UserData> _users;
        private readonly List<UserData> _usersActiveVs;
        private const string ImagePath = "Images/RPS/";
        private const string CoinsInKeyText = "&c";

        /// <summary>
        /// R < P < S < R
        /// </summary>
        public enum Actor
        {
            Rock, Paper, Scissors, Max
        }

        public enum GameState
        {
            Win, Lose, Draw, None
        }

        public struct UserData
        {
            public ulong UserId;
            public ulong UserIdVs;
            public ulong GuildId;
            public ulong ChannelId;
            public int Plays;
            public int TotalPlays;
            public int Wins;
            public int Losses;
            public int Draws;
            public int Ratio;
            public int CurrentStreak;
            public int BestStreak;
            public int Resets;
            public int RockChosen;
            public int PaperChosen;
            public int ScissorsChosen;
            public int Coins;
            public DateTime DateVs;
            public Actor Actor;
        }

        public RockPaperScissors()
        {
            _fileSystem = new RpsFileSystem();
            _users = _fileSystem.Load();
            _usersActiveVs = new List<UserData>();
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
            foreach (UserData ud in _usersActiveVs.Where(data => DateTime.Now > data.DateVs))
            {
                UserData temp = ud;
                temp.DateVs = DateTime.Now;
                temp.UserIdVs = 0;
                int i = FindIndex(temp);
                _users[i] = temp;
                _fileSystem.UpdateFile(temp);
            }
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            ProcessCommand(socketMessage);
        }

        private void ProcessCommand(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString().ToLowerInvariant();
            if (!message.StartsWith(Info.COMMAND_PREFIX + "rps"))
                return;

            Actor actor = Actor.Max;
            string action = string.Empty;
            try
            {
                if (message.Contains("|"))
                    message = message.Replace("|", string.Empty);

                if (socketMessage.MentionedUsers.Count == 0)
                    action = message.Remove(0, (Info.COMMAND_PREFIX + "rps").Length).Replace(" ", string.Empty);
                else
                {
                    List<string> split = message.Split(' ').ToList();
                    split.Remove(socketMessage.MentionedUsers.ElementAt(0).Mention);
                    action = split[1];
                }
                actor = ConvertPlayerDecisionToActor(action);
            }
            catch
            {
                // ignore
            }

            if (actor == Actor.Max)
            {
                switch (action)
                {
                    case "stats":
                    case "statistics":
                        ShowStats(socketMessage);
                        break;
                    case "reset":
                        ResetStats(socketMessage);
                        break;
                    default:
                        Info.SendMessageToChannel(socketMessage,
                            "Wrong input. \nType either rock(r), paper(p), or scissors(s) to play." +
                            "\nType statistics(stats) to show stats.\nType reset to reset the statistics.");
                        break;
                }
            }
            else
                ProcessChosenActors(actor, socketMessage);
        }

        private void ResetStats(SocketMessage socketMessage)
        {
            ulong userId = socketMessage.Author.Id;
            if (!Exists(userId))
                CreateUser(socketMessage);

            int i = FindIndex(userId);
            UserData ud = _users[i];

            ud.Plays = ud.Wins = ud.Losses = ud.Draws = ud.Ratio = ud.CurrentStreak =
                ud.BestStreak = ud.RockChosen = ud.PaperChosen = ud.ScissorsChosen = ud.Coins = 0;
            ud.Resets++;

            _users[i] = ud;
            _fileSystem.UpdateFile(ud);

            Info.SendMessageToChannel(socketMessage, $"Stats for {socketMessage.Author.Mention} has been reset.");
        }

        private void ShowStats(SocketMessage socketMessage)
        {
            ulong userId = socketMessage.Author.Id;
            if (!Exists(userId))
                CreateUser(socketMessage);

            UserData ud = Find(userId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Stats for {socketMessage.Author.Mention}");
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

            Info.SendMessageToChannel(socketMessage, sb.ToString());
        }

        private void ProcessChosenActors(Actor playerActor, SocketMessage socketMessage)
        {
            if(socketMessage.MentionedUsers.Count > 0)
                VsPlayer(playerActor, socketMessage);
            else
                VsBot(playerActor, socketMessage);
        }

        private void VsPlayer(Actor playerActor, SocketMessage socketMessage)
        {
            ulong authorId = socketMessage.Author.Id;
            ulong mentionId = socketMessage.MentionedUsers.ElementAt(0).Id;

            if (authorId == mentionId)
            {
                Info.SendMessageToChannel(socketMessage, $"{socketMessage.Author.Mention} " +
                                                         "If you have two hands, you can play against yourself that way.");
                return;
            }

            if(!Exists(authorId))
                CreateUser(socketMessage);
            if (!Exists(mentionId))
                CreateUser(socketMessage, mentionId);

            UserData authorUd = Find(authorId);
            UserData mentionUd = Find(mentionId);
            if (authorUd.UserIdVs != mentionId && mentionUd.UserIdVs == 0)
                Challenging(playerActor, authorUd, mentionUd, socketMessage);
            else if(authorUd.UserIdVs == mentionId && mentionUd.Actor != Actor.Max)
                Responding(authorUd, mentionUd, playerActor, socketMessage);
            else
                Info.SendMessageToChannel(socketMessage, $"{socketMessage.MentionedUsers.ElementAt(0).Username} is already in battle.");
        }

        private void Challenging(Actor playerActor, UserData authorUd, UserData mentionUd, SocketMessage socketMessage)
        {
            authorUd.UserIdVs = mentionUd.UserId;
            authorUd.Actor = playerActor;
            authorUd.DateVs = DateTime.Now.AddDays(1);
            int i = FindIndex(authorUd.UserId);
            _users[i] = authorUd;
            _fileSystem.UpdateFile(authorUd);

            mentionUd.UserIdVs = authorUd.UserId;
            mentionUd.Actor = Actor.Max;
            mentionUd.DateVs = authorUd.DateVs;
            i = FindIndex(mentionUd.UserId);
            _users[i] = mentionUd;
            _fileSystem.UpdateFile(mentionUd);

            string authorMention = socketMessage.Author.Mention;
            Info.DeleteMessageInChannel(socketMessage);
            Info.SendMessageToChannel(socketMessage, 
                $"{authorMention} is challenging " +
                $"{socketMessage.MentionedUsers.ElementAt(0).Mention} in Rock-Paper-Scissors!\n" +
                $"{authorMention} has already made a move.\nBattle ends: {authorUd.DateVs}");

            _usersActiveVs.Add(authorUd);
            _usersActiveVs.Add(mentionUd);

            if(!_timerVs.Enabled)
                _timerVs.Start();
        }

        private void Responding(UserData authorUd, UserData mentionUd, Actor authorActor, SocketMessage socketMessage)
        {
            authorUd.Actor = authorActor;
            int mentionActor = (int)mentionUd.Actor;
            string mention = socketMessage.MentionedUsers.ElementAt(0).Mention;
            string result =
                $"{mention} chose {ConvertActorToEmoji(mentionUd.Actor)}\n" +
                $"{socketMessage.Author.Mention} chose {ConvertActorToEmoji(authorUd.Actor)}\n\n";

            GameState mentionState, authorState;
            //Responding wins
            if ((mentionActor + 1) % (int) Actor.Max == (int) authorActor)
            {
                result += $"{socketMessage.Author.Mention} wins! {CoinsInKeyText}";
                authorState = GameState.Win;
                mentionState = GameState.Lose;
            }
            //Instigator wins
            else if (((int) authorActor + 1) % (int) Actor.Max == mentionActor)
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
            ProcessResults(authorUd, authorState, result, socketMessage.Author.Mention, out result);

            Info.SendMessageToChannel(socketMessage, result);

            _usersActiveVs.Remove(authorUd);
            _usersActiveVs.Remove(mentionUd);
            
            if(_usersActiveVs.Count == 0)
                _timerVs.Stop();
        }

        private void VsBot(Actor playerActor, SocketMessage socketMessage)
        {
            string userMention = "You";
            if (socketMessage.Author.Mention != null)
                userMention = socketMessage.Author.Mention;

            Random random = new Random();
            int bot = random.Next(0, (int)Actor.Max);
            int player = (int)playerActor;
            string result =
                $"{userMention} threw {ConvertActorToEmoji(playerActor)}\nBot threw {ConvertActorToEmoji((Actor)bot)}\n\n";

            string imagePath = ImagePath;
            ulong userId = socketMessage.Author.Id;
            if (!Exists(userId))
            {
                CreateUser(socketMessage);
            }

            GameState state;
            if ((bot + 1) % (int)Actor.Max == player)
            {
                state = GameState.Win;
                imagePath += "Lost.png";
                result += CoinsInKeyText;
            }
            else if ((player + 1) % (int)Actor.Max == bot)
            {
                state = GameState.Lose;
                imagePath += "Win.png";
            }
            else
            {
                state = GameState.Draw;
                imagePath += "Draw.png";
                result += "Draw game.";
            }

            UserData ud = Find(socketMessage.Author.Id);
            ud.Actor = playerActor;
            ProcessResults(ud, state, result, socketMessage.Author.Mention, out result);

            Info.SendFileToChannel(socketMessage, imagePath, result);
        }

        private void ProcessResults(UserData ud, GameState state, string result, string mentionUser, out string resultText)
        {
            resultText = result;
            ud.Plays++;
            ud.TotalPlays++;
            switch (ud.Actor)
            {
                case Actor.Rock:
                    ud.RockChosen++;
                    break;
                case Actor.Paper:
                    ud.PaperChosen++;
                    break;
                case Actor.Scissors:
                    ud.ScissorsChosen++;
                    break;
                case Actor.Max:
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

            ud.Actor = Actor.Max;
            ud.UserIdVs = 0;
            float ratio = (float)ud.Wins / ud.Plays;
            ud.Ratio = (int)(ratio * 100);

            int i = FindIndex(ud);
            _users[i] = ud;
            _fileSystem.UpdateFile(ud);
        }

        private void ProcessResults(SocketMessage socketMessage, GameState state, Actor playerActor, Actor botActor)
        {
            string userMention = "You";
            if (socketMessage.Author.Mention != null)
                userMention = socketMessage.Author.Mention;

            string botRespond =
                $"{userMention} threw {ConvertActorToEmoji(playerActor)}\nBot threw {ConvertActorToEmoji(botActor)}\n";

            string imagePath = ImagePath;
            ulong userId = socketMessage.Author.Id;
            if (!Exists(userId))
            {
                CreateUser(socketMessage);
            }

            int i = FindIndex(userId);
            UserData ud = _users[i];

            ud.Plays++;
            ud.TotalPlays++;

            switch (state)
            {
                case GameState.Win:
                    ud.Wins++;
                    ud.CurrentStreak++;

                    int bonus = CalculateStreakBonus(ud.CurrentStreak, ud.Plays);
                    ud.Coins += bonus;

                    string newRecord = ud.CurrentStreak > ud.BestStreak ? "New streak record!!!" : string.Empty;
                    string streak = ud.CurrentStreak > 1 ? ud.CurrentStreak + $" win streak! {newRecord}" : string.Empty;
                    imagePath += "Lost.png";
                    string plural = ud.Coins == 1 ? string.Empty : "s";
                    botRespond += $"You win!\n+{bonus} Ring{plural}. {streak}";
                    break;
                case GameState.Lose:
                    ud.Losses++;

                    ud.Coins--;
                    bool emptyWallet = ud.Coins <= 0;
                    if (emptyWallet)
                        ud.Coins = 0;
                    if (ud.CurrentStreak > ud.BestStreak)
                        ud.BestStreak = ud.CurrentStreak;
                    ud.CurrentStreak = 0;

                    string loseCoin = emptyWallet ? string.Empty : "\n-1 Ring.";
                    imagePath += "Win.png";
                    botRespond += $"You lost...{loseCoin}";
                    break;
                case GameState.Draw:
                    ud.Draws++;
                    imagePath += "Draw.png";
                    botRespond += "Draw game.";
                    break;
                case GameState.None:
                    LogToFile(LogSeverity.Error, "Wrong game state was given.");
                    break;
                default:
                    LogToFile(LogSeverity.Error, "No game state was given.");
                    break;
            }

            switch (playerActor)
            {
                case Actor.Rock:
                    ud.RockChosen++;
                    break;
                case Actor.Paper:
                    ud.PaperChosen++;
                    break;
                case Actor.Scissors:
                    ud.ScissorsChosen++;
                    break;
                case Actor.Max:
                    LogToFile(LogSeverity.Error, "Wrong actor was given.");
                    break;
                default:
                    LogToFile(LogSeverity.Error, "No actor was given.");
                    break;
            }

            float ratio = (float)ud.Wins / ud.Plays;
            ud.Ratio = (int)(ratio * 100);

            _users[i] = ud;
            _fileSystem.UpdateFile(ud);

            Info.SendFileToChannel(socketMessage, imagePath, botRespond);
        }

        private int CalculateStreakBonus(int streak, int plays)
        {
            int bonus = 1;
            if (streak % 10 == 0)
                bonus = streak + plays;
            return (int)Math.Ceiling(bonus * 0.5f);
        }

        private void CreateUser(SocketMessage socketMessage, ulong id = 0)
        {
            ulong guildId = Info.GetGuildIDFromSocketMessage(socketMessage);
            ulong channelId = Info.DEBUG ? Info.DEBUG_CHANNEL_ID : socketMessage.Channel.Id;

            UserData temp = new UserData
            {
                UserId = id == 0 ? socketMessage.Author.Id : id,
                GuildId = guildId,
                ChannelId = channelId
            };
            _users.Add(temp);

            _fileSystem.Save(temp);

            LogToFile(LogSeverity.Info, $"Saved user: RockPaperScissors {temp.UserId} [{socketMessage.Author.Username}] {temp.GuildId} {temp.ChannelId}");
        }

        private Actor ConvertPlayerDecisionToActor(string actorString)
        {
            switch (actorString.ToLowerInvariant())
            {
                case "r":
                case "rock":
                    return Actor.Rock;
                case "p":
                case "paper":
                    return Actor.Paper;
                case "s":
                case "scissors":
                    return Actor.Scissors;
            }

            return Actor.Max;
        }

        private string ConvertActorToEmoji(Actor a)
        {
            string s = string.Empty;
            switch (a)
            {
                case Actor.Rock:
                    return ":rock:";
                case Actor.Paper:
                    return ":roll_of_paper:";
                case Actor.Scissors:
                    return ":scissors:";
            }
            return s;
        }

        private UserData Find(ulong id)
        {
            return _users.Find(x => x.UserId == id);
        }
        private int FindIndex(UserData ud)
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
            Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        }
    }
}
