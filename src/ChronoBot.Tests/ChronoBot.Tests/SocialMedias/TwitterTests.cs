using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
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
        private const string _consumerKey = "EDulDDqiUeB3fUJtgpyaW1MT5";
        private const string _consumerSecret = "C8cENULvi9wxRr2TUvIjHylz23316jcglT0vhlcnYwZHHKD6Zp";

        private const string _bearerToken =
            "AAAAAAAAAAAAAAAAAAAAADAFUwEAAAAAFO2iPXO%2Ba5BTlxpSJVZXdMxIMmY%3DRKGCpo7NIxM8Tp2yDb6tt0kW0CGI4AWJwVXzy56tSX3H6tGvW8";

        private const string _token = "242392613-jIbRzxLyeg1oaah1KJzlbUfCYYcWIAmKSSY5BToP";
        private const string _secret = "LV3I6oajKsKxGv3Jp8X1DvJMWXKjFC9x7KP1NYoo9q1Qq";

        [Fact]
        public void AddTwitter_Test_Success()
        {
            var config = new Mock<IConfiguration>();
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerKey")]).Returns(_consumerKey);
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerSecret")]).Returns(_consumerSecret);
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Token")]).Returns(_token);
            config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Secret")]).Returns(_secret);
            config.SetupGet(x => x[It.Is<string>(y => y == "Debug")]).Returns("true");
            config.SetupGet(x => x[It.Is<string>(y => y == "IDs:TextChannel")]).Returns("1");
            Statics.Config = config.Object;
            SocialMediaFileSystem fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name));
            Twitter twitter = new Twitter(new DiscordSocketClient(), config.Object, new List<SocialMediaUserData>(), fileSystem);

            twitter.AddSocialMediaUser(123456789, 4, "daigothebeast").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "daigothebeast");
            Assert.Equal("daigothebeast", user.Name);
        }
    }
}
