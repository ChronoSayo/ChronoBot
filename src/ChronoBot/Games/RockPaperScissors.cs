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
using TwitchLib.Api.Core.Models.Undocumented.CSStreams;

namespace ChronoBot.Games
{
    class RockPaperScissors
    {
        private readonly RpsFileSystem _fileSystem;
        private Timer _timerVs;
        private readonly List<UserData> _users;
        private const string ImagePath = "Images/RPS/";

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
            VersusTimer();
        }

        //Timer to check the dates of last player response. 
        private void VersusTimer()
        {
            _timerVs = new Timer(60 * 60 * 1000)
            {
                AutoReset = false, 
                Enabled = false
            }; 
            //_timerVs.Elapsed += ;
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

            if(!Exists(authorId))
                CreateUser(socketMessage);
            if (!Exists(mentionId))
                CreateUser(socketMessage, mentionId);

            UserData authorUd = Find(authorId);
            UserData mentionUd = Find(authorId);
            if (!Exists(mentionId) || authorUd.UserIdVs != mentionId)
                Challenging(playerActor, authorUd, mentionUd, socketMessage);
            else
                Responding(authorUd, mentionUd, playerActor, socketMessage);
        }

        private void Challenging(Actor playerActor, UserData author, UserData mention, SocketMessage socketMessage)
        {
            author.UserIdVs = mention.UserId;
            author.Actor = playerActor;
            author.DateVs = mention.DateVs = DateTime.Now.AddDays(2);
            int i = FindIndex(author.UserId);
            _users[i] = author;

            mention.UserIdVs = author.UserId;
            mention.Actor = Actor.Max;
            i = FindIndex(author.UserId);
            _users[i] = author;

            string authorMention = socketMessage.Author.Mention;
            Info.DeleteMessageInChannel(socketMessage);
            Info.SendMessageToChannel(socketMessage, 
                $"{authorMention} is challenging " +
                $"{socketMessage.MentionedUsers.ElementAt(0).Mention} in Rock-Paper-Scissors!\n" +
                $"{authorMention} has already made a move.\nBattle ends: {author.DateVs}");
        }

        private void Responding(UserData authorUd, UserData mentionUd, Actor authorActor, SocketMessage socketMessage)
        {
            authorUd.Actor = authorActor;
            int mentionActor = (int)mentionUd.Actor;
            string mention = socketMessage.MentionedUsers.ElementAt(0).Mention;
            string result =
                $"{mention} chose {ConvertActorToEmoji(authorUd.Actor)}\n" +
                $"{socketMessage.Author.Mention} chose {ConvertActorToEmoji(mentionUd.Actor)}\n";

            GameState mentionState, authorState;
            //Responding wins
            if ((mentionActor + 1) % (int) Actor.Max == (int) authorActor)
            {
                result += $"{socketMessage.Author.Mention} wins!";
                authorState = GameState.Win;
                mentionState = GameState.Lose;
            }
            //Instigator wins
            else if (((int) authorActor + 1) % (int) Actor.Max == mentionActor)
            {
                result += $"{mention} wins!";
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

                    string newRecord = ud.CurrentStreak > ud.BestStreak ? $"{mentionUser} has a new streak record!!!" : string.Empty;
                    string streak = ud.CurrentStreak > 1 ? ud.CurrentStreak + $" win streak! {newRecord}" : string.Empty;
                    string plural = ud.Coins == 1 ? string.Empty : "s";
                    resultText += $"{mentionUser} win!\n+{bonus} Ring{plural}. {streak}";
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
                    resultText += $"{mentionUser} lost...{loseCoin}";
                    break;
                case GameState.Draw:
                    ud.Draws++;
                    resultText += "Draw game.";
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

        private void VsBot(Actor playerActor, SocketMessage socketMessage)
        {
            string userMention = "You";
            if (socketMessage.Author.Mention != null)
                userMention = socketMessage.Author.Mention;

            Random random = new Random();
            int bot = random.Next(0, (int)Actor.Max);
            int player = (int)playerActor;
            string result =
                $"{userMention} threw {ConvertActorToEmoji(playerActor)}\nBot threw {ConvertActorToEmoji((Actor)bot)}\n";

            string imagePath = ImagePath;
            ulong userId = socketMessage.Author.Id;
            if (!Exists(userId))
            {
                CreateUser(socketMessage);
            }

            GameState state;
            if ((bot + 1) % (int) Actor.Max == player)
            {
                state = GameState.Win;
                imagePath += "Lost.png";
            }
            else if ((player + 1) % (int) Actor.Max == bot)
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
            ProcessResults(ud, state, result, socketMessage.Author.Mention, out result);

            Info.SendFileToChannel(socketMessage, imagePath, result);
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
