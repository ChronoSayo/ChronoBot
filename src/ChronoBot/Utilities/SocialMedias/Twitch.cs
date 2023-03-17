using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitch : SocialMedia
    {
        private readonly ChronoTwitch.ChronoTwitch _api;

        public Twitch(ChronoTwitch.ChronoTwitch api, DiscordSocketClient client, IConfiguration config,
            IEnumerable<SocialMediaUserData> users, IEnumerable<string> availableOptions,
            SocialMediaFileSystem fileSystem, int seconds = 120) :
            base(client, config, users, availableOptions, fileSystem)
        {
            _api = api;
            _api.Authenticate(Config[Statics.TwitchClientId], Config[Statics.TwitchSecret],
                Config[Statics.TwitchAccessToken], Config[Statics.TwitchRefreshToken]);

            Hyperlink = "https://www.twitch.com/";

            OnUpdateTimerAsync(seconds);

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.Twitch.ToString().ToLowerInvariant();
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username,
            ulong sendToChannelId = 0, string options = "")
        {
            if (Duplicate(guildId, username, SocialMediaEnum.Twitch))
                return await Task.FromResult($"Already added {username}");

            string loginName = await _api.LoginName(username);
            if (string.IsNullOrEmpty(loginName))
                return await Task.FromResult("Can't find " + username);

            if (sendToChannelId == 0)
                sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

            if (!CreateSocialMediaUser(loginName, guildId, sendToChannelId, "offline", SocialMediaEnum.Twitch))
                return await Task.FromResult($"Failed to add {loginName}.");

            var displayName = await _api.DisplayName(username);
            return await Task.FromResult($"Successfully added {displayName} \n{Hyperlink}{loginName}");
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, ulong channelId, string username)
        {
            int i = FindIndexByName(guildId, username, SocialMediaEnum.Twitch);
            if (i <= -1)
                return await Task.FromResult("Can't find streamer.");

            SocialMediaUserData ud = Users[i];
            bool isLive = await _api.IsLive(ud.Name);
            string message;
            if (isLive)
            {
                message = await UpdateSocialMedia(new List<SocialMediaUserData> { ud });
            }
            else
                message = Hyperlink + ud.Name;

            return await Task.FromResult(message);

        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, SocialMediaEnum socialMedia, string channelMention = "")
        {
            if (Users.Count == 0)
                return await Task.FromResult("No streamers registered.");

            return await base.ListSavedSocialMediaUsers(guildId, SocialMediaEnum.Twitch, channelMention);
        }

        public override async Task<string> GetUpdatedSocialMediaUsers(ulong guildId)
        {
            if (Users.Count == 0)
                return await Task.FromResult("No streamers registered.");

            List<SocialMediaUserData> live = new List<SocialMediaUserData>();
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.Twitch || user.GuildId != guildId)
                    continue;

                bool isLive = await _api.IsLive(user.Name);
                string oldId = user.Id;
                if (isLive)
                {
                    if(user.Id.Contains("online"))
                        continue;
                    user.Id = await GetStreamInfo(user.Name);
                }
                else
                    user.Id = "offline";

                if (user.Id == oldId)
                    continue;

                Users[i] = user;
                live.Add(Users[i]);
                FileSystem.UpdateFile(user);
            }

            if (live.Count > 0)
                return await UpdateSocialMedia(live);

            return await Task.FromResult("No streamers are broadcasting.");
        }

        private async Task<string> GetStreamInfo(string name)
        {
            string displayName = await _api.DisplayName(name);
            string gameName = await _api.GameName(name);
            return $"online|{displayName} is playing {gameName}";
        }
    }
}
