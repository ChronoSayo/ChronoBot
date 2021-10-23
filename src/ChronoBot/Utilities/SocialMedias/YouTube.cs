using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.SocialMedias
{
    sealed class YouTube : SocialMedia
    {
        private readonly YouTubeService _service;
        private readonly string _channelLink;

        public YouTube(YouTubeService service, DiscordSocketClient client, IConfiguration config,
        IEnumerable<SocialMediaUserData> users, SocialMediaFileSystem fileSystem, int seconds = 90) :
            base(client, config, users, fileSystem, seconds)
        {
            _service = service;
            if (string.IsNullOrEmpty(_service.ApiKey))
                _service = new YouTubeService(new BaseClientService.Initializer
                    { ApiKey = Statics.YouTubeApiKey, ApplicationName = "ChronoBot" });

            Hyperlink = "https://www.youtube.com/watch?v=";

            _channelLink = "https://www.youtube.com/user/";
            
            OnUpdateTimerAsync(seconds);

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.YouTube.ToString().ToLowerInvariant();
        } 
        
        private List<string> GetVideoId(string username)
        {
            List<string> channelInfo = new List<string>();
            try
            {
                channelInfo = SearchForYouTuber(username, false);
            }
            catch
            {
                try
                {
                    channelInfo = SearchForYouTuber(username, true);
                }
                catch
                {
                    // ignored
                }
            }

            return channelInfo;
        }

        private List<string> SearchForYouTuber(string user, bool checkId)
        {
            List<string> channelInfo = new List<string>();
            ChannelsResource.ListRequest channelListReq = _service.Channels.List("contentDetails");
            if (checkId)
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

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0)
        {
            if (Duplicate(guildId, username, SocialMediaEnum.YouTube))
                return await Task.FromResult($"Already added {username}");

            List<string> ytChannelInfo = GetVideoId(username); //0: Video ID. 1: Channel ID. 2: Username.
            if (ytChannelInfo.Count > 0)
            {
                if (sendToChannelId == 0)
                    sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

                if (!CreateSocialMediaUser(username, guildId, sendToChannelId, "0", SocialMediaEnum.Twitter))
                    return await Task.FromResult($"Failed to add {username}.");

                return await Task.FromResult($"Successfully added {ytChannelInfo[2]} \n{_channelLink + username}");
            }

            return await Task.FromResult("Can't find " + username);
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, ulong channelId, string username)
        {
            int i = FindIndexByName(guildId, username);
            if (i == -1)
                return await Task.FromResult("Could not find YouTuber.");

            List<string> ytChannelInfo = GetVideoId(username);
            if (ytChannelInfo.Count > 0)
                return await Task.FromResult(GetYouTuber(Users[i]));


            return await Task.FromResult("Could not retrieve YouTube channel.");
        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, string channelMention = "")
        {
            if (Users.Count == 0)
                return await Task.FromResult("No YouTubers registered.");

            return await base.ListSavedSocialMediaUsers(guildId, channelMention);
        }

        public override async Task<string> GetUpdatedSocialMediaUsers(ulong guildId)
        {
            if (Users.Count == 0)
                return await Task.FromResult("No YouTuber registered.");

            List<SocialMediaUserData> newVideos = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.YouTube || user.GuildId != guildId)
                    continue;

                List<string> channelInfo = GetVideoId(user.Name);
                if (channelInfo.Count == 0)
                    continue;
                if (channelInfo[0] == user.Id)
                    continue;

                user.Id = channelInfo[0];
                Users[i] = user;
                newVideos.Add(Users[i]);
                FileSystem.UpdateFile(user);
            }

            if(newVideos.Count > 0)
                return await UpdateSocialMedia(newVideos);

            return await Task.FromResult("No updates since last time.");
        }
    }
}
