using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ChronoBot.Systems;

namespace ChronoBot.Games
{
    class RockPaperScissors
    {
        private readonly DiscordSocketClient _client;
        private FileSystem _fileSystem;
        private bool _playing, _playingVs;
        private Behavior _behavior;
        private Timer _timerVs;
        private string _resultsVs;
        private readonly List<VersusMode> _usersVS;
        private readonly List<PlayerStat> _players;

        public struct PlayerStat
        {
            public ulong User;
            public ulong Server;
            public int Win, Loss, Games;
        }

        public struct VersusMode
        {
            public User user;
            public Actor actor;
            public ulong privateID;
            public int score;
        }

        /// <summary>
        /// R < P < S < R
        /// </summary>
        public enum Actor
        {
            Rock, Paper, Scissors, MAX
        }

        enum ChronoBotResult
        {
            None, Draw, Win, Lose
        }

        struct Behavior
        {
            public Actor actor;
            public ChronoBotResult result;
        }

        public RockPaperScissors(DiscordSocketClient client)
        {
            _client = client;

            _playing = false;

            _behavior = new Behavior {actor = Actor.MAX, result = ChronoBotResult.None};

            _usersVS = new List<VersusMode>();

            _players = new List<PlayerStat>();

            CreateFile();

            VersusTimer();
        }

        private void VersusTimer()
        {
            _timerVs = new Timer(60 * 60 * 1000)
            {
                AutoReset = false, 
                Enabled = false
            }; 
            _timerVs.Elapsed += _versusTimer_Elapsed;
        }

        private void _versusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<VersusMode> forfeits = new List<VersusMode>();
            for (int i = 0; i < _usersVS.Count; i++)
            {
                if (_usersVS[i].actor == Actor.MAX)
                    forfeits.Add(_usersVS[i]);
            }

            foreach (var forfeit in forfeits)
                _usersVS.Remove(forfeit);

            if (_usersVS.Count > 0)
                ShowScores();
            else
                QuitVs();
        }

        private void QuitVs()
        {
            _timerVs.Stop();
            _playingVs = false;
            _resultsVs = "";
            _usersVS.Clear();
        }

        private void CreateFile()
        {
            _fileSystem = new FileSystem("rps", _client);

            if (_fileSystem.CheckFileExists())
            {
                List<string> newList = _fileSystem.Load();
                if (newList.Count > 0)
                {
                    foreach (string s in newList)
                        AddToStats(s);
                }
            }
        }

        private void AddToStats(string line)
        {
            string[] data = line.Split('|'); //0: ID. 1: Wins. 2: Losses. 3: Games.

            PlayerStat ps = new PlayerStat();
            ps.user = _client.GetServer(Info.DISQURDISTAN).GetUser(ulong.Parse(data[0]));
            ps.Win = int.Parse(data[1]);
            ps.Loss = int.Parse(data[2]);
            ps.Games = int.Parse(data[3]);

            _players[GetIndexFromUser(ps.user)] = ps;

            ActivityLogger.GetInstance.UpdateRPSStats(ps.user, ps.Win - ps.Loss, ps.Games);
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            StartPlaying(socketMessage);
        }

        public PlayerStat GetRPSStats(User user)
        {
            return GetPlayerFromUser(user);
        }

        private void StartPlaying(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString().ToLowerInvariant();
            if (!message.StartsWith(Info.COMMAND_PREFIX + "rps"))
                return;

            string action = message.Remove(0, (Info.COMMAND_PREFIX + "rps").Length).Replace(" ", string.Empty);

            switch (action)
            {
                case "r":
                case "rock":
                    CalculateScore((int)ConvertStringToActor(action));
                    break;
            }

            //string caps = message.Text.ToUpper();
            //_playing = caps == Info.ROCK.ToUpper() || caps == Info.PAPER.ToUpper() || caps == Info.SCISSORS.ToUpper();
            //if (!_playingVs && _playing && !message.IsAuthor)
            //{
            //    _behavior.actor = (Actor)Info.GETRANDOM((int)Actor.Rock, (int)Actor.MAX);// GetActorBehavior();
            //    CalculateScore((int)ConvertStringToActor(message.Text), message);
            //}
            //if (!_playingVs && caps.Contains("!" + Info.CHALLENGE.ToUpper()))
            //{
            //    StartPlayingVS(message);
            //}
            //if (_playingVs && !message.IsAuthor)
            //{
            //    PlayingVS(message);
            //}
        }

        private void StartPlayingVS(Message message)
        {
            IEnumerable<User> users = message.MentionedUsers;
            string pm = (Info.ISFRIDAY ? "Välj " : "Choose ") + Info.ROCK + ", " + Info.PAPER + ", " +
                (Info.ISFRIDAY ? "eller " : "or ") + Info.SCISSORS;
            if (users.Count() > 0)
            {
                _playingVs = true;
                foreach (User u in users)
                {
                    _client.GetServer(Info.DISQURDISTAN).GetUser(u.Id).CreatePMChannel().GetAwaiter().GetResult().SendMessage(pm);
                    AddToVS(u);
                }
            }
            if (_playingVs)
            {
                _timerVs.Start();

                _client.GetServer(Info.DISQURDISTAN).GetUser(message.User.Id).CreatePMChannel().GetAwaiter().GetResult().SendMessage(pm);

                AddToVS(message.User);
            }
        }

        private void AddToVS(User user)
        {
            VersusMode temp;
            temp.actor = Actor.MAX;
            temp.user = user;
            temp.privateID = user.PrivateChannel.Id;
            temp.score = 0;
            _usersVS.Add(temp);
        }

        private void PlayingVS(Message message)
        {
            for (int i = 0; i < _usersVS.Count(); i++)
            {
                if (_usersVS[i].actor != Actor.MAX)
                    continue;
                string caps = message.Text.ToUpper();
                bool playing = caps == Info.ROCK.ToUpper() || caps == Info.PAPER.ToUpper() || caps == Info.SCISSORS.ToUpper();
                if (playing && _usersVS[i].privateID == message.User.PrivateChannel.Id)
                {
                    VersusMode updatedVS = _usersVS[i];
                    updatedVS.actor = ConvertStringToActor(caps);
                    _usersVS[i] = updatedVS;

                    _resultsVs += Info.GETNAME(updatedVS.user.Id) + ": " + updatedVS.actor + "\n";
                }
            }

            ShowScores();
        }

        private void ShowScores()
        {
            bool allResponded = true;
            foreach (VersusMode vm in _usersVS)
            {
                if (vm.actor == Actor.MAX)
                {
                    allResponded = false;
                    break;
                }
            }

            if (allResponded)
            {
                _resultsVs += CalculateVS() + GetLeader();
                _client.GetChannel(Info.DEBUG ? Info.TEST : Info.DISQURDISTAN).SendMessage(_resultsVs);
                QuitVs();
            }
        }

        private string CalculateVS()
        {
            string results = "\n";

            for (int i = 0; i < _usersVS.Count; i++)
            {
                for (int j = 0; j < _usersVS.Count; j++)
                {
                    if (_usersVS[i].user.Id == _usersVS[j].user.Id)
                        continue;

                    int player1 = (int)_usersVS[i].actor;
                    int player2 = (int)_usersVS[j].actor;
                    if ((player2 + 1) % (int)Actor.MAX == player1)
                    {
                        VersusMode update = _usersVS[i];
                        update.score++;
                        _usersVS[i] = update;
                    }
                    //else if ((player1 + 1) % (int)Actor.MAX == player2)
                    //{
                    //    VersusMode update = _usersVS[j];
                    //    update.score++;
                    //    _usersVS[j] = update;
                    //}
                }
                results += Info.GETNAME(_usersVS[i].user.Id) + ": " + _usersVS[i].score + "\n";
            }

            return results;
        }

        private string GetLeader()
        {
            string leaders = "\n\n";

            for (int i = 0; i < _usersVS.Count; i++)
            {
                for (int j = i + 1; j < _usersVS.Count; j++)
                {
                    if (_usersVS[j].score > _usersVS[i].score)
                    {
                        VersusMode temp = _usersVS[i];
                        _usersVS[i] = _usersVS[j];
                        _usersVS[j] = temp;
                    }
                }
            }

            leaders = (Info.ISFRIDAY ? "VINNARE: " : "WINNER: ") + Info.GETNAME(_usersVS[0].user.Id);
            for (int i = 1; i < _usersVS.Count; i++)
            {
                if (_usersVS[0].score == _usersVS[i].score)
                    leaders += ", " + Info.GETNAME(_usersVS[i].user.Id);
                else
                    break;
            }

            return leaders;
        }

        /// <summary>
        /// Based on human behavior, according to science: 
        /// Draw: Random.
        /// Win: Pick loser's.
        /// Lose: Pick the not picked.
        /// </summary>
        private Actor GetActorBehavior()
        {
            int max = (int)Actor.MAX;
            int i = max - 1;
            switch (_behavior.result)
            {
                case ChronoBotResult.None:
                case ChronoBotResult.Draw:
                    i = Info.GETRANDOM(0, max);
                    break;
                case ChronoBotResult.Win:
                case ChronoBotResult.Lose:
                    i = (int)(_behavior.actor - 1) % i;
                    if (i < 0)
                        i += max;
                    break;
            }

            Actor a = (Actor)i;
            return a;
        }

        private void CalculateScore(int player)
        {
            _behavior.result = ChronoBotResult.Draw;
            int knuckles = (int)_behavior.actor;

            if ((knuckles + 1) % (int)Actor.MAX == player)
            {
                ps.Win++;
                _behavior.result = ChronoBotResult.Lose;
            }
            else if ((player + 1) % (int)Actor.MAX == knuckles)
            {
                ps.Loss++;
                _behavior.result = ChronoBotResult.Win;
            }

            string emoji = "";
            int i = -1;
            if (_behavior.result == ChronoBotResult.Win)
                i = 12;
            else if (_behavior.result == ChronoBotResult.Lose)
                i = 8;
            if (i > -1)
            {
                if (message.Server == _client.GetServer(Info.DISQURDISTAN))
                    emoji = $"<:{message.Server.CustomEmojis.ElementAt(i).Name}:{message.Server.CustomEmojis.ElementAt(i).Id}>";
                else
                    emoji = i == 8 ? "Oh no." : "Awright!";
            }

            message.Channel.SendMessage(ConvertActorToString(_behavior.actor) + "\n" +
                ps.Games++ + ": " + message.User.Mention + " " + ps.Win + "-" + ps.Loss + " " +
                message.Channel.GetUser(Info.IDKNUCKLES).NicknameMention + " " + emoji);

            _players[GetIndexFromUser(message.User)] = ps;
            SaveData(ps);

            ActivityLogger.GetInstance.UpdateRPSStats(message.User, ps.Win - ps.Loss, ps.Games);
        }

        private void SaveData(PlayerStat ps)
        {
            string newStats = ps.user.Id + "|" + ps.Win + "|" + ps.Loss + "|" + ps.Games + "|" + ps.user.Name;
            if (_fileSystem.CheckFileExists())
            {
                if (!_fileSystem.CheckFound(ps.user.Id.ToString()))
                    _fileSystem.Save(newStats);
                else
                    _fileSystem.UpdateFile(ps.user.Id.ToString(), newStats);
            }
            else
                _fileSystem.Save(newStats);
        }

        private void CreatePlayer(ulong id)
        {
            PlayerStat temp = new PlayerStat();
            temp.Loss = 0;
            temp.Win = 0;
            temp.user = _client.GetServer(Info.DISQURDISTAN).GetUser(id);
            temp.Games = 1;

            _players.Add(temp);
        }

        private int GetIndexFromUser(User u)
        {
            int index = _players.Count - 1;
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].user.Id == u.Id)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private PlayerStat GetPlayerFromUser(User user)
        {
            PlayerStat temp = _players[_players.Count - 1];

            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].user.Id == user.Id)
                {
                    temp = _players[i];
                    break;
                }
            }
            return temp;
        }

        private Actor ConvertStringToActor(string s)
        {
            Actor a = Actor.MAX;
            if (s.ToUpper() == Info.ROCK.ToUpper())
                a = Actor.Rock;
            else if (s.ToUpper() == Info.PAPER.ToUpper())
                a = Actor.Paper;
            else if (s.ToUpper() == Info.SCISSORS.ToUpper())
                a = Actor.Scissors;
            return a;
        }

        private string ConvertActorToString(Actor a)
        {
            string s = "Something went wrong. Tell Sayo.";
            switch (a)
            {
                case Actor.Rock:
                    s = Info.ROCK;
                    break;
                case Actor.Paper:
                    s = Info.PAPER;
                    break;
                case Actor.Scissors:
                    s = Info.SCISSORS;
                    break;
            }
            return s;
        }

        public bool PlayingRPS
        {
            get { return _playing || _playingVs; }
        }
    }
}
