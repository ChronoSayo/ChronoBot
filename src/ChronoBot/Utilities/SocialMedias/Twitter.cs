using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private const string OnlyPosts = "p";
        private const string OnlyRetweets = "r";
        private const string OnlyLikes = "l";
        private const string OnlyQuoteTweets = "q";
        private const string OnlyMedia = "m";
        private bool _rateLimitReached;

        public Twitter(TwitterService service, DiscordSocketClient client, IConfiguration config,
            IEnumerable<SocialMediaUserData> users, IEnumerable<string> availableOptions, 
            SocialMediaFileSystem fileSystem, int seconds = 10) :
            base(client, config, users, availableOptions, fileSystem, seconds)
        {
            _service = service;

            Authenticate();

            OnUpdateTimerAsync(seconds);

            Hyperlink = "https://twitter.com/@name/status/@id";

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.Twitter.ToString().ToLowerInvariant();

            AvailableOptions = new List<string>
            {
                OnlyPosts, OnlyRetweets, OnlyQuoteTweets, OnlyLikes, OnlyMedia
            };

            _rateLimitReached = false;
        }

        private async Task<TwitterStatus> GetLatestTweet(SocialMediaUserData ud)
        {
            List<string> options = GetLegitOptions(ud.Options).ToList();
            if (options.Count == 1 && options[0] == OnlyLikes)
                return await GetLatestLike(ud);

            var timeLineOptions = new ListTweetsOnUserTimelineOptions
            {
                ScreenName = ud.Name,
                IncludeRts = true,
                Count = 100,
                ExcludeReplies = false,
                TweetMode = "extended"
            };

            var tweets = await _service.ListTweetsOnUserTimelineAsync(timeLineOptions); 
            if (tweets.Response.RateLimitStatus.RemainingHits <= 0)
            {
                var wait = tweets.Response.RateLimitStatus.ResetTime.ToUniversalTime() - DateTime.UtcNow;
                await Statics.SendMessageToLogChannel(Client,
                    $"Twitter rate limit exceeded. Reset in {tweets.Response.RateLimitStatus.ResetTime}");
                Thread.Sleep(wait);
            }
            if (tweets?.Value == null)
                return null;

            tweets.Value.ToList().RemoveAll(x => x == null);
            if (tweets.Value.ToList().Count == 0)
                return null;

            List<TwitterStatus> postingTweets = new List<TwitterStatus>();
            foreach (string option in options)
            {
                TwitterStatus found;
                switch (option)
                {
                    case OnlyPosts:
                        found = tweets.Value.ToList()
                            .FirstOrDefault(x => !x.IsRetweeted && !x.IsQuoteStatus && !x.IsFavorited);
                        if(found != null)
                            postingTweets.Add(found);
                        break;
                    case OnlyRetweets:
                        found = tweets.Value.ToList().FirstOrDefault(x => x.IsRetweeted);
                        if (found != null)
                            postingTweets.Add(found);
                        break;
                    case OnlyQuoteTweets:
                        found = tweets.Value.ToList().FirstOrDefault(x => x.IsQuoteStatus);
                        if (found != null)
                            postingTweets.Add(found);
                        break;
                    case OnlyMedia:
                        found = tweets.Value.ToList().FirstOrDefault(x =>
                            x.ExtendedEntities != null && x.ExtendedEntities.Any() &&
                            x.ExtendedEntities.Media.Any() &&
                            x.ExtendedEntities.Media.ElementAt(0).ExtendedEntityType == TwitterMediaType.Photo);
                        if (found != null)
                            postingTweets.Add(found);
                        break;
                    case OnlyLikes:
                        postingTweets.Add(await GetLatestLike(ud));
                        break;
                }
            }

            if (postingTweets.Count == 0)
                return null;
            if (postingTweets.Count == 1)
                return postingTweets[0];

            DateTime latest = DateTime.MinValue;
            TwitterStatus tweet = null;
            foreach (TwitterStatus twitterStatus in postingTweets)
            {
                if (latest >= twitterStatus.CreatedDate)
                    continue;

                latest = twitterStatus.CreatedDate;
                tweet = twitterStatus;
            }

            return await Task.FromResult(tweet);
        }

        private async Task<TwitterStatus> GetLatestLike(SocialMediaUserData ud)
        {
            ListFavoriteTweetsOptions options = new ListFavoriteTweetsOptions
            {
                ScreenName = ud.Name,
                Count = 100,
                UserId = long.Parse(ud.Id)
            };

            var tweets = await _service.ListFavoriteTweetsAsync(options);
            if (tweets?.Value == null || !tweets.Value.Any())
                return null;
            

            return await Task.FromResult(tweets.Value.ElementAt(0));
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

            TwitterStatus tweet = await GetLatestTweet(Users[i]);
            if (tweet != null)
            {
                SocialMediaUserData temp = Users[i];
                temp.Id = tweet.IdStr;
                Users[i] = temp;
                return await Task.FromResult(GetTwitterUrl(Users[i]));
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
            List<SocialMediaUserData> twitterHandles = Users.FindAll(x => x.SocialMedia != SocialMediaEnum.Twitter);
            for (int i = 0; i < twitterHandles.Count; i++)
            {
                SocialMediaUserData user = twitterHandles[i];

                TwitterStatus tweet = await GetLatestTweet(user);
                if (tweet == null || tweet.Id <= -1)
                    continue;
                if(MessageDisplayed(tweet.IdStr, user.GuildId))
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
                return AvailableOptions;

            IEnumerable<string> optionsList = options.Split(" ").ToList();
            List<string> legitOptions = new List<string>();

            foreach (string option in optionsList)
            {
                if(AvailableOptions.Any(x => x == option))
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
