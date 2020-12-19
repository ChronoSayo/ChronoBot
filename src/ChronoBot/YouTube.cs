using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace ChronoBot
{
    class YouTube : SocialMedia
    {
        private YouTubeService _service;
        private string _channelLink;
        private string _altCommand;

        public YouTube(DiscordSocketClient client) : base(client)
        {
            _client = client;

            Authenticate();

            SetCommands("youtube");
            _hyperlink = "https://www.youtube.com/watch?v=";

            _channelLink = "https://www.youtube.com/user/";
            _altCommand = Info.COMMAND_PREFIX + "yt";

            UpdateTimer(60 * 5);//5 minutes

            _howToMessage = _howToMessage.Replace(_USER_KEYWORD, "YouTube channel");

            LoadOrCreateFromFile();
        } 
        
        private List<string> GetVideoID(string user)
        {
            List<string> channelInfo = new List<string>();
            try
            {
                channelInfo = SearchForYouTuber(user, false);
            }
            catch
            {
                try
                {
                    channelInfo = SearchForYouTuber(user, true);
                }
                catch
                {
                    Console.WriteLine("NO VIDEO ID FOUND FOR " + user);
                }
            }

            return channelInfo;
        }

        private List<string> SearchForYouTuber(string user, bool checkID)
        {
            List<string> channelInfo = new List<string>();
            ChannelsResource.ListRequest channelListReq = _service.Channels.List("contentDetails");
            if (checkID)
                channelListReq.Id = user;
            else
                channelListReq.ForUsername = user;

            ChannelListResponse channelResponse = channelListReq.Execute();
            string channelID = channelResponse.Items.ElementAt(0).ContentDetails.RelatedPlaylists.Uploads;

            PlaylistItemsResource.ListRequest playlistReq = _service.PlaylistItems.List("snippet");
            playlistReq.PlaylistId = channelID;

            PlaylistItemListResponse listRespond = playlistReq.Execute();
            channelInfo.Add(listRespond.Items.ElementAt(0).Snippet.ResourceId.VideoId);
            channelInfo.Add(listRespond.Items.ElementAt(0).Snippet.ChannelId);
            channelInfo.Add(listRespond.Items.ElementAt(0).Snippet.ChannelTitle);

            return channelInfo;
        }

        protected override void UpdateTimer(int seconds)
        {
            base.UpdateTimer(seconds);
        }

        protected override void PostUpdate()
        {
            if (_users.Count == 0)
                return;

            List<UserData> newVideos = new List<UserData>();
            for (int i = 0; i < _users.Count; i++)
            {
                UserData user = _users[i];
                if(user.socialMedia != "YouTube")
                    continue;

                List<string> channelInfo = GetVideoID(user.name);
                if (channelInfo.Count == 0)
                    continue;
                if (channelInfo[0] == user.id) 
                    continue;

                UpdateData(user, channelInfo, i);
                newVideos.Add(_users[i]);
                _fileSystem.UpdateFile(user);
            }

            UpdateSocialMedia(newVideos);
        }

        private void UpdateData(UserData data, List<string> channelInfo, int i)
        {
            UserData updateEntry = data;
            updateEntry.id = channelInfo[0];
            _users[i] = updateEntry;
        }

        protected override void AddSocialMediaUser(SocketMessage socketMessage)
        {
            if (AddCommand(socketMessage.ToString()) || AltCommandInput(socketMessage.ToString(), "add"))
            {
                string[] split = socketMessage.ToString().Split(' '); //0: Command. 1: Username/Channel ID. 2: Discord Channel

                if (split.Length == 1)
                    return;

                string user = split[1];
                ulong guildID = Info.GetGuildIDFromSocketMessage(socketMessage);
                if (!Duplicate(guildID, user, "YouTube"))
                {
                    List<string> ytChannelInfo = GetVideoID(user); //0: Video ID. 1: Channel ID. 2: Username.
                    if (ytChannelInfo.Count > 0)
                    {
                        ulong channelID = Info.DEBUG ? Info.DEBUG_CHANNEL_ID : socketMessage.Channel.Id;
                        if (split.Length > 2 && split[2].Contains(socketMessage.MentionedChannels.ElementAt(0).Id.ToString()))
                            channelID = socketMessage.MentionedChannels.ElementAt(0).Id;

                        CreateSocialMediaUser(user.ToLower(), guildID, channelID, "0", "Twitter");
                        
                        Info.SendMessageToChannel(guildID, channelID, "Successfully added " + ytChannelInfo[2] + "\n" +
                            _channelLink + user);
                    }
                    else
                        Info.SendMessageToChannel(socketMessage, "Can't find " + user);
                }
                else
                {
                    UserData ud = _users[FindIndexByName(guildID, user)];
                    Info.SendMessageToChannel(socketMessage, "Already added " + ud.name);
                }
            }
        }

        protected override void DeleteSocialMediaUser(SocketMessage socketMessage, bool altCommand = false)
        {
            base.DeleteSocialMediaUser(socketMessage, AltCommandInput(socketMessage.ToString(), "delete"));
        }

        protected override void GetSocialMediaUser(SocketMessage socketMessage)
        {
            if (GetCommand(socketMessage.ToString()) || AltCommandInput(socketMessage.ToString(), "get"))
            {
                if (_users.Count == 0)
                {
                    Info.Shrug(socketMessage);
                    return;
                }
                string[] split = socketMessage.ToString().Split(' '); //0: Command. 1: Youtuber.
                string username = split[1];
                if (split.Length == 2)
                {
                    int i = FindIndexByName(Info.GetGuildIDFromSocketMessage(socketMessage), username);
                    if (i > -1)
                        Info.SendMessageToChannel(socketMessage, _hyperlink.Replace(_USER_KEYWORD, _users[i].id));
                    else
                    {
                        List<string> ytChannelInfo = GetVideoID(username); //0: Video ID. 1: Channel ID. 2: Username.
                        if (ytChannelInfo.Count > 0)
                            Info.SendMessageToChannel(socketMessage, GetYouTuber(_users[i]));
                        else
                            Info.Shrug(socketMessage);
                    }
                }
                else
                    Info.SendMessageToChannel(socketMessage, "Something is wrong with your command.");
            }
        }

        protected override void ListSavedSocialMediaUsers(SocketMessage socketMessage)
        {
            if (ListCommand(socketMessage.ToString()) || AltCommandInput(socketMessage.ToString(), "list"))
            {
                if (_users.Count == 0)
                {
                    Info.Shrug(socketMessage);
                    return;
                }

                string line = "";
                ulong guildID = Info.DEBUG_GUILD_ID;
                for (int i = 0; i < _users.Count; i++)
                {
                    bool addToList = false;
                    if (Info.DEBUG)
                        addToList = true;
                    else
                    {
                        guildID = Info.GetGuildIDFromSocketMessage(socketMessage);
                        addToList = guildID == _users[i].guildID;
                    }
                    if (addToList)
                    {
                        SocketTextChannel channel = _client.GetGuild(guildID).GetTextChannel(_users[i].channelID);
                        line += "■ " + _users[i].name + " " +
                            (channel == null ? "***Missing channel info.***" : channel.Mention) + "\n";
                    }
                }
                if (line == "")
                    Info.Shrug(socketMessage);
                else
                    socketMessage.Channel.SendMessageAsync(line);
            }
        }

        private bool AltCommandInput(string message, string command)
        {
            return message.Contains(_altCommand + command);
        }

        private void Authenticate()
        {
            _service = new YouTubeService(new BaseClientService.Initializer()
            { ApiKey = File.ReadAllText("Memory Card/YouTubeToken.txt"), ApplicationName = "ChronoBot" });
        }

        protected override void CreateSocialMediaUser(string name, ulong guildID, ulong channelId, string id, string socialMedia)
        {
            base.CreateSocialMediaUser(name, guildID, channelId, id, socialMedia);
        }

        public override void MessageReceived(SocketMessage socketMessage)
        {
            base.MessageReceived(socketMessage);
        }
    }
}
