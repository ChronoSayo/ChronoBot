using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TwitchLib.Api;

namespace ChronoBot
{
    class Twitch : SocialMedia
    {
        private TwitchAPI _api;
        private string _mineTwitchCommmand;
        /// <summary>
        /// bool is to check if the streamer is online.
        /// </summary>
        private Dictionary<UserData, bool> _streamers;

        public Twitch(DiscordSocketClient client) : base(client)
        {
            _api = new TwitchAPI();
            _api.InitializeAsync("l3ssdhahs25s2aab9fuu2r40hhhn34", "71fp2g750bexnluwvn74kt50a12rmj");
            
            UpdateTimer(60 * 2);//2 minutes.

            string s = "twitch";
            _mineTwitchCommmand = Info.COMMAND_PREFIX + s + "mine";
            SetCommands(s);
            
            _howToMessage = _howToMessage.Replace(_USER_KEYWORD, "streamer");
            _howToMessage += "***\n" + _mineTwitchCommmand + " [name] [channel]\n" +
                    "***-Adds all streamers the user follows based on Discord name." +
                    " Add name if Discord name isn't the same as Twitch name. Max 100 per request.";

            _streamers = new Dictionary<UserData, bool>();
            LoadOrCreateFromFile("streamers");
        }

        private string GetStreamID(string name)
        {
            string foundStreamer = string.Empty;
            try
            {
                var user = _api.Users.v5.GetUserByNameAsync(name).GetAwaiter().GetResult();
                foundStreamer = GetStreamer(name).Id;
            }
            catch
            {
                foundStreamer = string.Empty;
            }

            return foundStreamer;
        }

        private TwitchLib.Api.Models.v5.Users.User GetStreamer(string name)
        {
            return _api.Users.v5.GetUserByNameAsync(name).GetAwaiter().GetResult().Matches.ElementAt(0);
        }

        protected override void UpdateTimer(int seconds)
        {
            base.UpdateTimer(seconds);
        }

        protected override void PostUpdate()
        {
            if (_users.Count == 0)
                return;

            List<UserData> onlineStreamers = new List<UserData>();
            for (int i = 0; i < _streamers.Keys.Count; i++)
            {
                UserData ud = new UserData();
                //Try-catch in case someone was removing a streamer while posting update.
                try
                {
                    ud = _streamers.Keys.ElementAt(i);
                }
                catch
                {
                    return;
                }
                if (_streamers[ud] != _api.Streams.v5.BroadcasterOnlineAsync(ud.id).GetAwaiter().GetResult())
                {
                    _streamers[ud] = _api.Streams.v5.BroadcasterOnlineAsync(ud.id).GetAwaiter().GetResult();
                    if (_streamers[ud] && !onlineStreamers.Contains(ud))
                        onlineStreamers.Add(ud);
                }
            }
            UpdateSocialMedia(onlineStreamers, _api);
        }

        protected override void AddSocialMediaUser(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString();
            if (AddCommand(message))
            {
                string[] split = message.ToLower().Split(' '); //0: Command. 1: Streamer. 2: Channel ID
                if (split.Length == 1)
                {
                    Info.SendMessageToChannel(socketMessage, "Missing Twitch name.");
                    return;
                }

                string addName = split[1];
                ulong guildID = Info.GetGuildIDFromSocketMessage(socketMessage);
                if (!Duplicate(guildID, addName))
                {
                    string streamerID = GetStreamID(addName);
                    if (!string.IsNullOrEmpty(streamerID))
                    {
                        //Use the name displayed from Twitch instead of what the user input.
                        string username = GetStreamer(addName).DisplayName;
                        ulong channelID = Info.DEBUG ? Info.DEBUG_CHANNEL_ID : socketMessage.Channel.Id;
                        if (split.Length > 2 && 
                            split[2].Contains(socketMessage.MentionedChannels.ElementAt(0).Id.ToString()))
                            channelID = socketMessage.MentionedChannels.ElementAt(0).Id;

                        CreateSocialMediaUser(username, guildID, channelID, streamerID, true);

                        Info.SendMessageToChannel(socketMessage, "Successfully added " + username);
                    }
                    else
                        Info.SendMessageToChannel(socketMessage, "Can't find " + addName);
                }
                else
                {
                    UserData ud = _streamers.Keys.ElementAt(FindIndexByName(guildID, addName));
                    Info.SendMessageToChannel(socketMessage, "Already added " + ud.name);
                }
            }
        }

        protected override void DeleteSocialMediaUser(SocketMessage socketMessage, bool altCommand = false)
        {
            string message = socketMessage.ToString();
            if (DeleteCommand(message))
            {
                string[] split = message.Split(' '); //0: Command. 1: Streamer name.
                if (split.Length == 1)
                    return;

                int i = FindIndexByName(Info.GetGuildIDFromSocketMessage(socketMessage), split[1]);
                if (i > -1)
                {
                    UserData ud = _streamers.Keys.ElementAt(i);
                    string formattedLine = FormatLineToFile(ud.name, ud.guildID, ud.channelID, ud.id);
                    string line = _fileSystem.FindLine(formattedLine);
                    string delMessage = "Successfully deleted " + ud.name;
                    if (Info.NoGuildID(ud.guildID))
                        Info.SendMessageToUser(socketMessage.Author, delMessage);
                    else
                        Info.SendMessageToChannel(socketMessage, delMessage);

                    _fileSystem.DeleteLine(line);
                    _streamers.Remove(ud);
                    _users.RemoveAt(i);
                }
            }
        }

        protected override void GetSocialMediaUser(SocketMessage socketMessage)
        {
            if (GetCommand(socketMessage.ToString()))
            {
                if (_streamers.Keys.Count == 0)
                {
                    Info.Shrug(socketMessage);
                    return;
                }
                string[] split = socketMessage.ToString().Split(' '); //0: Command. 1: Streamer display name.
                if (split.Length == 2)
                {
                    int i = FindIndexByName(Info.GetGuildIDFromSocketMessage(socketMessage), split[1]);
                    if (i > -1)
                    {
                        UserData ud = _streamers.Keys.ElementAt(i);
                        string message = GetStreamerURLAndGame(ud, _api);
                        if (Info.NoGuildID(ud.guildID))
                            Info.SendMessageToUser(socketMessage.Author, message);
                        else
                            Info.SendMessageToChannel(socketMessage, message);
                    }
                    else
                        Info.Shrug(socketMessage);
                }
            }
        }

        protected override void ListSavedSocialMediaUsers(SocketMessage socketMessage)
        {
            if (ListCommand(socketMessage.ToString()))
            {
                if (_streamers.Keys.Count == 0)
                {
                    Info.Shrug(socketMessage);
                    return;
                }

                string line = "";
                ulong guildID = Info.DEBUG_GUILD_ID;
                bool privateDM = false;
                for (int i = 0; i < _streamers.Keys.Count; i++)
                {
                    UserData streamer = _streamers.Keys.ElementAt(i);

                    bool addToList = false;
                    if (Info.DEBUG)
                        addToList = true;
                    else
                    {
                        guildID = Info.GetGuildIDFromSocketMessage(socketMessage);
                        addToList = guildID == streamer.guildID;
                        if (Info.NoGuildID(guildID))
                            privateDM = true;
                    }

                    if (addToList)
                    {
                        bool online = _api.Streams.v5.BroadcasterOnlineAsync(streamer.id).GetAwaiter().GetResult();
                        string channelMention = privateDM ? "" :
                            _client.GetGuild(streamer.guildID).GetTextChannel(streamer.channelID).Mention + " ";
                        line += "■ " + streamer.name + " " + channelMention + 
                            (online ? "**ONLINE**" : "OFFLINE") + "\n";
                    }
                }
                if (line == "")
                    Info.Shrug(socketMessage);
                else
                {
                    if (privateDM)
                        Info.SendMessageToUser(socketMessage.Author, line);
                    else
                        Info.SendMessageToChannel(socketMessage, line);
                }
            }
        }


        //Adds all the user's follows into the list and posts them privately.
        private void AddMyTwitchFollows(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString().ToLower();
            if (message.Contains(_mineTwitchCommmand))
            {
                string[] split = message.Split(' '); //0: Command. 1: User's name. 2: Channel.
                if (split.Length > 3)
                    return;

                string streamerName = string.Empty;
                SocketGuildChannel channel = null;
                if (split.Length == 3)
                {
                    channel = GetChannelFromSocketMessage(socketMessage, split[2]);
                    streamerName = split[1];
                }
                else if (split.Length == 2)
                {
                    channel = GetChannelFromSocketMessage(socketMessage, split[1]);
                    if (channel != null)
                        streamerName = socketMessage.Author.Username;
                }
                else
                    streamerName = socketMessage.Author.Username;

                TwitchLib.Api.Models.v5.Users.User mine = GetStreamer(streamerName);
                if (mine == null)
                {
                    Info.SendMessageToChannel(socketMessage, "Cannot find you.");
                    return;
                }

                //Display loading message, then find streamers. If no delay, loading message will delay with the finding code.
                string loading = "Adding streamers. This may take a few seconds depending on how many " +
                    streamerName + " follows.";
                Info.SendMessageToChannel(socketMessage, loading);
                Thread.Sleep(500);

                ulong guildID = Info.NO_GUILD_ID;
                TwitchLib.Api.Models.Helix.Users.GetUsersFollows.GetUsersFollowsResponse getUsersFollows = 
                    _api.Users.helix.GetUsersFollowsAsync(null, null, 100, mine.Id).GetAwaiter().GetResult();
                List<UserData> followers = new List<UserData>();
                foreach (TwitchLib.Api.Models.Helix.Users.GetUsersFollows.Follow f in getUsersFollows.Follows)
                {
                    string name = GetStreamerDisplayNameFromID(f.ToUserId);

                    //If the name has none-English letters, use the English name instead.
                    bool isEnglish = Regex.IsMatch(name, "^[a-zA-Z0-9]*$");
                    if (!isEnglish)
                        name = GetStreamerNameFromID(f.ToUserId);

                    if(channel != null)
                        guildID = Info.GetGuildIDFromSocketMessage(socketMessage);

                    if (!Duplicate(guildID, name))
                    {
                        UserData temp = new UserData
                        {
                            name = name,
                            guildID = guildID,
                            channelID = channel == null ? socketMessage.Author.Id : channel.Id,
                            id = f.ToUserId
                        };
                        followers.Add(temp);
                    }
                }

                if (followers.Count > 0)
                {
                    foreach (UserData ud in followers)
                        CreateSocialMediaUser(ud.name, ud.guildID, ud.channelID, ud.id, true);

                    string confirmed = "Successfully added followings of " + streamerName +
                        ". They will be @ when they go online.";
                    confirmed = confirmed.Replace("@", channel == null ? 
                        "DM'd" : "posted in " + _client.GetGuild(guildID).GetTextChannel(channel.Id).Mention);
                    Info.SendMessageToChannel(socketMessage, confirmed);
                }
                else
                    Info.Shrug(socketMessage);
            }
        }

        private SocketGuildChannel GetChannelFromSocketMessage(SocketMessage socketMessage, string split)
        {
            SocketGuildChannel sgc = null;
            if (split.Contains(socketMessage.MentionedChannels.ElementAt(0).Id.ToString()))
                sgc = socketMessage.MentionedChannels.ElementAt(0);
            return sgc;
        }

        protected override void CreateSocialMediaUser(string name, ulong guildID, ulong channelID, string streamerID, bool saveToFile)
        {
            base.CreateSocialMediaUser(name, guildID, channelID, streamerID, saveToFile);
            _streamers.Add(_users[_users.Count - 1], false);
        }

        private string GetStreamerDisplayNameFromID(string id)
        {
            return _api.Users.v5.GetUserByIDAsync(id).GetAwaiter().GetResult().DisplayName;
        }
        //When a Twitch name has a non-English name, use this instead.
        private string GetStreamerNameFromID(string id)
        {
            return _api.Users.v5.GetUserByIDAsync(id).GetAwaiter().GetResult().Name;
        }

        public override void MessageReceived(SocketMessage socketMessage)
        {
            base.MessageReceived(socketMessage);

            AddMyTwitchFollows(socketMessage);
        }
    }
}
