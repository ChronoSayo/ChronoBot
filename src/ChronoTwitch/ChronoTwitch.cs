using System;
using System.Net;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChronoTwitch
{
    public class ChronoTwitch : TwitchAPI
    {
        private AccessToken _accessToken;
        private string _refreshToken;

        public virtual string ClientId => Settings.ClientId;
        public virtual string Secret => Settings.Secret;
        public virtual string AccessToken => Settings.AccessToken;

        public ChronoTwitch()
        {
        }

        public virtual async void Authenticate(string clientId, string secret, string accessToken, string refreshToken)
        {
            _refreshToken = refreshToken;
            _accessToken = new AccessToken(clientId, secret);

            Settings.ClientId = clientId;
            Settings.Secret = secret;
            Settings.AccessToken = await _accessToken.GetAccessTokenAsync() ?? accessToken;   
        }

        public virtual async Task<string> LoginName(string name)
        {
            string loginName = await GetLoginName(name);
            return loginName;
        }

        public virtual async Task<string> DisplayName(string name)
        {
            string displayName = await GetDisplayName(name);
            return displayName;
        }

        public virtual async Task<bool> IsLive(string name)
        {
            Stream info = await GetStreamInfo(name);
            return info is { Type: "live" };
        }

        public virtual async Task<string> GameName(string name)
        {
            Stream info =await GetStreamInfo(name);
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
            catch (BadScopeException bex)
            {
                try
                {
                    await SetNewAccessToken();
                    users = await this.GetUserAsync(name);
                }
                catch
                {
                    throw bex;
                }
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
            catch (BadScopeException bex)
            {
                try
                {
                    await SetNewAccessToken();
                    users = await this.GetUserAsync(name);
                }
                catch
                {
                    throw bex;
                }
                
            }
            if (users == null || users.Users.Length == 0)
                return null;

            return await Task.FromResult(users.Users[0].Login);
        }

        private async Task<Stream> GetStreamInfo(string name)
        {
            GetStreamsResponse streams = null;
            try
            {
                streams = await this.GetStreamAsync(name);
            }
            catch (BadScopeException bex)
            {
                try
                {
                    await SetNewAccessToken();
                    streams = await this.GetStreamAsync(name);
                }
                catch
                {
                    throw bex;
                }
            }
            if (streams == null || streams.Streams.Length == 0)
                return null;

            return await Task.FromResult(streams.Streams[0]);
        }

        private async Task SetNewAccessToken()
        {
            var tokens = await _accessToken.GetRefreshTokenAsync(_refreshToken);
            Settings.AccessToken = tokens.Item1;
            _refreshToken = tokens.Item2;
        }
    }
}
