using System.Collections.Generic;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

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

            Hyperlink = "https://www.twitch.com/";

            OnUpdateTimerAsync(seconds);

            LoadOrCreateFromFile();

            TypeOfSocialMedia = SocialMediaEnum.Twitch.ToString().ToLowerInvariant();
        }

        public override async Task<string> AddSocialMediaUser(ulong guildId, ulong channelId, string username, ulong sendToChannelId = 0)
        {
            if (Duplicate(guildId, username, SocialMediaEnum.Twitch))
                return await Task.FromResult($"Already added {username}");

            var displayName = await GetStreamUsernameAsync(username);
            if (string.IsNullOrEmpty(displayName))
                return await Task.FromResult("Can't find " + username);

            if (sendToChannelId == 0)
                sendToChannelId = Statics.Debug ? Statics.DebugChannelId : channelId;

            if (!CreateSocialMediaUser(username, guildId, sendToChannelId, "0", SocialMediaEnum.Twitch))
                return await Task.FromResult($"Failed to add {username}.");

            return await Task.FromResult($"Successfully added {displayName} \n{Hyperlink}{displayName}");
        }

        public override async Task<string> GetSocialMediaUser(ulong guildId, ulong channelId, string username)
        {
            int i = FindIndexByName(guildId, username, SocialMediaEnum.Twitch);
            if (i > -1)
            {
                SocialMediaUserData ud = Users[i];
                var stream = await IsLive(ud.Name);
                string message;
                if (stream is { Type: "live" })
                    message = await UpdateSocialMedia(new List<SocialMediaUserData> { ud }, stream);
                else
                    message = Hyperlink + ud.Name;
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

        public override async Task<string> GetUpdatedSocialMediaUsers(ulong guildId)
        {
            if (Users.Count == 0)
                return await Task.FromResult("No streamers registered.");

            List<SocialMediaUserData> live = new List<SocialMediaUserData>();
            Stream stream = null;
            for (int i = 0; i < Users.Count; i++)
            {
                SocialMediaUserData user = Users[i];
                if (user.SocialMedia != SocialMediaEnum.Twitch || user.GuildId != guildId)
                    continue;

                stream = await IsLive(user.Name);
                user.Id = stream is { Type: "live" } ? "1" : "0";
                Users[i] = user;
                live.Add(Users[i]);
                FileSystem.UpdateFile(user);
            }

            if (live.Count > 0)
                return await UpdateSocialMedia(live, stream);

            return await Task.FromResult("No updates since last time.");
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

        private async Task<Stream> IsLive(string name)
        {
            var streams = await _api.Helix.Streams.GetStreamsAsync(first: 1, userLogins: new List<string> { name });
            if (streams == null || streams.Streams.Length == 0 || streams.Streams[0].Type != "live")
                return null;
            
            return await Task.FromResult(streams.Streams[0]);
        }
    }
}
