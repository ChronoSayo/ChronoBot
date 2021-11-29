using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Tests.Fakes;
using ChronoBot.Utilities.SocialMedias;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Moq;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Core.RateLimiter;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Xunit;

namespace ChronoBot.Tests.SocialMedias
{
    public class TwitchTests
    {
        private Mock<TwitchAPI> _mockTwitch;
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
        public void ListSocialMedias_Test_Success()
        {
            var twitter = LoadYouTubeService();
            var result = twitter.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitch).GetAwaiter().GetResult();

            Assert.Equal("■ Streamer1 \n■ Streamer2 \n■ Streamer3 \n", result);
        }

        [Fact]
        public void DeleteYouTube_Test_Success()
        {
            var twitter = LoadCopyYouTubeService(out var fileSystem);

            string result = twitter.DeleteSocialMediaUser(123456789, "Streamer2", SocialMediaEnum.Twitch);
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            users.RemoveAll(x => x.SocialMedia != SocialMediaEnum.Twitch);

            Assert.Equal(2, users.Count);
            Assert.Equal("Successfully deleted Streamer2", result);

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }

        private Twitch LoadYouTubeService()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Twitch(_fakeTwitch, _mockClient.Object, _config.Object, new List<SocialMediaUserData>(), fileSystem);
        }

        private Twitch LoadCopyYouTubeService(out SocialMediaFileSystem fileSystem)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Update");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load", "123456789.xml"),
                Path.Combine(path, "123456789.xml"));
            fileSystem = new SocialMediaFileSystem(path);

            return new Twitch(_fakeTwitch, _mockClient.Object, _config.Object, new List<SocialMediaUserData>(), fileSystem);
        }
    }
}
