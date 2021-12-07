using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.Undocumented;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Interfaces;
using TwitchLib.Api.ThirdParty;
using TwitchLib.Api.V5;
using Streams = TwitchLib.Api.Helix.Streams;
using Users = TwitchLib.Api.Helix.Users;

namespace ChronoTwitch.Tests
{
    public class FakeTwitchApi : ITwitchAPI
    {
        private readonly FakeHelix _helix;
        public FakeTwitchApi()
        {
            _helix = new FakeHelix();
        }

        public IApiSettings Settings { get; }
        public V5 V5 { get; }
        TwitchLib.Api.Helix.Helix ITwitchAPI.Helix => _helix;

        public ThirdParty ThirdParty { get; }
        public Undocumented Undocumented { get; }
    }

    public class FakeHelix : Helix
    {
        private readonly FakeUsers _users;
        public FakeHelix()
        {
            _users = new FakeUsers(null, null, null);
        }

        public FakeStreams Streams { get; }
        public FakeUsers Users => _users;
    }

    public class FakeUsers : Users
    {
        public Task<FakeGetUsersResponse> GetUsersAsync(
            List<string> ids = null,
            List<string> logins = null,
            string accessToken = null) => Task.FromResult(new FakeGetUsersResponse(logins));

        public FakeUsers(IApiSettings settings, IRateLimiter rateLimiter, IHttpCallHandler http) : base(settings, rateLimiter, http)
        {
        }
    }

    public class FakeGetUsersResponse : GetUsersResponse
    {
        private readonly string _name;
        public FakeGetUsersResponse(List<string> names)
        {
            _name = names[0];
        }
        public FakeUser[] Streams
        {
            get
            {
                return new[] { new FakeUser(_name) };
            }
        }
    }

    public class FakeUser : User
    {
        private readonly string _name;
        public FakeUser(string name)
        {
            _name = name;
        }

        public string DisplayName => _name;
    }

    public class FakeStreams : Streams
    {
        public Task<FakeGetStreamsResponse> GetStreamsAsync(string after = null, 
            List<string> communityIds = null,
            int first = 20,
            List<string> gameIds = null,
            List<string> languages = null,
            string type = "all",
            List<string> userIds = null,
            List<string> userLogins = null)
        {
            return Task.FromResult(new FakeGetStreamsResponse(userLogins));
        }

        public FakeStreams(IApiSettings settings, IRateLimiter rateLimiter, IHttpCallHandler http) : base(settings, rateLimiter, http)
        {
        }
    }

    public class FakeGetStreamsResponse : GetStreamsResponse
    {
        private readonly string _name;
        public FakeGetStreamsResponse(List<string> names)
        {
            _name = names[0];
        }
        public FakeStream[] Streams
        {
            get
            {
                return new[] { new FakeStream(_name) };
            }
        }
    }

    public class FakeStream : Stream
    {
        private readonly string _name;
        public FakeStream(string name)
        {
            _name = name;
        }

        public string GameName => "Chrono Trigger";
        public string Type => _name == "Streamer" ? "live" : "";
    }
}
