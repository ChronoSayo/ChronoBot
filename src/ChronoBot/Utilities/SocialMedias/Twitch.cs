using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;

namespace ChronoBot.Utilities.SocialMedias
{
    public sealed class Twitch : SocialMedia
    {
        private readonly TwitchAPI _api;
        private readonly string _mineTwitchCommand;
        /// <summary>
        /// bool is to check if the streamer is online.
        /// </summary>
        private readonly Dictionary<SocialMediaUserData, bool> _streamers;

        public Twitch(TwitchAPI api, DiscordSocketClient client, IConfiguration config,
            IEnumerable<SocialMediaUserData> users, SocialMediaFileSystem fileSystem, int seconds = 120) : base(client, config, users, fileSystem)
        {
            _api = api;
            _api.Settings.ClientId = Config[Statics.TwitchClientId];
            _api.Settings.Secret = Config[Statics.TwitchSecret];

            OnUpdateTimerAsync(seconds);

            _streamers = new Dictionary<SocialMediaUserData, bool>();

            LoadOrCreateFromFile();

            foreach (SocialMediaUserData user in Users)
                _streamers.Add(user, false);
        }

        private async Task<string>GetStreamId(string name)
        {
            string foundStreamer;
            try
            {
                var user = await _api.V5.Users.GetUserByNameAsync(name);
                foundStreamer = GetStreamer(name).Id;
            }
            catch
            {
                //LogToFile(LogSeverity.Warning, $"Can't find {name}.", e);
                foundStreamer = string.Empty;
            }

            return foundStreamer;
        }

        private TwitchLib.Api.V5.Models.Users.User GetStreamer(string name)
        {
            return _api.V5.Users.GetUserByNameAsync(name).GetAwaiter().GetResult().Matches.ElementAt(0);
        }

        protected override async Task AutoUpdate()
        {
            if (Users.Count == 0)
                return;

            List<SocialMediaUserData> onlineStreamers = new List<SocialMediaUserData>();
            for (int i = 0; i < _streamers.Keys.Count; i++)
            {
                SocialMediaUserData ud;

                //Try-catch in case someone was removing a streamer while posting update.
                try
                {
                    ud = _streamers.Keys.ElementAt(i);
                    if (ud.SocialMedia != SocialMediaEnum.Twitch)
                        continue;
                }
                catch
                {
                    return;
                }

                try
                {
                    if (_streamers[ud] != await _api.V5.Streams.BroadcasterOnlineAsync(ud.Id))
                    {
                        _streamers[ud] = await _api.V5.Streams.BroadcasterOnlineAsync(ud.Id);
                        if (_streamers[ud] && !onlineStreamers.Contains(ud))
                            onlineStreamers.Add(ud);
                    }
                }
                catch (Exception e)
                {
                    //LogToFile(LogSeverity.Error, $"Unable to get status for {ud.name}.", e);
                }
            }

            await base.AutoUpdate();
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0)
        {
            if (!Duplicate(guildId, username, SocialMediaEnum.Twitch))
            {
                string streamerId = await GetStreamId(username);
                if (!string.IsNullOrEmpty(streamerId))
                {
                    //Use the name displayed from Twitch instead of what the user input.
                    string displayName = GetStreamer(username).DisplayName;
                    if (sendToChannelId == 0)
                        sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

                    if(!CreateSocialMediaUser(displayName, guildId, sendToChannelId, streamerId, SocialMediaEnum.Twitch))
                        return await Task.FromResult($"Failed to add {displayName}.");

                    return await Task.FromResult($"Successfully added {displayName}");
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
