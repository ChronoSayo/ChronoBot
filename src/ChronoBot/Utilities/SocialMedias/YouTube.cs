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
    public sealed class YouTube : SocialMedia
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
                    { ApiKey = Config[Statics.YouTubeApiKey], ApplicationName = "ChronoBot" });

            Hyperlink = "https://www.youtube.com/watch?v=";

            _channelLink = "https://www.youtube.com/user/";
            
            OnUpdateTimerAsync(seconds);

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.YouTube.ToString().ToLowerInvariant();
        } 

        private async Task<List<string>> SearchForYouTuber(string user)
        {
            List<string> channelInfo = new List<string>();

            var searchListRequest = _service.Search.List("snippet");
            searchListRequest.Q = user;
            searchListRequest.MaxResults = 5;

            var searchListResponse = await searchListRequest.ExecuteAsync(); 
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#channel":
                        channelInfo.Add(searchResult.Snippet.Title);
                        channelInfo.Add(searchResult.Id.ChannelId);
                        break;
                }
            }

            return await Task.FromResult(channelInfo);
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0)
        {
            if (Duplicate(guildId, username, SocialMediaEnum.YouTube))
                return await Task.FromResult($"Already added {username}");

            var ytChannelInfo = await SearchForYouTuber(username);
            if (ytChannelInfo.Count <= 0) 
                return await Task.FromResult("Can't find " + username);

            string name = ytChannelInfo[0];
            if (sendToChannelId == 0)
                sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

            if (!CreateSocialMediaUser(username, guildId, sendToChannelId, "0", SocialMediaEnum.Twitter))
                return await Task.FromResult($"Failed to add {name}.");

            return await Task.FromResult($"Successfully added {name} \n{_channelLink + name}");

        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, ulong channelId, string username)
        {
            int i = FindIndexByName(guildId, username, SocialMediaEnum.YouTube);
            if (i == -1)
                return await Task.FromResult("Could not find YouTuber.");

            List<string> ytChannelInfo = await SearchForYouTuber(username);
            if (ytChannelInfo.Count > 0)
                return await Task.FromResult(GetYouTuber(Users[i]));


            return await Task.FromResult("Could not retrieve YouTube channel.");
        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, SocialMediaEnum socialMedia, string channelMention = "")
        {
            if (Users.Count == 0)
                return await Task.FromResult("No YouTubers registered.");

            return await base.ListSavedSocialMediaUsers(guildId, SocialMediaEnum.YouTube, channelMention);
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

                List<string> channelInfo = await SearchForYouTuber(user.Name);
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
