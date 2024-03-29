﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TweetSharp;
using System.Globalization;
using Color = Discord.Color;
using ChronoBot.Common.UserDatas;
using System.Runtime.CompilerServices;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitter : SocialMedia
    {
        private readonly TwitterService _service;
        private readonly Dictionary<List<SocialMediaUserData>, int> _groupedUsers;
        private readonly Dictionary<string, List<TwitterStatus>> _history;
        private readonly List<string> _availableOptions;
        private DateTime _rateLimitResetTime;

        public Twitter(TwitterService service, DiscordSocketClient client, IConfiguration config,
            IEnumerable<SocialMediaUserData> users,
            SocialMediaFileSystem fileSystem, int seconds = 10) :
            base(client, config, users, fileSystem, seconds)
        {
            _service = service;

            Authenticate();

            OnUpdateTimerAsync(seconds);

            Hyperlink = "https://twitter.com/@name/status/@id";
            
            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.Twitter.ToString().ToLowerInvariant();

            _availableOptions = new List<string>();
            foreach (TwitterFiltersEnum.TwitterFilters filter in TwitterFiltersEnum.FiltersList())
            {
                if(filter == TwitterFiltersEnum.TwitterFilters.All)
                    continue;
                _availableOptions.Add(TwitterFiltersEnum.ConvertEnumToFilter(filter));
            }

            _groupedUsers = new Dictionary<List<SocialMediaUserData>, int>();
            GroupTwitterUsers();

            _history = new Dictionary<string, List<TwitterStatus>>();

            _rateLimitResetTime = DateTime.Now;
        }

        private async Task<TwitterStatus> GetLatestTweet(SocialMediaUserData ud)
        {
            List<string> options = ud.Options.Split(" ").ToList();
            if (options.Count == 1 && TwitterFiltersEnum.ConvertStringToEnum(options[0]) == TwitterFiltersEnum.TwitterFilters.Likes)
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
            if (tweets == null)
                return null;
            try
            {
                if (tweets.Response != null && tweets.Response.RateLimitStatus.RemainingHits <= 0)
                {
                    _rateLimitResetTime = tweets.Response.RateLimitStatus.ResetTime;
                    if (_rateLimitResetTime.Year == 1970)
                        return null;
                    var wait = _rateLimitResetTime - DateTime.Now;
                    await Statics.SendEmbedMessageToLogChannel(Client,
                        $"Twitter rate limit exceeded. Reset in {_rateLimitResetTime}", Color.Gold);
                    UpdateTimer.Interval++;
                    Thread.Sleep(wait);
                }
            }
            catch
            {
                return null;
            }

            if (tweets.Value == null)
                return null;

            tweets.Value.ToList().RemoveAll(x => x == null);
            if (tweets.Value.ToList().Count == 0)
                return null;

            List<TwitterStatus> postingTweets = new List<TwitterStatus>();
            foreach (string option in options)
            {
                TwitterStatus found = null;
                var filter = TwitterFiltersEnum.ConvertStringToEnum(option);
                switch (filter)
                {
                    case TwitterFiltersEnum.TwitterFilters.Posts:
                        found = tweets.Value.ToList()
                            .FirstOrDefault(x =>
                                !x.IsRetweeted && !x.IsQuoteStatus && !x.IsFavorited && x.RetweetedStatus == null &&
                                x.QuotedStatus == null);
                        break;
                    case TwitterFiltersEnum.TwitterFilters.Retweets:
                        found = tweets.Value.ToList().FirstOrDefault(x => x.IsRetweeted || x.RetweetedStatus != null);
                        break;
                    case TwitterFiltersEnum.TwitterFilters.QuoteRetweets:
                        found = tweets.Value.ToList().FirstOrDefault(x => x.IsQuoteStatus || x.QuotedStatus != null);
                        break;
                    case TwitterFiltersEnum.TwitterFilters.Pictures:
                    case TwitterFiltersEnum.TwitterFilters.Gif:
                    case TwitterFiltersEnum.TwitterFilters.Video:
                        TwitterMediaType media = filter == TwitterFiltersEnum.TwitterFilters.Pictures ? TwitterMediaType.Photo :
                            filter == TwitterFiltersEnum.TwitterFilters.Gif ? TwitterMediaType.AnimatedGif : TwitterMediaType.Video;
                        found = tweets.Value.ToList().FirstOrDefault(x =>
                            x.ExtendedEntities != null && x.ExtendedEntities.Any() &&
                            x.ExtendedEntities.Media.Any() &&
                            x.ExtendedEntities.Media.ElementAt(0).ExtendedEntityType == media && !x.IsRetweeted &&
                            !x.IsQuoteStatus && !x.IsFavorited && x.RetweetedStatus == null &&
                            x.QuotedStatus == null);
                        break;
                    case TwitterFiltersEnum.TwitterFilters.AllMedia:
                        found = tweets.Value.ToList().FirstOrDefault(x =>
                            x.ExtendedEntities != null && x.ExtendedEntities.Any() &&
                            x.ExtendedEntities.Media.Any() && !x.IsRetweeted && !x.IsQuoteStatus && !x.IsFavorited &&
                            x.RetweetedStatus == null &&
                            x.QuotedStatus == null);
                        break;
                    case TwitterFiltersEnum.TwitterFilters.Likes:
                        found = await GetLatestLike(ud);
                        break;
                }
                if (found != null)
                    postingTweets.Add(found);
            }

            if (postingTweets.Count == 0)
                return null;
            if (postingTweets.Count == 1)
                return postingTweets[0];
            
            postingTweets = postingTweets.OrderByDescending(x => x.CreatedDate).ToList();

            return postingTweets[0];
        }

        private async Task<TwitterStatus> GetLatestLike(SocialMediaUserData ud)
        {
            if (!long.TryParse(ud.Id, out long userId))
                userId = 0;
            ListFavoriteTweetsOptions options = new ListFavoriteTweetsOptions
            {
                ScreenName = ud.Name,
                Count = 100,
                UserId = userId,
                IncludeEntities = true,
                TweetMode = "extended"
            };

            var tweets = await _service.ListFavoriteTweetsAsync(options);
            if (tweets?.Value == null || !tweets.Value.Any())
                return null;

            if(!_history.ToList().Exists(x => x.Key == ud.Name))
                _history.Add(ud.Name, tweets.Value.ToList());
            else
            {
                var newTweets = tweets.Value.
                   Where(x => _history[ud.Name].All(y => x.IdStr != y.IdStr)).ToList();

                if (!newTweets.Any()) 
                    return await Task.FromResult(_history[ud.Name].ElementAt(0));

                _history[ud.Name].Insert(0, newTweets.ElementAt(0));
                _history[ud.Name] = _history[ud.Name].Distinct().ToList();
            }
            
            return await Task.FromResult(_history[ud.Name].ElementAt(0));
        }

        protected override async Task AutoUpdate()
        {
            GroupTwitterUsers();
            if (_groupedUsers.Count == 0)
                return;

            List<SocialMediaUserData> newTweets = new List<SocialMediaUserData>();
            for (int i = 0; i < _groupedUsers.Count; i++)
            {
                int currentUser = _groupedUsers.Values.ElementAt(i);
                var user = _groupedUsers.Keys.ElementAt(i)[currentUser];
                TwitterStatus tweet = await GetLatestTweet(user);
                if (tweet != null && tweet.Id != 0 && !string.IsNullOrEmpty(tweet.IdStr) && !MessageDisplayed(tweet.IdStr, user.GuildId))
                {
                    user.Id = tweet.IdStr;
                    int userIndex = FindIndexByName(user.GuildId, user.Name, SocialMediaEnum.Twitter);
                    Users[userIndex] = user;
                    newTweets.Add(user);
                    FileSystem.UpdateFile(user);
                }

                _groupedUsers[_groupedUsers.Keys.ElementAt(i)] += 1;
                if(_groupedUsers[_groupedUsers.Keys.ElementAt(i)] == _groupedUsers.Keys.ElementAt(i).Count)
                    _groupedUsers[_groupedUsers.Keys.ElementAt(i)] = 0;
            }

            if (newTweets.Any())
            {
                await UpdateSocialMedia(newTweets);
                foreach (SocialMediaUserData ud in newTweets)
                {
                    string video = await PostVideo(GetTwitterUrl(ud));
                    if (!string.IsNullOrEmpty(video))
                        await Client.GetGuild(ud.GuildId).GetTextChannel(ud.ChannelId).SendMessageAsync(video);
                }
            }
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
                List<string> legitOptions = GetLegitFilters(options).ToList();
                if (!legitOptions.Any())
                    return await Task.FromResult($"Unrecognizable option: \"{options}\"");
                if (sendToChannelId == 0)
                    sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

                bool newEntry = Users.Exists(x => x.GuildId == guildId);

                if (!CreateSocialMediaUser(legitUsername, guildId, sendToChannelId, "0", SocialMediaEnum.Twitter, legitOptions))
                    return await Task.FromResult($"Failed to add {legitUsername}.");

                if (newEntry)
                    GroupTwitterUsers();

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

            if (DateTime.Now < _rateLimitResetTime)
                return await Task.FromResult("Twitter service not available until: " +
                                             $"{_rateLimitResetTime}");

            TwitterStatus tweet = await GetLatestTweet(Users[i]);
            if (tweet != null && tweet.Id != 0 && !string.IsNullOrEmpty(tweet.IdStr))
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

            if (DateTime.Now < _rateLimitResetTime)
                return await Task.FromResult("Twitter service not available until: " +
                                             $"{_rateLimitResetTime}");

            List<SocialMediaUserData> newTweets = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.Twitter || user.GuildId != guildId)
                    continue;

                TwitterStatus tweet = await GetLatestTweet(user);
                if (tweet == null || tweet.Id <= -1)
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

        public override string DeleteSocialMediaUser(ulong guildId, string user, SocialMediaEnum socialMedia)
        {
            string result = base.DeleteSocialMediaUser(guildId, user, socialMedia);

            if (result.Contains("Successfully"))
                GroupTwitterUsers();

            return result;
        }

        public async Task<Embed> PostEmbed(ulong guildId, ulong channelId, string message)
        {
            if (ContainsTweetLink(message))
                return await ConvertToEmbed(message.Split(' ')[^1]);

            message = await GetMessageFromChannelHistory(guildId, channelId);

            if (string.IsNullOrEmpty(message))
                return new EmbedBuilder
                    {Description = "Too many messages away from a Tweet."}.Build();

            return await ConvertToEmbed(message);
        }

        public async Task<string> PostVideo(string message)
        {
            return !ContainsTweetLink(message) ? "No X URL found." : await ConvertToVideo(message); ;
        }

        public string AddFx(string message)
        {
            return !ContainsTweetLink(message) ? "No X URL found." : message.Replace(message.Contains("twitter") ? "twitter.com" : "x.com", "fxtwitter.com");
        }

        public static string GlobalAddFx(string message)
        {
            return !ContainsTweetLink(message) ? "No X URL found." : message.Replace(message.Contains("twitter") ? "twitter.com" : "x.com", "fxtwitter.com");
        }

        public async Task<string> GetMessageFromChannelHistory(ulong guildId, ulong channelId, string skip = "")
        {
            var messages = await Client.GetGuild(guildId).GetTextChannel(channelId).GetMessagesAsync(50)
                .FlattenAsync();

            foreach (var m in messages)
            {
                if (!ContainsTweetLink(m.Content)) continue;
                if (!string.IsNullOrEmpty(skip) && m.Content.Contains(skip))
                    continue;

                return m.Content;
            }
            return string.Empty;
        }

        private async Task<string> ConvertToVideo(string message)
        {
            var tweet = await GetTwitter(message);
            if (tweet == null || tweet.Value == null || tweet.Value.ExtendedEntities == null ||
                tweet.Value.ExtendedEntities.Media.Count != 1)
                return string.Empty;
            TwitterExtendedEntity tee = tweet.Value.ExtendedEntities.Media.ElementAt(0);
            if (tee.ExtendedEntityType != TwitterMediaType.Video)
                return string.Empty;

            long highest = 0;
            int j = -1;
            for (int i = 0; i < tee.VideoInfo.Variants.Count; i++)
            {
                TwitterMediaVariant variant = tee.VideoInfo.Variants[i];
                if (variant.BitRate <= highest)
                    continue;

                highest = variant.BitRate;
                j = i;
            }

            if (j == -1)
                return string.Empty;

            return tee.VideoInfo.Variants[j].Url.ToString();
        }
        private async Task<Embed> ConvertToEmbed(string message)
        {
            var tweet = await GetTwitter(message);
            if (tweet.Value == null)
                return null;
            var t = tweet.Value;
            var embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithAuthor($"{t.User.Name} (@{t.Author.ScreenName})", t.Author.ProfileImageUrl)
                .WithDescription(t.FullText)
                .AddField("Likes", t.FavoriteCount, true)
                .AddField("Retweets", t.RetweetCount, true)
                .AddField("Twitter", t.ToTwitterUrl())
                .WithFooter($"Twitter • {t.CreatedDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)}",
                    "Resources\\Images\\SocialMedia\\twitter.png");

            return embed.Build();
        }

        private async Task<TwitterAsyncResult<TwitterStatus>> GetTwitter(string message)
        {
            string[] urlSplit = message.Split('/');
            string id = urlSplit[^1];
            if(id.Contains("?"))
                id = id.Substring(0, id.IndexOf("?", StringComparison.Ordinal));

            GetTweetOptions options = new GetTweetOptions
            {
                Id = long.Parse(id),
                IncludeEntities = true,
                IncludeMyRetweet = true,
                TweetMode = "extended"
            };
            var tweet = await _service.GetTweetAsync(options);
            if (tweet == null)
                return null;
            if (tweet.Response != null && tweet.Response.RateLimitStatus.RemainingHits <= 0)
                return null;
            return tweet;
        }

        public static bool ContainsTweetLink(string message)
        {
            return (message.Contains("https://twitter.com/") || message.Contains("https://x.com/")) &&
                   message.Contains("status", StringComparison.InvariantCultureIgnoreCase);
        }

        private bool MessageDisplayed(string id, ulong guildId)
        {
            return Users.FindAll(x => x.SocialMedia == SocialMediaEnum.Twitter).Any(ud => ud.Id == id && guildId == ud.GuildId);
        }

        private async Task<Tuple<string, bool>> IsLegitTwitterHandle(string name)
        {
            if (name.Contains("https://twitter.com/"))
            {
                string[] split = name.Split("/");
                name = split[^1];
            }
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
                Count = 10
            };
            try
            {
                var tu = await _service.SearchForUserAsync(options);
                var twitterUsers = tu.Value as TwitterUser[] ?? tu.Value.ToArray();
                TwitterUser foundTwitterUser = null;
                if (twitterUsers.Length > 1)
                {
                    foreach (TwitterUser twitterUser in twitterUsers)
                    {
                        if (!string.Equals(name, twitterUser.ScreenName, StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        foundTwitterUser = twitterUser;
                        break;
                    }
                }
                else if (twitterUsers.Length == 1)
                    foundTwitterUser = twitterUsers.ElementAt(0);

                return await Task.FromResult(foundTwitterUser);
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<string> GetLegitFilters(string filters)
        {
            if (string.IsNullOrWhiteSpace(filters))
            {
                List<string> returnDefaults = new List<string>();
                returnDefaults.AddRange(_availableOptions);
                int i = returnDefaults.FindIndex(x => TwitterFiltersEnum.ConvertStringToEnum(x) == TwitterFiltersEnum.TwitterFilters.AllMedia);
                returnDefaults.RemoveRange(i + 1, 3);
                return returnDefaults;
            }

            IEnumerable<string> filtersList = filters.Split(" ").ToList();
            List<string> legitFilters = new List<string>();
            foreach (string filter in filtersList)
            {
                if (_availableOptions.Any(x => x == filter))
                    legitFilters.Add(filter);
            }

            return legitFilters;
        }

        private void GroupTwitterUsers()
        {
            List<Tuple<ulong, int>> currentUsersPerGuild = new List<Tuple<ulong, int>>();
            foreach (var group in _groupedUsers)
            {
                var currentUsers = group.Key;
                currentUsersPerGuild.Add(new Tuple<ulong, int>(currentUsers[0].GuildId, group.Value));
            }

            _groupedUsers.Clear();

            foreach (var group in Users.FindAll(x => x.SocialMedia == SocialMediaEnum.Twitter)
                         .GroupBy(x => x.GuildId))
            {
                var currentUsers = group.ToList();
                int currentIndex = 0;
                if (currentUsersPerGuild.Exists(x => x.Item1 == currentUsers[0].GuildId))
                    currentIndex = currentUsersPerGuild.Find(x => x.Item1 == currentUsers[0].GuildId)!.Item2;
                _groupedUsers.Add(currentUsers, currentIndex);
            }
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
