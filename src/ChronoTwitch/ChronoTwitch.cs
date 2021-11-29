using System.Threading.Tasks;
using ChronoTwitch.Helix;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace ChronoTwitch
{
    public class ChronoTwitch
    {
        private readonly TwitchAPI _api;

        public ChronoTwitch()
        {
            _api = new TwitchAPI();
        }

        public void Authenticate(string clientId, string secret)
        {
            _api.Settings.ClientId = clientId;
            _api.Settings.Secret = secret;
        }

        public async Task<string> DisplayName(string name)
        {
            var displayName = await GetDisplayName(name);
            return displayName;
        }

        public async Task<bool> IsLive(string name)
        {
            var info = await GetStreamInfo(name);
            return info is { Type: "live" };
        }

        public async Task<string> GameName(string name)
        {
            var info = await GetStreamInfo(name);
            if (info != null && info.GameName != string.Empty)
                return info.GameName;

            return string.Empty;
        }

        private async Task<string> GetDisplayName(string name)
        {
            var users = await _api.GetUserAsync(name);
            if (users == null || users.Users.Length == 0)
                return null;

            return await Task.FromResult(users.Users[0].DisplayName);
        }

        private async Task<Stream> GetStreamInfo(string name)
        {
            var streams = await _api.GetStreamAsync(name);
            if (streams == null || streams.Streams.Length == 0)
                return null;

            return await Task.FromResult(streams.Streams[0]);
        }
    }
}
