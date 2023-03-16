using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChronoTwitch
{
    public class ChronoTwitch : TwitchAPI
    {
        private Authorization _authorization;

        public virtual string ClientId => Settings.ClientId;
        public virtual string Secret => Settings.Secret;
        public virtual string AccessToken => Settings.AccessToken;

        public ChronoTwitch()
        {
        }

        public virtual void Authenticate(string clientId, string secret, string accessToken)
        {
            _authorization = new Authorization(this, clientId, secret);

            Settings.ClientId = clientId;
            Settings.Secret = secret;
            Settings.AccessToken = _authorization.GetAccessToken().Result;
        }

        public virtual async Task<string> LoginName(string name)
        {
            string loginName;
            try
            {
                loginName = await GetLoginName(name);
            }
            catch
            {
                Settings.AccessToken = await _authorization.RefreshToken();
                loginName = await GetLoginName(name);
            }
            return loginName;
        }

        public virtual async Task<string> DisplayName(string name)
        {
            string displayName;
            try
            {
                displayName = await GetDisplayName(name);
            }
            catch
            {
                Settings.AccessToken = await _authorization.RefreshToken();
                displayName = await GetDisplayName(name);
            }
            return displayName;
        }

        public virtual async Task<bool> IsLive(string name)
        {
            Stream info; 
            try
            {
                info = await GetStreamInfo(name);
            }
            catch
            {
                Settings.AccessToken = await _authorization.RefreshToken();
                info = await GetStreamInfo(name);
            }
            return info is { Type: "live" };
        }

        public virtual async Task<string> GameName(string name)
        {
            Stream info;
            try
            {
                info = await GetStreamInfo(name);
            }
            catch
            {
                Settings.AccessToken = await _authorization.RefreshToken();
                info = await GetStreamInfo(name);
            }
            if (info != null && info.GameName != string.Empty)
                return info.GameName;

            return string.Empty;
        }

        private async Task<string> GetDisplayName(string name)
        {
            GetUsersResponse users;
            try
            {
                users = await this.GetUserAsync(name);
            }
            catch
            {
                Settings.AccessToken = await _authorization.RefreshToken();
                users = await this.GetUserAsync(name);
            }
            if (users == null || users.Users.Length == 0)
                return null;

            return await Task.FromResult(users.Users[0].DisplayName);
        }
        private async Task<string> GetLoginName(string name)
        {
            GetUsersResponse users;
            try
            {
                users = await this.GetUserAsync(name);
            }
            catch
            {
                Settings.AccessToken = await _authorization.RefreshToken();
                users = await this.GetUserAsync(name);
            }
            if (users == null || users.Users.Length == 0)
                return null;

            return await Task.FromResult(users.Users[0].Login);
        }

        private async Task<Stream> GetStreamInfo(string name)
        {
            GetStreamsResponse streams;
            try
            {
                streams = await this.GetStreamAsync(name);
            }
            catch
            {
                Settings.AccessToken = await _authorization.RefreshToken();
                streams = await this.GetStreamAsync(name);
            }
            if (streams == null || streams.Streams.Length == 0)
                return null;

            return await Task.FromResult(streams.Streams[0]);
        }
    }
}
