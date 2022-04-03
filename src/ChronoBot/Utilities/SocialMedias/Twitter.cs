using System;
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

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitter : SocialMedia
    {
        private readonly TwitterService _service;
        private const string IncludeRetweets = "rt";
        private const string OnlyRetweets = "onlyrt";
        private const string IncludeLikes = "l";
        private const string OnlyLikes = "onlyl";
        private const string IncludeQuoteTweets = "q";
        private const string OnlyQuoteTweets = "onlyq";
        private const string IncludeMedia = "m";
        private const string OnlyMedia = "onlym";

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

        private async Task<TwitterStatus> GetLatest(SocialMediaUserData ud)
        {
            List<string> optionsCommands = GetLegitOptions(ud.Options).ToList();
            
            if (optionsCommands.Any(x => x != IncludeLikes || x == OnlyLikes))
            {
                return await GetLatestLike(ud);
            }
            
            ListTweetsOnUserTimelineOptions options = new ListTweetsOnUserTimelineOptions
            {
                ScreenName = ud.Name,
                IncludeRts = optionsCommands.Any(x => x == IncludeRetweets || x == OnlyRetweets) || !optionsCommands.Any(),
                Count = 100,
                TweetMode = "extended"
            };

            var tweets = await _service.ListTweetsOnUserTimelineAsync(options);

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
            if (optionsCommands.Any(x => x == IncludeMedia || x == OnlyMedia))
            {
                if (tweet.ExtendedEntities == null || !tweet.ExtendedEntities.Any() ||
                    !tweet.ExtendedEntities.Media.Any() ||
                    tweet.ExtendedEntities.Media.ElementAt(0).ExtendedEntityType != TwitterMediaType.Photo)
                {
                    if(optionsCommands.All(x => x != OnlyMedia))
                        tweet.Id = -1;
                }
            }

            if (tweet.Id == 0 || tweet.IdStr == "" || tweet.IdStr == null)
            {
                if (optionsCommands.Any(x => x == IncludeLikes))
                    return await GetLatestLike(ud);

                return null;
            }
            
            return await Task.FromResult(tweet);
        }

        private async Task<TwitterStatus> GetLatestRetweet(SocialMediaUserData ud)
        {
            ListFavoriteTweetsOptions options = new ListFavoriteTweetsOptions
            {
                ScreenName = ud.Name,
                Count = 100,
                TweetMode = "extended",
                UserId = long.Parse(ud.Id),
            };
            var likes = await _service.ListFavoriteTweetsAsync(options);

            TwitterStatus[] twitterStatuses;
            try
            {
                twitterStatuses = likes.Value as TwitterStatus[] ?? likes.Value.ToArray();
                if (!twitterStatuses.Any())
                    return null;
            }
            catch
            {
                return null;
            }

            TwitterStatus like = twitterStatuses.ElementAt(0);
            if (like.Id == 0 || like.IdStr == "" || like.IdStr == null)
                return null;

            return await Task.FromResult(like);
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
            var likes = await _service.ListFavoriteTweetsAsync(options);

            TwitterStatus[] twitterStatuses;
            try
            {                            
                twitterStatuses = likes.Value as TwitterStatus[] ?? likes.Value.ToArray();
                if (!twitterStatuses.Any())
                    return null;
            }
            catch
            {
                return null;
            }

            TwitterStatus like = twitterStatuses.ElementAt(0);
            if (like.Id == 0 || like.IdStr == "" || like.IdStr == null)
                return null;

            return await Task.FromResult(like);
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

            TwitterStatus tweet = await GetLatest(Users[i]);
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
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.Twitter || user.GuildId != guildId)
                    continue;
                
                TwitterStatus tweet = await GetLatest(user);
                if (tweet == null || tweet.Id == -1)
                    continue;

                if (MessageDisplayed(tweet.IdStr, user.GuildId)) 
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
                IncludeRetweets, OnlyRetweets,
                IncludeQuoteTweets, OnlyQuoteTweets,
                IncludeLikes, OnlyLikes,
                IncludeMedia, OnlyMedia
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
