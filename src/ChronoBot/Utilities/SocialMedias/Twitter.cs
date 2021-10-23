using System;
using System.Collections.Generic;
using System.Linq;
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

        protected override async Task PostUpdate()
        {
            ulong current = 0;
            for(int i = 0; i < Users.Count; i++)
            {
                if(Users[i].GuildId == current)
                    continue;
                
                current = Users[i].GuildId;
                await GetUpdatedSocialMediaUsers(current);
            }
        }

        private async Task<TwitterStatus> GetLatestTwitter(SocialMediaUserData ud, bool isNsfw)
        {
            ListTweetsOnUserTimelineOptions options = new ListTweetsOnUserTimelineOptions()
            {
                ScreenName = ud.Name,
                IncludeRts = false,
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
            if (isNsfw)
            {
                if (tweet.ExtendedEntities == null || !tweet.ExtendedEntities.Any() || !tweet.ExtendedEntities.Media.Any() ||
                    tweet.ExtendedEntities.Media.ElementAt(0).ExtendedEntityType != TwitterMediaType.Photo)
                    tweet.Id = -1;
            }

            if (tweet.Id == 0 || tweet.IdStr == "" || tweet.IdStr == null)
                return null;
            
            return await Task.FromResult(tweet);
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0)
        {
            Tuple<string, bool> legit;
            if (!Duplicate(guildId, username, SocialMediaEnum.Twitter))
            {
                legit = await IsLegitTwitterHandle(username);
                var legitUsername = legit.Item1;
                bool isLegit = legit.Item2;
                if (isLegit)
                {
                    if (sendToChannelId == 0)
                        sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

                    if (!CreateSocialMediaUser(legitUsername, guildId, sendToChannelId, "0", SocialMediaEnum.Twitter))
                        return await Task.FromResult($"Failed to add {legitUsername}.");

                    return await Task.FromResult("Successfully added " + legitUsername + "\n" + "https://twitter.com/" + legitUsername);
                }
                    
                return await Task.FromResult("Can't find " + username);
            }

            legit = await IsLegitTwitterHandle(username);
            return await Task.FromResult("Already added " + legit.Item1);
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, bool isNsfw, string user)
        {
            int i = FindIndexByName(guildId, user);
            if (i > -1)
            {
                TwitterStatus tweet = await GetLatestTwitter(Users[i], isNsfw);
                if (tweet != null)
                {
                    SocialMediaUserData temp = Users[i];
                    temp.Id = tweet.IdStr;
                    Users[i] = temp;
                    return await Task.FromResult(GetTwitterUrl(Users[i]));
                }

                return await Task.FromResult("Could not retrieve Tweet.");
            }

            return await Task.FromResult("Could not find user.");
        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, string channelMention = "")
        {
            string line = string.Empty;
            foreach (var user in Users)
            {
                bool addToList;
                if (Statics.Debug)
                    addToList = true;
                else
                    addToList = guildId == user.GuildId;

                if (!addToList) 
                    continue;
                
                string name = user.Name;
                line += "■ " + name + " " + (channelMention ?? "***Missing channel info.***") + "\n";
            }

            return await Task.FromResult(line);
        }

        public override async Task<string> GetUpdatedSocialMediaUsers(ulong guildId)
        {
            if (Users.Count == 0)
                return await Task.FromResult("No user registered.");

            List<SocialMediaUserData> newTweets = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.Twitter || user.GuildId != guildId)
                    continue;

                var channel = Client.GetGuild(user.GuildId)?.GetTextChannel(user.ChannelId);
                TwitterStatus tweet = await GetLatestTwitter(user, channel is { IsNsfw: true });
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
