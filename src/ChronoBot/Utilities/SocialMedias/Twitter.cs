using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TweetSharp;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitter : SocialMedia
    {
        private TwitterService _service;

        public Twitter(TwitterService service, DiscordSocketClient client, IConfiguration config, IEnumerable<SocialMediaUserData> users, SocialMediaFileSystem fileSystem) : 
            base(client, config, users, fileSystem)
        {
            _service = service;
            Authenticate();

            OnUpdateTimerAsync(60);

            Hyperlink = "https://twitter.com/@name/status/@id";

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.Twitter.ToString().ToLowerInvariant();
        }

        protected override async Task PostUpdate()
        {
            if (Users.Count == 0)
                return;

            List<SocialMediaUserData> newTweets = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if(user.SocialMedia != SocialMediaEnum.Twitter)
                    continue;

                var channel = Client.GetGuild(Users[i].GuildId).GetTextChannel(Users[i].ChannelId);
                TwitterStatus tweet = await GetLatestTwitter(user, channel);
                if (tweet == null || tweet.Id == -1)
                    continue;

                if (!MessageDisplayed(tweet.IdStr, user.GuildId))
                {
                    user.Id = tweet.IdStr;
                    Users[i] = user;
                    newTweets.Add(user);
                    FileSystem.UpdateFile(user);
                }
            }

            await UpdateSocialMedia(newTweets);
        }

        private async Task<TwitterStatus> GetLatestTwitter(SocialMediaUserData ud, SocketTextChannel channel)
        {
            ListTweetsOnUserTimelineOptions options = new ListTweetsOnUserTimelineOptions()
            {
                ScreenName = ud.Name,
                IncludeRts = false,
                Count = 100,
                TweetMode = "extended"
            };
            TwitterAsyncResult<IEnumerable<TwitterStatus>> tweets;
            try
            {
                tweets = await _service.ListTweetsOnUserTimelineAsync(options);
                if (tweets.Response.StatusCode != HttpStatusCode.OK)
                    return null;
            }
            catch
            {
                return null;
            }

            var twitterStatuses = tweets.Value as TwitterStatus[] ?? tweets.Value.ToArray();
            if (!twitterStatuses.Any())
            {
                return null;
            }

            TwitterStatus tweet = twitterStatuses.ElementAt(0);
            if (channel.IsNsfw)
            {
                if (tweet.ExtendedEntities == null || !tweet.ExtendedEntities.Any() || !tweet.ExtendedEntities.Media.Any() ||
                    tweet.ExtendedEntities.Media.ElementAt(0).ExtendedEntityType != TwitterMediaType.Photo)
                    tweet.Id = -1;
            }

            if (tweet.Id == 0 || tweet.IdStr == "" || tweet.IdStr == null)
            {
                return null;
            }
            
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

                    if (!CreateSocialMediaUser(legitUsername, channelId, sendToChannelId, "0", SocialMediaEnum.Twitter))
                        return await Task.FromResult($"Failed to add {legitUsername}.");

                    return await Task.FromResult("Successfully added " + legitUsername + "\n" + "https://twitter.com/" + legitUsername);
                }
                    
                return await Task.FromResult("Can't find " + username);
            }

            legit = await IsLegitTwitterHandle(username);
            return await Task.FromResult("Already added " + legit.Item1);
        }

        public override async Task<string> GetSocialMediaUser(SocketCommandContext context, string user)
        {
            ulong guildId = context.Guild.Id;
            int i = FindIndexByName(guildId, user);
            if (i > -1)
            {
                TwitterStatus tweet = await GetLatestTwitter(Users[i], context.Channel as SocketTextChannel);
                if (tweet != null)
                {
                    SocialMediaUserData temp = Users[i];
                    temp.Id = tweet.IdStr;
                    Users[i] = temp;
                    return await Task.FromResult(GetTwitterUrl(Users[i]));
                }

                return await Task.FromResult("Could not retrieve Tweet");
            }

            return await Task.FromResult("Could not find user.");
        }

        public override async Task<string> ListSavedSocialMediaUsers(SocketCommandContext context)
        {
            string line = string.Empty;
            foreach (var user in Users)
            {
                bool addToList;
                if (Statics.Debug)
                    addToList = true;
                else
                {
                    var guildId = context.Guild.Id;
                    addToList = guildId == user.GuildId;
                }

                if (!addToList) 
                    continue;

                SocketTextChannel channel = context.Channel as SocketTextChannel;
                string name = user.Name;
                line += "■ " + name + " " + (channel == null ? "***Missing channel info.***" : channel.Mention) + "\n";
            }

            return await Task.FromResult(line);
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
            var tu = await _service.SearchForUserAsync(options);
            var twitterUsers = tu.Value as TwitterUser[] ?? tu.Value.ToArray();
            return await Task.FromResult(twitterUsers.Any() ? twitterUsers.ElementAt(0) : null);
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
