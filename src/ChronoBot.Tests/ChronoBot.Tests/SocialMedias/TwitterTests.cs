using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
using ChronoBot.Tests.Fakes;
using ChronoBot.Utilities.SocialMedias;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ChronoBot.Tests.SocialMedias
{
    public class TwitterTests
    {
        private readonly Mock<IConfiguration> _config;
        private string _path;

        public TwitterTests()
        {
            _config = new Mock<IConfiguration>();
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Discord")]).Returns("DiscordToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerKey")]).Returns("ConsumerKey");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerSecret")]).Returns("ConsumerSecret");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Token")]).Returns("TwitterToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Secret")]).Returns("Secret");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Debug")]).Returns("false");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:TextChannel")]).Returns("1");
            Statics.Config = _config.Object;
        }

        [Fact]
        public void AddTwitter_Test_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(1, 4, "Tweeter").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "Tweeter");

            Assert.NotNull(user);
            Assert.Equal("Tweeter", user.Name);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        [Fact]
        public void AddTwitter_Test_Duplicate()
        {
            var twitter = LoadTwitter(out _);

            string result = twitter.AddSocialMediaUser(123456789, 4, "Tweeter").GetAwaiter().GetResult();

            Assert.Equal("Already added Tweeter", result);
        }

        [Fact]
        public void GetTwitter_Test_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, false, "Tweeter").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Tweeter/status/chirp\n\n", result);
        }

        [Fact]
        public void DeleteTwitter_Test_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(1, 4, "DeleteTweeter").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            Assert.Single(users);
            Assert.NotNull(users.Find(x => x.Name == "DeleteTweeter"));
            Assert.Equal("DeleteTweeter", users.Find(x => x.Name == "DeleteTweeter")?.Name);
            var user = users.Find(x => x.Name == "DeleteTweeter");
            string result = twitter.DeleteSocialMediaUser(1, user?.Name);
            users = (List<SocialMediaUserData>)fileSystem.Load();

            Assert.Empty(users);
            Assert.Equal("Successfully deleted DeleteTweeter", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        private Twitter CreateNewTwitter(out SocialMediaFileSystem fileSystem)
        {
            fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "New"));
            var mockTwitter = new FakeTwitterService();
            var mockClient = new Mock<DiscordSocketClient>();

            return new Twitter(mockTwitter, mockClient.Object, _config.Object, new List<SocialMediaUserData>(), fileSystem);
        }

        private Twitter LoadTwitter(out SocialMediaFileSystem fileSystem)
        {
            fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));
            var mockTwitter = new FakeTwitterService();
            var mockClient = new Mock<DiscordSocketClient>();

            return new Twitter(mockTwitter, mockClient.Object, _config.Object, new List<SocialMediaUserData>(), fileSystem);
        }
    }
}
