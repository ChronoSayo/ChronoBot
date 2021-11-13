using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TwitchLib.Api;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitch : SocialMedia
    {
        private readonly TwitchAPI _api;

        public Twitch(TwitchAPI api, DiscordSocketClient client, IConfiguration config,
            IEnumerable<SocialMediaUserData> users, SocialMediaFileSystem fileSystem, int seconds = 120) : base(client, config, users, fileSystem)
        {
            _api = api;
            _api.Settings.ClientId = Config[Statics.TwitchClientId];
            _api.Settings.Secret = Config[Statics.TwitchSecret];

            OnUpdateTimerAsync(seconds);

            LoadOrCreateFromFile();
        }

        private async Task<string> GetStreamUsernameAsync(string name)
        {
            try
            {
                var users = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { name });
                return users.Users[0].DisplayName;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected override async Task AutoUpdate()
        {
            if (Users.Count == 0)
                return;
            
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData ud = Users[i];

                if (ud.SocialMedia != SocialMediaEnum.Twitch)
                    continue;

                //if (_streamers[ud] != await _api.V5.Streams.BroadcasterOnlineAsync(ud.Id))
                //{
                //    _streamers[ud] = await _api.V5.Streams.BroadcasterOnlineAsync(ud.Id);
                //    if (_streamers[ud] && !onlineStreamers.Contains(ud))
                //        onlineStreamers.Add(ud);
                //}

                var streams = await _api.Helix.Streams.GetStreamsAsync(first: 1, userLogins: new List<string> { ud.Name });
                if (streams == null)
                    continue;

                Users[i].Id = streams.Streams[0].Type == "live" ? "live" : string.Empty;
            }

            await base.AutoUpdate();
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0)
        {
            if (!Duplicate(guildId, username, SocialMediaEnum.Twitch))
            {
                var displayName = await GetStreamUsernameAsync(username);
                if (!string.IsNullOrEmpty(displayName))
                {
                    if (sendToChannelId == 0)
                        sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

                    if(!CreateSocialMediaUser(displayName, guildId, sendToChannelId, string.Empty, SocialMediaEnum.Twitch))
                        return await Task.FromResult($"Failed to add {displayName}.");

                    string message = $"Successfully added {displayName}";

                    return await Task.FromResult(message);
                }
                
                return await Task.FromResult($"Can't find {username}");
            }

            SocialMediaUserData ud = _streamers.Keys.ElementAt(FindIndexByName(guildId, username, SocialMediaEnum.Twitch));
            return await Task.FromResult($"Already added {ud.Name}");
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, ulong channelId, string username)
        {
            int i = FindIndexByName(guildId, username, SocialMediaEnum.Twitch);
            if (i > -1)
            {
                SocialMediaUserData ud = _streamers.Keys.ElementAt(i);
                string message = await GetStreamerUrlAndGame(ud, _api);
                return await Task.FromResult(message);
            }

            return await Task.FromResult("Can't find streamer.");
        }

        public override async Task<string> ListSavedSocialMediaUsers(ulong guildId, SocialMediaEnum socialMedia, string channelMention = "")
        {
            if (Users.Count == 0)
                return await Task.FromResult("No streamers registered.");

            return await base.ListSavedSocialMediaUsers(guildId, SocialMediaEnum.Twitch, channelMention);
        }

        protected override bool CreateSocialMediaUser(string name, ulong guildId, ulong channelId, string id, SocialMediaEnum socialMedia)
        {
            base.CreateSocialMediaUser(name, guildId, channelId, id, SocialMediaEnum.Twitch);
            _streamers.Add(Users[^1], false);
            return true;
        }
    }
}
