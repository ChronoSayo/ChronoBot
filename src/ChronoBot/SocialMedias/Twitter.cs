﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord.WebSocket;
using TweetSharp;

namespace ChronoBot.SocialMedias
{
    sealed class Twitter : SocialMedia
    {
        private TwitterService _service;

        public Twitter(DiscordSocketClient client) : base(client)
        {
            _client = client;

            Authenticate();

            UpdateTimer(60);

            SetCommands("twitter");
            _hyperlink = "https://twitter.com/@name/status/@id";

            _howToMessage = _howToMessage.Replace(_USER_KEYWORD, "Twitter user");

            LoadOrCreateFromFile();
        }

        private void PostRestOfImages(SocketMessage socketMessage)
        {
            string userMessage = socketMessage.ToString();
            List<string> tweetsToPost = new List<string>();

            while (userMessage.Contains("https://twitter.com/") &&
                   userMessage.Contains("/status/"))
            {
                string[] split = userMessage.Split('/');
                string url = GetTweetFromPost(split, out string id);

                GetTweetOptions options = new GetTweetOptions()
                {
                    Id = long.Parse(id),
                    IncludeEntities = true
                };
                TwitterStatus tweet = _service.GetTweet(options);

                if (tweet != null && tweet.ExtendedEntities != null && tweet.ExtendedEntities.Media.Count() > 1)
                {
                    for (int i = 1; i < tweet.ExtendedEntities.Media.Count; i++)
                    {
                        TwitterExtendedEntity tee = tweet.ExtendedEntities.Media[i];
                        if (tee.ExtendedEntityType == TwitterMediaType.Photo)
                            tweetsToPost.Add(tee.MediaUrlHttps + "\n");
                    }

                }

                userMessage = userMessage.Replace(url, "X");
            }

            if (tweetsToPost.Count == 0)
                return;

            string message = string.Empty;
            foreach (string s in tweetsToPost)
            {
                message += s;
            }
            Info.SendMessageToChannel(socketMessage, message);
        }

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

        protected override void PostUpdate()
        {
            if (_users.Count == 0)
                return;

            List<UserData> newTweets = new List<UserData>();
            for (int i = 0; i < _users.Count; i++)
            {
                UserData user = _users[i];
                if(user.socialMedia != "Twitter")
                    continue;
                
                TwitterStatus tweet = GetLatestTwitter(user);
                if (tweet == null || tweet.Id == -1)
                    continue;

                if (!MessageDisplayed(tweet.IdStr, user.guildID))
                {
                    user.id = tweet.IdStr;
                    _users[i] = user;
                    newTweets.Add(user);
                    _fileSystem.UpdateFile(user);
                }
            }

            UpdateSocialMedia(newTweets);
        }

        private TwitterStatus GetLatestTwitter(UserData ud)
        {
            ListTweetsOnUserTimelineOptions options = new ListTweetsOnUserTimelineOptions()
            {
                ScreenName = ud.name,
                IncludeRts = false,
                Count = 100
            };
            IEnumerable<TwitterStatus> tweets;
            try
            {
                tweets = _service.ListTweetsOnUserTimeline(options);
            }
            catch
            {
                return null;
            }

            if (tweets == null)
            {
                return null;
            }

            var twitterStatuses = tweets as TwitterStatus[] ?? tweets.ToArray();
            if (!twitterStatuses.Any())
            {
                return null;
            }

            TwitterStatus tweet = twitterStatuses.ElementAt(0);
            if (_client.GetGuild(ud.guildID).GetTextChannel(ud.channelID).IsNsfw)
            {
                if (tweet.ExtendedEntities == null || !tweet.ExtendedEntities.Any() || !tweet.ExtendedEntities.Media.Any() ||
                    tweet.ExtendedEntities.Media.ElementAt(0).ExtendedEntityType != TwitterMediaType.Photo)
                    tweet.Id = -1;
            }

            if (tweet.Id == 0 || tweet.IdStr == "" || tweet.IdStr == null)
            {
                return null;
            }
            
            return tweet;
        }

        protected override void AddSocialMediaUser(SocketMessage socketMessage)
        {
            if (AddCommand(socketMessage.ToString()))
            {
                string[] split = socketMessage.ToString().Split(' '); //0: Command. 1: Twitter handle. 2: Channel.
                if (split.Length == 1)
                {
                    Info.SendMessageToChannel(socketMessage, "Missing Twitter handle.");
                    return;
                }

                string username = split[1];
                ulong guildId = Info.GetGuildIDFromSocketMessage(socketMessage);
                if (!Duplicate(guildId, username, "Twitter"))
                {
                    if (IsLegitTwitterHandle(username, out username))
                    {
                        ulong channelId = Info.DEBUG ? Info.DEBUG_CHANNEL_ID : socketMessage.Channel.Id;
                        if (split.Length > 2 && split[2].Contains(socketMessage.MentionedChannels.ElementAt(0).Id.ToString()))
                            channelId = socketMessage.MentionedChannels.ElementAt(0).Id;

                        CreateSocialMediaUser(username, guildId, channelId, "0", "Twitter");

                        string message = "Successfully added " + username + "\n" +
                            "https://twitter.com/" + username;
                        _client.GetGuild(guildId).GetTextChannel(channelId).SendMessageAsync(message);
                    }
                    else
                        Info.SendMessageToChannel(socketMessage, "Can't find " + username);
                }
                else
                {
                    IsLegitTwitterHandle(username, out username);
                    Info.SendMessageToChannel(socketMessage, "Already added " + username);
                }
            }
        }

        protected override void GetSocialMediaUser(SocketMessage socketMessage)
        {
            if (GetCommand(socketMessage.ToString()))
            {
                if (_users.Count == 0)
                {
                    Info.Shrug(socketMessage);
                    return;
                }
                string[] split = socketMessage.ToString().Split(' '); //0: Command. 1: Twitter handler.
                if (split.Length == 2)
                {
                    int i = FindIndexByName(Info.GetGuildIDFromSocketMessage(socketMessage), split[1]);
                    if (i > -1)
                    {
                        TwitterStatus tweet = GetLatestTwitter(_users[i]);
                        if (tweet != null)
                        {
                            UserData temp = _users[i];
                            temp.id = tweet.IdStr;
                            _users[i] = temp;
                            Info.SendMessageToChannel(socketMessage, GetTwitterUrl(_users[i]));
                        }
                        else
                            Info.SendMessageToChannel(socketMessage, 
                                _users[i].name + " hasn't tweeted yet or for a while.");
                    }
                    else
                        Info.Shrug(socketMessage);
                }
                else
                    Info.SendMessageToChannel(socketMessage, "No Twitter handle added.");
            }
        }

        protected override void ListSavedSocialMediaUsers(SocketMessage socketMessage)
        {
            if (ListCommand(socketMessage.ToString()))
            {
                if (_users.Count == 0)
                {
                    Info.Shrug(socketMessage);
                    return;
                }

                string line = "";
                ulong guildId = Info.DEBUG_GUILD_ID;
                for (int i = 0; i < _users.Count; i++)
                {
                    bool addToList;
                    if (Info.DEBUG)
                        addToList = true;
                    else
                    {
                        guildId = Info.GetGuildIDFromSocketMessage(socketMessage);
                        addToList = guildId == _users[i].guildID;
                    }
                    if (addToList)
                    {
                        SocketTextChannel channel = _client.GetGuild(guildId).GetTextChannel(_users[i].channelID);
                        string name = _users[i].name;
                        //line += "■ " + SearchTwitterUser(name).Name + " (" + name + ") " +
                        line += "■ " + name + " " + 
                            (channel == null ? "***Missing channel info.***" : channel.Mention) + "\n";
                    }
                }
                if(line == "")
                    Info.Shrug(socketMessage);
                else
                    socketMessage.Channel.SendMessageAsync(line);
            }
        }

        private bool MessageDisplayed(string id, ulong guildId)
        {
            foreach (UserData ud in _users)
            {
                if (ud.id == id && guildId == ud.guildID)
                    return true;
            }
            return false;
        }

        private bool IsLegitTwitterHandle(string name, out string screenName)
        {
            TwitterUser tu = SearchTwitterUser(name);
            bool found = tu != null;
            screenName = found ? tu.ScreenName : string.Empty;

            return found;
        }

        private TwitterUser SearchTwitterUser(string name)
        {
            SearchForUserOptions options = new SearchForUserOptions
            {
                Q = name,
                Count = 1
            };
            IEnumerable<TwitterUser> tu = _service.SearchForUser(options);
            var twitterUsers = tu as TwitterUser[] ?? tu.ToArray();
            return twitterUsers.Any() ? twitterUsers.ElementAt(0) : null;
        }

        private void Authenticate()
        {
            string[] lines = File.ReadAllLines("Memory Card/TwitterToken.txt");
            _service = new TwitterService(lines[0], lines[1], lines[2], lines[3]) {TweetMode = "extended"};
        }

        public override void MessageReceivedSelf(SocketMessage socketMessage)
        {
            base.MessageReceivedSelf(socketMessage);

            //PostRestOfImages(socketMessage);
        }
    }
}
