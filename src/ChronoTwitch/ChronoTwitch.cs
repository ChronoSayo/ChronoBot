using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace ChronoTwitch
{
    public class ChronoTwitch : TwitchAPI
    {
        public virtual string ClientId => Settings.ClientId;
        public virtual string Secret => Settings.Secret;
        public virtual string AccessToken => Settings.AccessToken;

        public ChronoTwitch()
        {
        }

        public virtual void Authenticate(string clientId, string secret, string accessToken)
        {
            Settings.ClientId = clientId;
            Settings.Secret = secret;
            Settings.AccessToken = accessToken;
        }

        public virtual async Task<string> LoginName(string name)
        {
            var loginName = await GetLoginName(name);
            return loginName;
        }

        public virtual async Task<string> DisplayName(string name)
        {
            var displayName = await GetDisplayName(name);
            return displayName;
        }

        public virtual async Task<bool> IsLive(string name)
        {
            var info = await GetStreamInfo(name);
            return info is { Type: "live" };
        }

        public virtual async Task<string> GameName(string name)
        {
            var info = await GetStreamInfo(name);
            if (info != null && info.GameName != string.Empty)
                return info.GameName;

            return string.Empty;
        }

        private async Task<string> GetDisplayName(string name)
        {
            var users = await this.GetUserAsync(name);
            if (users == null || users.Users.Length == 0)
                return null;

            return await Task.FromResult(users.Users[0].DisplayName);
        }
        private async Task<string> GetLoginName(string name)
        {
            var users = await this.GetUserAsync(name);
            if (users == null || users.Users.Length == 0)
                return null;

            return await Task.FromResult(users.Users[0].Login);
        }

        private async Task<Stream> GetStreamInfo(string name)
        {
            var streams = await this.GetStreamAsync(name);
            if (streams == null || streams.Streams.Length == 0)
                return null;

            return await Task.FromResult(streams.Streams[0]);
        }
    }
}
