using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class YouTube : SocialMedia
    {
        private readonly YouTubeService _service;
        private readonly string _channelLink, _videoLink;
        private bool _quotaReached;
        private DateTime _newDay;

        public YouTube(YouTubeService service, DiscordSocketClient client, IConfiguration config,
        IEnumerable<SocialMediaUserData> users, IEnumerable<string> availableOptions, SocialMediaFileSystem fileSystem, int seconds = 240) :
            base(client, config, users, availableOptions, fileSystem, seconds)
        {
            _service = service;
            if (string.IsNullOrEmpty(_service.ApiKey))
                _service = new YouTubeService(new BaseClientService.Initializer
                { ApiKey = Config[Statics.YouTubeApiKey], ApplicationName = "ChronoBot" });

            Hyperlink = "https://www.youtube.com/watch?v=";

            _channelLink = "https://www.youtube.com/@";
            _videoLink = "https://www.youtube.com/watch?v=";
            _quotaReached = false;
            _newDay = DateTime.MinValue;

            OnUpdateTimerAsync(seconds);

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.YouTube.ToString().ToLowerInvariant();
        }

        private async Task<string> SearchForYouTuber(string user)
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
                        return await Task.FromResult(searchResult.Snippet.ChannelTitle);
                }
            }

            return await Task.FromResult(string.Empty);
        }

        private async Task<string> SearchForVideo(string user)
        {
            List<string> channelInfo = new List<string>();

            var searchListRequest = _service.Search.List("snippet");
            searchListRequest.Q = user;
            searchListRequest.MaxResults = 5;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            string channelId = string.Empty;
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#channel":
                        channelId = searchResult.Snippet.ChannelId;
                        break;
                }
            }

            if(string.IsNullOrEmpty(channelId))
                return await Task.FromResult(string.Empty);

            searchListRequest = _service.Search.List("snippet");
            searchListRequest.MaxResults = 5;
            searchListRequest.ChannelId = channelId;
            searchListRequest.Type = "video";
            searchListResponse = await searchListRequest.ExecuteAsync();

            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        return await Task.FromResult(searchResult.Id.VideoId);
                        break;
                }
            }

            return await Task.FromResult(string.Empty);
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0, string options = "")
        {
            if (Duplicate(guildId, username, SocialMediaEnum.YouTube))
                return await Task.FromResult($"Already added {username}");

            if (_quotaReached && _newDay == DateTime.Today)
                return await Task.FromResult("Cannot use YouTube service. Try again tomorrow."); ;

            if (_newDay != DateTime.Today)
                _quotaReached = false;

            string youtuber;
            string videoId;
            try
            {
                youtuber = await SearchForYouTuber(username);
                videoId = await SearchForVideo(youtuber);
            }
            catch
            {
                _quotaReached = true;
                _newDay = DateTime.Today;
                await Statics.SendMessageToLogChannel(Client, "YouTube quota has been reached.");
                return await Task.FromResult("Cannot use YouTube service. Try again tomorrow.");
            }
            if (string.IsNullOrEmpty(youtuber))
                return await Task.FromResult("Can't find " + username);
            
            if (sendToChannelId == 0)
                sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

            if (!CreateSocialMediaUser(username, guildId, sendToChannelId, youtuber, SocialMediaEnum.YouTube))
                return await Task.FromResult($"Failed to add {youtuber}.");

            return await Task.FromResult($"Successfully added {youtuber} \n{_channelLink + youtuber}\nLatest video:\n{_videoLink + videoId}");

        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, ulong channelId, string username)
        {
            int i = FindIndexByName(guildId, username, SocialMediaEnum.YouTube);
            if (i == -1)
                return await Task.FromResult("Could not find YouTuber."); 
            
            if (_quotaReached && _newDay == DateTime.Today)
                return string.Empty;
            
            if (_newDay != DateTime.Today)
                _quotaReached = false;

            string youtuber;
            try
            {
                youtuber = await SearchForYouTuber(username);
            }
            catch
            {
                _quotaReached = true;
                _newDay = DateTime.Today;
                await Statics.SendMessageToLogChannel(Client, "YouTube quota has been reached.");
                return string.Empty;
            }
            if (string.IsNullOrEmpty(youtuber))
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

            if (_quotaReached && _newDay == DateTime.Today)
                return string.Empty;

            if (_newDay != DateTime.Today)
                _quotaReached = false;

            List<SocialMediaUserData> newVideos = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.YouTube || user.GuildId != guildId)
                    continue;

                string youtuber;
                try
                {
                    youtuber = await SearchForYouTuber(user.Name);
                }
                catch
                {
                    _quotaReached = true;
                    _newDay = DateTime.Today;
                    await Statics.SendMessageToLogChannel(Client, "YouTube quota has been reached.");
                    UpdateTimer.Interval++;
                    return string.Empty;
                }
                
                if (string.IsNullOrEmpty(youtuber))
                    continue;
                //if (1231231232130 == user.Id)
                //    continue;

                //user.Id = channelInfo[0];
                Users[i] = user;
                newVideos.Add(Users[i]);
                FileSystem.UpdateFile(user);
            }

            if (newVideos.Count > 0)
                return await UpdateSocialMedia(newVideos);

            return await Task.FromResult("No updates since last time.");
        }
    }
}
