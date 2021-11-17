using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

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
        public Task<GetUsersResponse> GetUsersAsync(
            List<string> ids = null,
            List<string> logins = null,
            string accessToken = null)
        {

        }

        public FakeUsers(IApiSettings settings, IRateLimiter rateLimiter, IHttpCallHandler http) : base(settings, rateLimiter, http)
        {
        }
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
