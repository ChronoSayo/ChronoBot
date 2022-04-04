﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TweetSharp;
using TwitchLib.Api.Core.Models.Undocumented.CSStreams;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitter : SocialMedia
    {
        private readonly TwitterService _service;
        private const string IncludePosts = "p";
        private const string IncludeRetweets = "r";
        private const string IncludeLikes = "l";
        private const string IncludeQuoteTweets = "q";
        private const string IncludeMedia = "m";

        public Twitter(TwitterService service, DiscordSocketClient client, IConfiguration config,
            IEnumerable<SocialMediaUserData> users, SocialMediaFileSystem fileSystem, int seconds = 60) :
            base(client, config, users, fileSystem, seconds)
        {
            _service = service;

            Authenticate();

            OnUpdateTimerAsync(seconds);

            Hyperlink = "https://twitter.com/@name/status/@id";

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.Twitter.ToString().ToLowerInvariant();
        }

        private async Task<TwitterStatus> GetLatestTweet(SocialMediaUserData ud, bool includeRetweets = false, bool media = false)
        {
            var options = await TimelineOptionsAsync(ud.Name, includeRetweets);
            return await ConfirmFetchedTweet(options, media);
        }

        private async Task<TwitterAsyncResult<IEnumerable<TwitterStatus>>> TimelineOptionsAsync(string name, bool includeRetweets)
        {
            var options = new ListTweetsOnUserTimelineOptions
            {
                ScreenName = name,
                IncludeRts = includeRetweets,
                Count = 100
            };
            return await _service.ListTweetsOnUserTimelineAsync(options);
        }

        private async Task<TwitterStatus> GetLatestLike(SocialMediaUserData ud)
        {
            ListFavoriteTweetsOptions options = new ListFavoriteTweetsOptions
            {
                ScreenName = ud.Name,
                Count = 100,
                TweetMode = "extended",
                UserId = long.Parse(ud.Id)
            };

            var tweets = await _service.ListFavoriteTweetsAsync(options);
            return await ConfirmFetchedTweet(tweets, false);
        }

        private async Task<TwitterStatus> ConfirmFetchedTweet(
            TwitterAsyncResult<IEnumerable<TwitterStatus>> tweets, bool media)
        {
            TwitterStatus[] twitterStatuses;
            try
            {
                twitterStatuses = tweets.Value as TwitterStatus[] ?? tweets.Value.ToArray();
                if (!twitterStatuses.Any())
                    return null;
            }
            catch
            {
                return null;
            }

            TwitterStatus tweet = twitterStatuses.ElementAt(0);
            if (media)
            {
                if (tweet.ExtendedEntities == null || !tweet.ExtendedEntities.Any() ||
                    !tweet.ExtendedEntities.Media.Any() ||
                    tweet.ExtendedEntities.Media.ElementAt(0).ExtendedEntityType != TwitterMediaType.Photo)
                    tweet.Id = -1;
            }

            if (tweet.Id == 0 || tweet.IdStr == "" || tweet.IdStr == null)
                return null;

            return await Task.FromResult(tweet);
        }

        private async Task<TwitterStatus> CheckOptionsForLatestTweet(SocialMediaUserData ud, string option)
        {
            TwitterStatus tweet = null;
            if (option == IncludePosts)
                tweet = await GetLatestTweet(ud);
            else if (option == IncludeRetweets)
                tweet = await GetLatestTweet(ud, true);
            else if (option == IncludeMedia)
                tweet = await GetLatestTweet(ud, media: true);
            else if (option == IncludeLikes)
                tweet = await GetLatestLike(ud);

            return tweet;
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username,
            ulong sendToChannelId = 0, string options = "")
        {
            Tuple<string, bool> legit;
            if (!Duplicate(guildId, username, SocialMediaEnum.Twitter))
            {
                legit = await IsLegitTwitterHandle(username);
                var legitUsername = legit.Item1;
                bool isLegit = legit.Item2;
                if (!isLegit)
                    return await Task.FromResult("Can't find " + username);
                if(!GetLegitOptions(options).Any())
                    return await Task.FromResult($"Unrecognizable option: \"{options}\"" + username);
                if (sendToChannelId == 0)
                    sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

                if (!CreateSocialMediaUser(legitUsername, guildId, sendToChannelId, "0", SocialMediaEnum.Twitter, options))
                    return await Task.FromResult($"Failed to add {legitUsername}.");

                return await Task.FromResult("Successfully added " + legitUsername + "\n" + "https://twitter.com/" +
                                             legitUsername);
            }

            legit = await IsLegitTwitterHandle(username);
            return await Task.FromResult("Already added " + legit.Item1);
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, string username)
        {
            int i = FindIndexByName(guildId, username, SocialMediaEnum.Twitter);
            if (i == -1) 
                return await Task.FromResult("Could not find Twitter handle.");

            List<string> options = GetLegitOptions(Users[i].Options).ToList();
            foreach (string option in options)
            {
                TwitterStatus tweet = await CheckOptionsForLatestTweet(Users[i], option);
                if (tweet != null)
                {
                    SocialMediaUserData temp = Users[i];
                    temp.Id = tweet.IdStr;
                    Users[i] = temp;
                    return await Task.FromResult(GetTwitterUrl(Users[i]));
                }
            }

            return await Task.FromResult("Could not retrieve Tweet.");
        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, SocialMediaEnum socialMedia, string channelMention = "")
        {
            if (Users.Count == 0)
                return await Task.FromResult("No Twitter handles registered.");

            return await base.ListSavedSocialMediaUsers(guildId, SocialMediaEnum.Twitter, channelMention);
        }

        public override async Task<string> GetUpdatedSocialMediaUsers(ulong guildId)
        {
            if (Users.Count == 0)
                return await Task.FromResult("No Twitter handles registered.");

            List<SocialMediaUserData> newTweets = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.Twitter || user.GuildId != guildId)
                    continue;

                TwitterStatus tweet = null;
                List<string> options = GetLegitOptions(Users[i].Options).ToList();
                foreach (string option in options)
                {
                    tweet = await CheckOptionsForLatestTweet(Users[i], option);
                    if (tweet == null || tweet.Id == -1)
                        continue;
                    if (MessageDisplayed(tweet.IdStr, user.GuildId))
                        continue;
                }
                if(tweet == null)
                    continue;

                user.Id = tweet.IdStr;
                Users[i] = user;
                newTweets.Add(user);
                FileSystem.UpdateFile(user);
            }

            if (newTweets.Count > 0)
                return await UpdateSocialMedia(newTweets);

            return await Task.FromResult("No updates since last time.");
        }

        private bool MessageDisplayed(string id, ulong guildId)
        {
            foreach (SocialMediaUserData ud in Users)
            {
                if (ud.Id == id && guildId == ud.GuildId)
                    return true;
            }
            return false;
        }

        private async Task<Tuple<string, bool>> IsLegitTwitterHandle(string name)
        {
            TwitterUser tu = await SearchTwitterUser(name);
            bool found = tu != null;
            string screenName = found ? tu.ScreenName : string.Empty;
            var tuple = new Tuple<string, bool>(screenName, found);

            return await Task.FromResult(tuple);
        }

        private async Task<TwitterUser> SearchTwitterUser(string name)
        {
            SearchForUserOptions options = new SearchForUserOptions
            {
                Q = name,
                Count = 1
            };
            try
            {
                var tu = await _service.SearchForUserAsync(options);
                var twitterUsers = tu.Value as TwitterUser[] ?? tu.Value.ToArray();
                return await Task.FromResult(twitterUsers.Any() ? twitterUsers.ElementAt(0) : null);
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<string> GetLegitOptions(string options)
        {
            if (string.IsNullOrWhiteSpace(options))
                return new List<string>();

            IEnumerable<string> optionsList = options.Split(" ").ToList();
            IEnumerable<string> legit = new List<string>
            {
                IncludePosts, IncludeRetweets, IncludeQuoteTweets, IncludeLikes, IncludeMedia
            };

            List<string> legitOptions = new List<string>();
            foreach (string option in optionsList)
            {
                if(legit.Any(x => x == option))
                    legitOptions.Add(option);
            }

            return legitOptions;
        }

        private void Authenticate()
        {
            string consumerKey = Config[Statics.TwitterConsumerKey];
            string consumerSecret = Config[Statics.TwitterConsumerSecret];
            string token = Config[Statics.TwitterToken];
            string secret = Config[Statics.TwitterSecret];
            _service.AuthenticateWith(consumerKey, consumerSecret, token, secret);
        }
    }
}
