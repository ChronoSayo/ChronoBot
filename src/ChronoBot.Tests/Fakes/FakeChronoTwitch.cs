using System.Threading.Tasks;

namespace ChronoBot.Tests.Fakes
{
    public class FakeChronoTwitch : ChronoTwitch.ChronoTwitch
    {
        public override Task<string> LoginName(string name)
        {
            if(name == "NotFound")
                return Task.FromResult(string.Empty);
            return Task.FromResult(name);
        }

        public override Task<string> DisplayName(string name)
        {
            switch (name)
            {
                case "streamer":
                    return Task.FromResult("Streamer");
                case "streamer3":
                    return Task.FromResult("Streamer3");
                case "postupdate1":
                    return Task.FromResult("PostUpdate1");
                case "postupdate2":
                    return Task.FromResult("PostUpdate2");
                case "updated":
                    return Task.FromResult("Updated");
                case "EmptyStatus":
                case "DeleteStreamer":
                    return Task.FromResult(name);
                default:
                    return Task.FromResult(string.Empty);
            }
        }

        public override Task<bool> IsLive(string name)
        {
            switch (name)
            {
                case "streamer3":
                case "postupdate1":
                case "updated":
                    return Task.FromResult(true);
                default:
                    return Task.FromResult(false);
            }
        }

        public override Task<string> GameName(string name)
        {
            switch (name)
            {
                case "streamer3":
                    return Task.FromResult("The Game");
                case "PostUpdate1":
                    return Task.FromResult("Game");
                case "updated":
                    return Task.FromResult("Play");
                default:
                    return Task.FromResult(string.Empty);
            }
        }
    }
}
