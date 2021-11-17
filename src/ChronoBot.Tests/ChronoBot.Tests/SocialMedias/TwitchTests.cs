using System.Collections.Generic;
using System.IO;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
using ChronoBot.Tests.Fakes;
using ChronoBot.Utilities.SocialMedias;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Moq;
using TwitchLib.Api;
using Xunit;

namespace ChronoBot.Tests.SocialMedias
{
    public class TwitchTests
    {
        private readonly FakeTwitchApi _fakeTwitch;
        private readonly Mock<DiscordSocketClient> _mockClient;
        private readonly Mock<IConfiguration> _config;

        public TwitchTests()
        {
            _config = new Mock<IConfiguration>();
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Discord")]).Returns("DiscordToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitch:ClientID")]).Returns("ClientID");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitch:Secret")]).Returns("Secret");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Debug")]).Returns("false");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:Guild")]).Returns("199");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:TextChannel")]).Returns("1");
            Statics.Config = _config.Object;

            _fakeTwitch = new FakeTwitchApi();
            _mockClient = new Mock<DiscordSocketClient>(MockBehavior.Loose);
        }

        [Fact]
        public void Test()
        {
            var twitch = CreateNewTwitter(out var fileSystem);

            twitch.AddSocialMediaUser(1, 4, "Streamer").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "Streamer");

            Assert.NotNull(user);

            Assert.NotNull(user);
            Assert.Equal("Streamer", user.Name);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        private Twitch CreateNewTwitter(out SocialMediaFileSystem fileSystem, string folderName = "New", int seconds = 60)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new SocialMediaFileSystem(path);

            return new Twitch(_fakeTwitch, _mockClient.Object, _config.Object, new List<SocialMediaUserData>(), fileSystem, seconds);
        }
    }
}
