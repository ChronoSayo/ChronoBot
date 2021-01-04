using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ChronoBot.Interfaces;
using ChronoBot.Systems;
using ChronoBot.UserDatas;
using Discord;
using Discord.WebSocket;
using SharpLink;
using SharpLink.Enums;

namespace ChronoBot.Tools
{
    class Music : IBotInteraction
    {
        public DiscordSocketClient Client { get; }

        private readonly MusicFileSystem _fileSystem;
        private readonly LavalinkManager _lavalinkManager;
        private readonly List<MusicUserData> _userData;

        public Music(DiscordSocketClient client, LavalinkManager lavalinkManager)
        {
            Client = client;
            _lavalinkManager = lavalinkManager;
            _fileSystem = new MusicFileSystem();

            _userData = new List<MusicUserData>();
            var loadUserData = _fileSystem.Load();
            foreach (IUserData userData in loadUserData)
                _userData.Add((MusicUserData)userData);
        }

        private void ProcessCommand(SocketMessage socketMessage)
        {
            MusicUserData ud = new MusicUserData();
            ulong guildId = Info.GetGuildIDFromSocketMessage(socketMessage);
            if (!_userData.Exists(x => x.GuildId == guildId))
                ud.GuildId = guildId;

            string message = socketMessage.ToString().ToLowerInvariant();
            string command = string.Empty;
            string parameter = string.Empty;
            try
            {
                string[] splits = message.Split(' ');
                command = splits[1];
                if (splits.Length > 2)
                    parameter = string.Join(" ", splits.Skip(2));

            }
            catch
            {
                // ignore
            }

            switch (command)
            {
                case "p":
                case "play":
                    PlayMusic(parameter, guildId, socketMessage).GetAwaiter().GetResult();
                    break;
                case "c":
                case "j":
                case "channel":
                case "join":
                    JoinVoiceChannel(parameter, guildId, socketMessage).GetAwaiter().GetResult();
                    break;
                case "l":
                case "leave":
                    LeaveVoiceChannel(guildId).GetAwaiter().GetResult();
                    break;
                default:
                    Info.SendMessageToChannel(socketMessage, "Wrong input.");
                    break;
            }
        }

        private async Task PlayMusic(string parameter, ulong guildId, SocketMessage socketMessage)
        {
            LavalinkPlayer player = _lavalinkManager.GetPlayer(guildId);

            if (player == null)
            {
                Info.SendMessageToChannel(socketMessage, $"Must join a voice channel first. ({Info.COMMAND_PREFIX}music c [Channel name or ID]\n" +
                                                         "If I'm already in a channel, disconnect me first.");
                return;
            }

            if (player.Playing)
                await player.StopAsync();

            if(string.IsNullOrEmpty(parameter))
            {
                Info.SendMessageToChannel(socketMessage, "Unknown command.");
                return;
            }

            string identifier = string.Empty;
            if (parameter.Contains("youtu"))
                identifier = $"ytsearch:{parameter}";
            else if (parameter.Contains("soundcloud"))
                identifier = $"scsearch:{parameter}";

            LoadTracksResponse response = await _lavalinkManager.GetTracksAsync(identifier);
            switch (response.LoadType)
            {
                case LoadType.LoadFailed:
                    Info.SendMessageToChannel(socketMessage, "Unable to load song.");
                    return;
                case LoadType.TrackLoaded:
                    Info.SendMessageToChannel(socketMessage, $"Playing song: {response.Tracks.ElementAt(0).Title}");
                    break;
                case LoadType.PlaylistLoaded:
                    Info.SendMessageToChannel(socketMessage, "Playing playlist.");
                    break;
                case LoadType.SearchResult:
                    break;
                case LoadType.NoMatches:
                    break;
                case null:
                    break;
            }

            LavalinkTrack track = response.Tracks.First();
            await player.PlayAsync(track);

            Timer loopTimer = new Timer { AutoReset = true, Enabled = true, Interval = track.Length.Seconds + 5000 };
            loopTimer.Elapsed += async (sender, args) =>
            {
                if (player.Playing)
                {
                    loopTimer.Stop();
                    loopTimer.Interval = 5000;
                    loopTimer.Start();
                }
                await player.PlayAsync(track);
                loopTimer.Interval = track.Length.Seconds + 5000;
            };

            int i = _userData.FindIndex(x => x.GuildId == guildId);
            _userData[i].Id = track.TrackId;

            _fileSystem.UpdateFile(_userData[i]);
        }

        private async Task JoinVoiceChannel(string parameter, ulong guildId, SocketMessage socketMessage)
        {
            if(string.IsNullOrEmpty(parameter))
            {
                Info.SendMessageToChannel(socketMessage, "Unknown command.");
                return;
            }

            SocketGuild guild = Client.GetGuild(guildId);
            MusicUserData ud = new MusicUserData {GuildId = guildId, ChannelId = 0};
            foreach (SocketVoiceChannel channel in guild.VoiceChannels)
            {
                if (parameter != channel.Name.ToLowerInvariant() && parameter != channel.Id.ToString())
                    continue;

                ud.ChannelId = channel.Id;
                break;
            }

            if (ud.ChannelId == 0)
            {
                Info.SendMessageToChannel(socketMessage, "Did not find voice channel.");
                return;
            }

            foreach (SocketVoiceChannel channel in Client.GetGuild(guildId).VoiceChannels)
            {
                if(!channel.Users.ToList().Exists(x => x.Id == Info.BotId))
                    continue;

                await LeaveVoiceChannel(guildId);
                break;
            }

            SocketVoiceChannel voiceChannel = Client.GetGuild(guildId).GetVoiceChannel(ud.ChannelId);
            await _lavalinkManager.JoinAsync(voiceChannel);
            ud.Name = voiceChannel.Name;
            Info.SendMessageToChannel(socketMessage, $"Joining voice channel: {ud.Name}");

            ud.Id = string.Empty;
            _fileSystem.Save(ud);
        }

        private async Task LeaveVoiceChannel(ulong guildId)
        {
            if (!_userData.Exists(x => x.GuildId == guildId) || _lavalinkManager.GetPlayer(guildId) == null)
                return;

            await _lavalinkManager.GetPlayer(guildId).StopAsync();
            await _lavalinkManager.LeaveAsync(guildId);

            _fileSystem.DeleteInFile(_userData.Find(x => x.GuildId == guildId));
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString().ToLowerInvariant();
            if (!message.StartsWith(Info.COMMAND_PREFIX + "music"))
                return;

            ProcessCommand(socketMessage);
        }

        public void LogToFile(LogSeverity severity, string message, Exception e = null, string caller = null)
        {
            StackTrace st = new StackTrace();
            Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        }
    }
}
