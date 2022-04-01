using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChronoTwitch
{
    public static class Helix
    {
        public static async Task<GetStreamsResponse> GetStreamAsync(this TwitchAPI api, string name) => 
            await api.Helix.Streams.GetStreamsAsync(first: 1, userLogins: new List<string> { name });
        public static async Task<GetUsersResponse> GetUserAsync(this TwitchAPI api, string name) =>
            await api.Helix.Users.GetUsersAsync(logins: new List<string> { name });
    }
}
