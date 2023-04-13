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
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class YouTube : SocialMedia
    {
        private readonly YouTubeService _service;
        private readonly string _channelLink;
        private bool _quotaReached;
        private DateTime _newDay;

        public YouTube(YouTubeService service, DiscordSocketClient client, IConfiguration config,
        IEnumerable<SocialMediaUserData> users, SocialMediaFileSystem fileSystem, int seconds = 600) :
            base(client, config, users, fileSystem, seconds)
        {
            _service = service;
            if (string.IsNullOrEmpty(_service.ApiKey))
                _service = new YouTubeService(new BaseClientService.Initializer
                { ApiKey = Config[Statics.YouTubeApiKey], ApplicationName = "ChronoBot" });

            Hyperlink = "https://www.youtube.com/watch?v=";

            _channelLink = "https://www.youtube.com/@";
            _quotaReached = false;
            _newDay = DateTime.MinValue;

            OnUpdateTimerAsync(seconds);

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.YouTube.ToString().ToLowerInvariant();
        }

        private async Task<string> SearchForYouTuber(string user)
        {
            var searchListRequest = _service.Search.List("snippet");
            searchListRequest.Q = user;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#channel":
                        return await Task.FromResult(searchResult.Snippet.ChannelId);
                }
            }

            return await Task.FromResult(string.Empty);
        }

        private async Task<Tuple<string, bool>> SearchForVideo(string channelId)
        {
            bool live = false;
            IList<SearchResult> searchItems = null;
            for (YouTubeLiveStatus i = 0; i < YouTubeLiveStatus.Max; i++)
            {
                searchItems = await GetSearchItems(channelId, i);
                if (searchItems is not {Count: > 0}) 
                    continue;
                live = i == 0;
                break;
            }

            if (searchItems == null) 
                return await Task.FromResult(new Tuple<string, bool>(string.Empty, false));

            foreach (var searchResult in searchItems)
            {
                if (searchResult.Id.Kind != "youtube#video")
                    continue;

                string videoId = await Task.FromResult(await Task.FromResult(searchResult.Id.VideoId));
                return new Tuple<string, bool>(videoId, live);
            }

            return await Task.FromResult(new Tuple<string, bool>(string.Empty, false));
        }

        private async Task<IList<SearchResult>> GetSearchItems(string channelId, YouTubeLiveStatus status)
        {
            var searchListRequest = _service.Search.List("snippet");
            searchListRequest.ChannelId = channelId;
            searchListRequest.Type = "video";
            searchListRequest.EventType = Enum.GetValues<SearchResource.ListRequest.EventTypeEnum>()
                .ToList().Find(x => string.Equals(x.ToString(), status.ToString(), StringComparison.CurrentCultureIgnoreCase));
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            searchListRequest.PublishedBefore = DateTime.Now;
            searchListRequest.SafeSearch = SearchResource.ListRequest.SafeSearchEnum.None;
            var searchListResponse = await searchListRequest.ExecuteAsync();
            return searchListResponse.Items;
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0, string options = "")
        {
            if (Duplicate(guildId, username, SocialMediaEnum.YouTube))
                return await Task.FromResult($"Already added {username}");

            if (_quotaReached && _newDay >= DateTime.Now)
                return await Task.FromResult("Cannot use YouTube service. Try again tomorrow."); ;

            if (_newDay <= DateTime.Now)
                _quotaReached = false;

            string youtubeChannelId;
            string videoId;
            bool live;
            try
            {
                youtubeChannelId = await SearchForYouTuber(username);
                var videoInfo = await SearchForVideo(youtubeChannelId);
                videoId = videoInfo.Item1;
                live = videoInfo.Item2;
            }
            catch (Exception ex)
            {
                QuotaReach();
                await Statics.SendMessageToLogChannel(Client, $"YOUTUBE\n" + ex.Message);
                return await Task.FromResult("Cannot use YouTube service. Try again tomorrow.");
            }
            if (string.IsNullOrEmpty(youtubeChannelId))
                return await Task.FromResult("Can't find " + username);
            
            if (sendToChannelId == 0)
                sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

            if (!CreateSocialMediaUser(username, guildId, sendToChannelId, youtubeChannelId, SocialMediaEnum.YouTube, videoId, live))
                return await Task.FromResult($"Failed to add {username}.");

            string message = $"Successfully added {username} \n{_channelLink + username}";
            if (string.IsNullOrEmpty(videoId))
                return await Task.FromResult(message);
            return await Task.FromResult($"{message}\n{(live ? "LIVE" : "Latest video")} :\n{Hyperlink + videoId}");
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, string username)
        {
            int i = FindIndexByName(guildId, username, SocialMediaEnum.YouTube);
            if (i == -1)
                return await Task.FromResult("Could not find YouTuber.");

            if (_quotaReached && _newDay >= DateTime.Now)
                return string.Empty;

            if (_newDay <= DateTime.Now)
                _quotaReached = false;

            string videoId;
            bool live;
            try
            {
                var videoInfo = await SearchForVideo(Users[i].Id);
                videoId = videoInfo.Item1;
                live = videoInfo.Item2;
            }
            catch (Exception ex)
            {
                QuotaReach();
                await Statics.SendMessageToLogChannel(Client, $"YOUTUBE\n" + ex.Message);
                return await Task.FromResult("Cannot use YouTube service. Try again tomorrow.");
            }

            if (string.IsNullOrEmpty(videoId)) 
                return await Task.FromResult("Could not retrieve YouTube video.");

            Users[i].Options = videoId;
            Users[i].Live = live;
            FileSystem.UpdateFile(Users[i]);
            return await Task.FromResult(GetYouTuber(Users[i]));

        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, SocialMediaEnum socialMedia, string channelMention = "")
        {
            if (Users.Count == 0 || Users.All(x => x.SocialMedia != socialMedia))
                return await Task.FromResult("No YouTubers registered.");

            string line = string.Empty;
            foreach (var user in Users)
            {
                if (user.SocialMedia != socialMedia)
                    continue;

                bool addToList;
                if (Statics.Debug)
                    addToList = true;
                else
                    addToList = guildId == user.GuildId;

                if (!addToList)
                    continue;
                
                line += $"■ {user.Name} ({_channelLink + user.Name}) {channelMention ?? "***Missing channel info.***"}\n";
            }

            return await Task.FromResult(line);
        }

        public override async Task<string> GetUpdatedSocialMediaUsers(ulong guildId)
        {
            if (Users.Count == 0)
                return await Task.FromResult("No YouTuber registered.");

            if (_quotaReached && _newDay <= DateTime.Now)
                return string.Empty;

            if (_newDay <= DateTime.Now)
                _quotaReached = false;

            List<SocialMediaUserData> newVideos = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.YouTube || user.GuildId != guildId)
                    continue;

                string videoId;
                bool live;
                try
                {
                    var videoInfo = await SearchForVideo(Users[i].Id);
                    videoId = videoInfo.Item1;
                    live = videoInfo.Item2;
                }
                catch (Exception ex)
                {
                    QuotaReach();
                    await Statics.SendMessageToLogChannel(Client, $"YOUTUBE\n" + ex.Message);
                    return await Task.FromResult(string.Empty);
                }

                if (string.IsNullOrEmpty(videoId))
                    continue;
                if (videoId == user.Options && live == user.Live)
                    continue;

                user.Options = videoId;
                user.Live = live;
                Users[i] = user;
                newVideos.Add(Users[i]);
                FileSystem.UpdateFile(user);
            }

            if (newVideos.Count > 0)
                return await UpdateSocialMedia(newVideos);

            return await Task.FromResult("No updates since last time.");
        }


        private void QuotaReach()
        {
            _quotaReached = true;
            _newDay = DateTime.Now.AddDays(1);
        }
    }
}
