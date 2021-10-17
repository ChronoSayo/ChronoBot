using System.Collections.Generic;
using System.IO;
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
using TweetSharp;
using Xunit;

namespace ChronoBot.Tests.SocialMedias
{
    public class TwitterTests
    {
        [Fact]
        public void AddTwitter_Test_Success()
        {
            var config = new Mock<IConfiguration>();
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Discord")]).Returns("DiscordToken");
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerKey")]).Returns("ConsumerKey");
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerSecret")]).Returns("ConsumerSecret");
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Token")]).Returns("TwitterToken");
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Secret")]).Returns("Secret");
            config.SetupGet(x => x[It.Is<string>(y => y == "Debug")]).Returns("true");
            config.SetupGet(x => x[It.Is<string>(y => y == "IDs:TextChannel")]).Returns("1");
            Statics.Config = config.Object;
            SocialMediaFileSystem fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name));
            var mockTwitter = new FakeTwitterService();
            var mockClient = new Mock<DiscordSocketClient>();
            Twitter twitter = new Twitter(mockTwitter, mockClient.Object, config.Object, new List<SocialMediaUserData>(), fileSystem);
            
            twitter.AddSocialMediaUser(123456789, 4, "daigothebeast").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "daigothebeast");
            Assert.Equal("daigothebeast", user.Name);
        }
    }
}
