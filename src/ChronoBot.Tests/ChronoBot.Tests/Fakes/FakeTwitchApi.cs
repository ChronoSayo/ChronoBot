using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Helix = ChronoTwitch.Helix.Helix;

namespace ChronoBot.Tests.Fakes
{
    public class FakeTwitchApi : TwitchAPI
    {
        public FakeHelix Helix { get; }
    }

    public class FakeHelix : Helix
    {
        public FakeStreams Streams { get; }
    }

    public class FakeUsers : Users
    {
        public Task<FakeGetUsersResponse> GetUsersAsync(
            List<string> ids = null,
            List<string> logins = null,
            string accessToken = null)
        {
            return Task.FromResult(new FakeGetUsersResponse(logins));
        }

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
