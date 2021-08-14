using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            public int WinsBot;
            public int WinsVs;
            public int LossesBot;
            public int LossesVs;
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
            public Actor ActorVs;
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
                action = message.Remove(0, (Info.COMMAND_PREFIX + "rps").Length).Replace(" ", string.Empty);
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
            if (!_users.Exists(x => x.UserId == userId))
                CreateUser(socketMessage);

            int i = _users.FindIndex(x => x.UserId == userId);
            UserData ud = _users[i];

            ud.Plays = ud.WinsBot = ud.WinsVs = ud.LossesBot = ud.LossesVs = ud.Draws = ud.Ratio = ud.CurrentStreak =
                ud.BestStreak = ud.RockChosen = ud.PaperChosen = ud.ScissorsChosen = ud.Coins = 0;
            ud.Resets++;

            _users[i] = ud;
            _fileSystem.UpdateFile(ud);

            Info.SendMessageToChannel(socketMessage, $"Stats for {socketMessage.Author.Mention} has been reset.");
        }

        private void ShowStats(SocketMessage socketMessage)
        {
            ulong userId = socketMessage.Author.Id;
            if (!_users.Exists(x => x.UserId == userId))
                CreateUser(socketMessage);

            UserData ud = _users.Find(x => x.UserId == userId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Stats for {socketMessage.Author.Mention}");
            sb.AppendLine($"**Plays:** {ud.Plays}");
            sb.AppendLine($"**Total Plays:** {ud.TotalPlays}");
            sb.AppendLine($"**Wins vs Bot:** {ud.WinsBot}");
            sb.AppendLine($"**Losses vs Bot:** {ud.LossesBot}");
            sb.AppendLine($"**Wins vs Player:** {ud.WinsVs}");
            sb.AppendLine($"**Losses vs Player:** {ud.LossesVs}");
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
            SocketUser opponent = null;
            try
            {

                opponent = socketMessage.MentionedUsers.ElementAt(0);
            }
            catch
            {
                Info.SendMessageToChannel(socketMessage, "Could not find user.");
                return;
            }

            SocketUser player = socketMessage.Author;

            ulong userIdVs = opponent.Id;
            int i = _users.FindIndex(x => x.UserId == player.Id);
            UserData udPlayer = _users[i];
            if (udPlayer.UserIdVs == userIdVs && udPlayer.DateVs > DateTime.Now)
            {
                Info.SendMessageToChannel(socketMessage, $"Already in battle with {opponent.Mention}. Battle ends: {udPlayer.DateVs}");
                return;
            }

            int j = _users.FindIndex(x => x.UserIdVs == opponent.Id);
            UserData udOpponent = _users[i];

            if (udOpponent.UserIdVs == player.Id)
            {
                GameState state;
                int player1 = (int)udOpponent.ActorVs;
                int player2 = (int)playerActor;
                if ((player1 + 1) % (int)Actor.Max == player2)
                    state = GameState.Win;
                else if ((player2 + 1) % (int)Actor.Max == player1)
                    state = GameState.Lose;
                else
                    state = GameState.Draw;
                ProcessResultsVs(socketMessage);
                return;
            }

            udPlayer.UserIdVs = userIdVs;
            udPlayer.DateVs = DateTime.Now.AddDays(2);

            socketMessage.DeleteAsync().GetAwaiter().GetResult();

            Info.SendMessageToChannel(socketMessage, $"{player.Mention} is challenging {opponent.Mention} in Rock-Paper-Scissors. " +
                                                     $"\n{player.Mention} has already made a move.");
        }

        private void VsBot(Actor playerActor, SocketMessage socketMessage)
        {
            Random random = new Random();
            int bot = random.Next(0, (int)Actor.Max);
            int player = (int)playerActor;
            GameState state;
            if ((bot + 1) % (int)Actor.Max == player)
                state = GameState.Win;
            else if ((player + 1) % (int)Actor.Max == bot)
                state = GameState.Lose;
            else
                state = GameState.Draw;

            ProcessResultsBot(socketMessage, state, playerActor, (Actor)bot);
        }

        private void ProcessResultsVs(SocketMessage socketMessage, UserData player1, UserData player2)
        {

            GameState state;
            int player1 = (int)udOpponent.ActorVs;
            int player2 = (int)playerActor;
            if ((player1 + 1) % (int)Actor.Max == player2)
                state = GameState.Win;
            else if ((player2 + 1) % (int)Actor.Max == player1)
                state = GameState.Lose;
            else
                state = GameState.Draw;
        }

        private void ProcessResultsBot(SocketMessage socketMessage, GameState state, Actor playerActor, Actor botActor)
        {
            string userMention = "You";
            if (socketMessage.Author.Mention != null)
                userMention = socketMessage.Author.Mention;

            string botRespond =
                $"{userMention} threw {ConvertActorToEmoji(playerActor)}\nBot threw {ConvertActorToEmoji(botActor)}\n";

            string imagePath = "Images/RPS/";
            ulong userId = socketMessage.Author.Id;
            if (!_users.Exists(x => x.UserId == userId))
            {
                CreateUser(socketMessage);
            }

            int i = _users.FindIndex(x => x.UserId == userId);
            UserData ud = _users[i];

            ud.Plays++;
            ud.TotalPlays++;

            switch (state)
            {
                case GameState.Win:
                    ud.WinsBot++;
                    ud.CurrentStreak++;

                    int bonus = CalculateStreakBonus(ud.CurrentStreak, ud.Plays);;
                    ud.Coins += bonus;

                    string newRecord = ud.CurrentStreak > ud.BestStreak ? "New streak record!!!" : string.Empty;
                    string streak = ud.CurrentStreak > 1 ? ud.CurrentStreak + $" win streak! {newRecord}" : string.Empty;
                    imagePath += "Lost.png";
                    string plural = ud.Coins == 1 ? string.Empty : "s";
                    botRespond += $"You win!\n+{bonus} Ring{plural}. {streak}";
                    break;
                case GameState.Lose:
                    ud.LossesBot++;

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

            float ratio = (float)(ud.WinsBot + ud.WinsVs) / ud.Plays;
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

        private void CreateUser(SocketMessage socketMessage)
        {
            ulong guildId = Info.GetGuildIDFromSocketMessage(socketMessage);
            ulong channelId = Info.DEBUG ? Info.DEBUG_CHANNEL_ID : socketMessage.Channel.Id;

            UserData temp = new UserData
            {
                UserId = socketMessage.Author.Id,
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
        
        private void LogToFile(LogSeverity severity, string message, Exception e = null, [CallerMemberName] string caller = null)
        {
            StackTrace st = new StackTrace();
            Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        }
    }
}
