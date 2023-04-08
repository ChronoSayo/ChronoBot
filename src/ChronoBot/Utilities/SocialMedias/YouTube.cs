using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
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
        IEnumerable<SocialMediaUserData> users, IEnumerable<string> availableOptions, SocialMediaFileSystem fileSystem, int seconds = 240) :
            base(client, config, users, availableOptions, fileSystem, seconds)
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

        private async Task<Tuple<string, string>> SearchForYouTuber(string user)
        {
            var searchListRequest = _service.Search.List("snippet");
            searchListRequest.Q = user;
            searchListRequest.MaxResults = 5;

            var searchListResponse = await searchListRequest.ExecuteAsync();
            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#channel":
                        string youtuber = await Task.FromResult(searchResult.Snippet.ChannelTitle);
                        string channelId = await Task.FromResult(searchResult.Snippet.ChannelId);
                        return new Tuple<string, string>(youtuber, channelId);
                }
            }

            return await Task.FromResult(new Tuple<string, string>(string.Empty, string.Empty));
        }

        private async Task<Tuple<string, string>> SearchForVideo(string channelId)
        {
            var searchListRequest = _service.Search.List("snippet");
            searchListRequest.MaxResults = 50;
            searchListRequest.ChannelId = channelId;
            searchListRequest.Type = "video";
            searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            searchListRequest.PublishedBefore = DateTime.Now;
            searchListRequest.SafeSearch = SearchResource.ListRequest.SafeSearchEnum.None;
            var searchListResponse = await searchListRequest.ExecuteAsync();

            foreach (var searchResult in searchListResponse.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        var videoId = await Task.FromResult(searchResult.Id.VideoId);
                        var live = await Task.FromResult(searchResult.Snippet.LiveBroadcastContent);
                        return new Tuple<string, string>(videoId, live);
                }
            }

            return await Task.FromResult(new Tuple<string, string>(string.Empty, string.Empty));
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
            string youtuberId;
            string videoId;
            string live;   
            try
            {
                var channelInfo = await SearchForYouTuber(username);
                youtuber = channelInfo.Item1;
                youtuberId = channelInfo.Item2;
                var videoInfo = await SearchForVideo(youtuberId);
                videoId = videoInfo.Item1;
                live = videoInfo.Item2;
            }
            catch
            {
                _quotaReached = true;
                _newDay = DateTime.Today;
                await Statics.SendMessageToLogChannel(Client, "YouTube quota has been reached.");
                return await Task.FromResult("YouTube service is down. Try again later.");
            }
            if (string.IsNullOrEmpty(youtuber))
                return await Task.FromResult("Can't find " + username);
            
            if (sendToChannelId == 0)
                sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

            if (!CreateSocialMediaUser(username, guildId, sendToChannelId, youtuber, SocialMediaEnum.YouTube, videoId, live != "none"))
                return await Task.FromResult($"Failed to add {youtuber}.");

            string message = $"Successfully added {youtuber} \n{_channelLink + youtuber}";
            if (string.IsNullOrEmpty(videoId))
                return await Task.FromResult(message);
            return await Task.FromResult($"{message}\nLatest video:\n{Hyperlink + videoId}");
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, string username)
        {
            string youtuber = username;
            int i = FindIndexByName(guildId, youtuber, SocialMediaEnum.YouTube);
            if (i == -1)
            {
                var youtuberInfo = await SearchForYouTuber(youtuber);
                youtuber = youtuberInfo.Item1;
                i = FindIndexByName(guildId, youtuber, SocialMediaEnum.YouTube);
                if(i == -1) 
                    return await Task.FromResult("Could not find YouTuber.");
            } 
            
            if (_quotaReached && _newDay == DateTime.Today)
                return await Task.FromResult("YouTube service is down. Try again later.");

            if (_newDay != DateTime.Today)
                _quotaReached = false;

            string videoId;
            string live;
            try
            {
                var videoInfo = await SearchForVideo(Users[i].Id);
                videoId = videoInfo.Item1;
                live = videoInfo.Item2;
            }
            catch
            {
                _quotaReached = true;
                _newDay = DateTime.Today;
                await Statics.SendMessageToLogChannel(Client, "YouTube quota has been reached.");
                return await Task.FromResult("YouTube service is down. Try again later.");
            }
            if (!string.IsNullOrEmpty(videoId))
                return await Task.FromResult(GetYouTuber(Users[i]));

            return await Task.FromResult("Could not retrieve YouTube channel.");
        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, SocialMediaEnum socialMedia, string channelMention = "")
        {
            if (Users.Count == 0 || Users.All(x => x.SocialMedia != socialMedia))
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

                string videoId;
                string live;
                try
                {
                    var videoInfo = await SearchForVideo(Users[i].Id);
                    videoId = videoInfo.Item1;
                    live = videoInfo.Item2;
                }
                catch
                {
                    _quotaReached = true;
                    _newDay = DateTime.Today;
                    await Statics.SendMessageToLogChannel(Client, "YouTube quota has been reached.");
                    UpdateTimer.Interval++;
                    return await Task.FromResult("YouTube service is down. Try again later.");
                }
                
                if (string.IsNullOrEmpty(videoId))
                    continue;
                if (videoId == user.Id)
                    continue;

                user.Id = videoId;
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
