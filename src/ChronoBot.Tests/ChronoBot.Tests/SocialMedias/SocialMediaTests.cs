using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ChronoBot.Tests.SocialMedias
{
    public class SocialMediaTests
    {
        private readonly SocialMedia _socialMedia;

        public SocialMediaTests()
        {
            var mockClient = new Mock<DiscordSocketClient>(MockBehavior.Loose);
            var config = new Mock<IConfiguration>();
            _socialMedia = new SocialMedia(mockClient.Object, config.Object,
                new List<SocialMediaUserData>(), new SocialMediaFileSystem());
        }

        [Fact]
        public void AddSocialMediaUsers_Test_Success()
        {
            var result = _socialMedia.AddSocialMediaUser(123456789, 1, "Test", 2).GetAwaiter().GetResult();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetSocialMediaUsers_Test_General_Success()
        {
            var result = _socialMedia.GetSocialMediaUser(123456789, 1, "Test").GetAwaiter().GetResult();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetSocialMediaUsers_Test_Nsfw_Success()
        {
            var result = _socialMedia.GetSocialMediaUser(123456789, false, "Test").GetAwaiter().GetResult();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ListSocialMediaUsers_Test_Success()
        {
            var result = _socialMedia.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitter).GetAwaiter().GetResult();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void DeleteSocialMediaUsers_Test_Success()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Delete");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            var users = new List<SocialMediaUserData>()
            {
                new()
                {
                    ChannelId = 987654321, GuildId = 123456789, Id = "1", Name = "Test1",
                    SocialMedia = SocialMediaEnum.Twitter
                },
                new()
                {
                    ChannelId = 123456789, GuildId = 987654321, Id = "2", Name = "Test2",
                    SocialMedia = SocialMediaEnum.Twitch
                }
            };
            var socialMedia = new SocialMedia(new Mock<DiscordSocketClient>().Object, new Mock<IConfiguration>().Object,
                users, new SocialMediaFileSystem(path));

            string result = socialMedia.DeleteSocialMediaUser(123456789, "Test1", SocialMediaEnum.Twitch);

            Assert.Equal("Successfully deleted Test1", result);

            Directory.Delete(Path.Combine(path));
        }

        [Fact]
        public void DeleteSocialMediaUsers_Test_Fail()
        {
            var result = _socialMedia.DeleteSocialMediaUser(123456789, "Fail", SocialMediaEnum.YouTube);
            Assert.Equal("Failed to delete Fail", result);
        }

        [Fact]
        public void GetUpdatedSocialMediaUsers_Test_Success()
        {
            var result = _socialMedia.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();
            Assert.Equal(string.Empty, result);
        }
    }
}
