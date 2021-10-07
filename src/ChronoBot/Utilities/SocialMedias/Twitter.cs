using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TweetSharp;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitter : SocialMedia
    {
        private TwitterService _service;

        public Twitter(DiscordSocketClient client, IConfiguration config) : base(client, config)
        {
            Authenticate();

            OnUpdateTimerAsync(10);

            Hyperlink = "https://twitter.com/@name/status/@id";

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.Twitter.ToString().ToLowerInvariant();
        }

        //private void PostRestOfImages(SocketMessage socketMessage)
        //{
        //    string userMessage = socketMessage.ToString();
        //    List<string> tweetsToPost = new List<string>();

        //    while (userMessage.Contains("https://twitter.com/") &&
        //           userMessage.Contains("/status/"))
        //    {
        //        string[] split = userMessage.Split('/');
        //        string url = GetTweetFromPost(split, out string id);

        //        GetTweetOptions options = new GetTweetOptions()
        //        {
        //            Id = long.Parse(id),
        //            IncludeEntities = true
        //        };
        //        TwitterStatus tweet = _service.GetTweet(options);

        //        if (tweet != null && tweet.ExtendedEntities != null && tweet.ExtendedEntities.Media.Count() > 1)
        //        {
        //            for (int i = 1; i < tweet.ExtendedEntities.Media.Count; i++)
        //            {
        //                TwitterExtendedEntity tee = tweet.ExtendedEntities.Media[i];
        //                if (tee.ExtendedEntityType == TwitterMediaType.Photo)
        //                    tweetsToPost.Add(tee.MediaUrlHttps + "\n");
        //            }

        //        }

        //        userMessage = userMessage.Replace(url, "X");
        //    }

        //    if (tweetsToPost.Count == 0)
        //        return;

        //    string message = string.Empty;
        //    foreach (string s in tweetsToPost)
        //    {
        //        message += s;
        //    }
        //    Info.SendMessageToChannel(socketMessage, message);
        //}

        private string GetTweetFromPost(string[] split, out string id)
        {
            string nameKey = "&";
            string idKey = "%";
            string tweetPost = $"https://twitter.com/{nameKey}/status/{idKey}";
            int statusIndex = split.ToList().FindIndex(x => x == "status");
            string name = split[statusIndex - 1];
            string idInString = split[statusIndex + 1];
            id = string.Empty;
            foreach (char c in idInString)
            {
                if (int.TryParse(c.ToString(), out int i))
                    id += i.ToString();
                else
                    break;
            }

            tweetPost = tweetPost.Replace(nameKey, name);
            tweetPost = tweetPost.Replace(idKey, id);
            return tweetPost;
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
                Count = 100
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

        public override async Task<string> AddSocialMediaUser(SocketCommandContext context, string username, ulong channelId = 0)
        {
            Tuple<string, bool> legit;
            if (!Duplicate(context.Guild.Id, username, SocialMediaEnum.Twitter))
            {
                legit = await IsLegitTwitterHandle(username);
                var legitUsername = legit.Item1;
                bool isLegit = legit.Item2;
                if (isLegit)
                {
                    if (channelId == 0)
                        channelId = Statics.Debug ? Statics.DebugChannelId : context.Channel.Id;

                    if (!CreateSocialMediaUser(legitUsername, context.Guild.Id, channelId, "0", SocialMediaEnum.Twitter))
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
            _service = new TwitterService(consumerKey, consumerSecret, token, secret)
            {
                TweetMode = "extended",
            };
        }

        //protected override void OtherCommands(SocketMessage socketMessage)
        //{
        //    if (!socketMessage.Embeds.Any(x => x.Url.ToLowerInvariant().Contains("https://twitter.com/")))
        //    {
        //        if (!socketMessage.Content.Contains("https://twitter.com/") && !socketMessage.Content.Contains("/status/"))
        //            return;
        //    }

        //    Embed embed =
        //        socketMessage.Embeds.FirstOrDefault(x => x.Url.ToLowerInvariant().Contains("https://twitter.com/"));
        //    string id;
        //    if (embed == null)
        //        GetTweetFromPost(socketMessage.Content.Split('/'), out id);
        //    else
        //        GetTweetFromPost(embed.Url.Split('/'), out id);

        //    if(id == string.Empty)
        //        return;

        //    GetTweetOptions options = new GetTweetOptions
        //    {
        //        Id = long.Parse(id),
        //        IncludeEntities = true
        //    };
        //    var tweet = _service.GetTweet(options);
        //    if (tweet == null || tweet.ExtendedEntities == null || tweet.ExtendedEntities.Media.Count != 1)
        //        return;

        //    TwitterExtendedEntity tee = tweet.ExtendedEntities.Media.ElementAt(0);
        //    if (tee.ExtendedEntityType != TwitterMediaType.Video)
        //        return;

        //    int highest = 0;
        //    int j = -1;
        //    for(int i = 0; i < tee.VideoInfo.Variants.Count; i++)
        //    {
        //        TwitterMediaVariant variant = tee.VideoInfo.Variants[i];
        //        string res = string.Empty;
        //        string[] segments = tee.VideoInfo.Variants[i].Url.Segments;
        //        foreach (var s in segments)
        //        {
        //            if(!int.TryParse(s[0].ToString(), out _))
        //                continue;
        //            if(s.Length <= 5)
        //                continue;
        //            if(s[2] != 'x' && s[3] != 'x' && s[4] != 'x')
        //                continue;

        //            res = s.TrimEnd('/');
        //            break;
        //        }

        //        string[] split = res.Split('x');
        //        if(!int.TryParse(split[0], out int x))
        //            continue;
        //        if (!int.TryParse(split[0], out int y))
        //            continue;

        //        int multiplyRes = x * y;
        //        if (multiplyRes <= highest) 
        //            continue;

        //        highest = multiplyRes;
        //        j = i;
        //    }
        //    Info.SendMessageToChannel(socketMessage, tee.VideoInfo.Variants[j].Url.ToString());
        //}

        public override void MessageReceivedSelf(SocketMessage socketMessage)
        {
            base.MessageReceivedSelf(socketMessage);

            //PostRestOfImages(socketMessage);
        }
    }
}
