using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ChronoBot.Tests.Fakes
{
    public class FakeChronoTwitch : ChronoTwitch.ChronoTwitch
    {
        public override Task<string> DisplayName(string name)
        {
            switch (name)
            {
                case "streamer":
                    return Task.FromResult("Streamer");
                case "postupdate1":
                    return Task.FromResult("PostUpdate1");
                case "postupdate2":
                    return Task.FromResult("PostUpdate2");
            }

            return Task.FromResult(string.Empty);
        }

        public override Task<bool> IsLive(string name)
        {
            switch (name)
            {
                case "PostUpdate1":
                    return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
