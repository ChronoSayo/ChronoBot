using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Interfaces;
using Xunit;

namespace ChronoBot.Tests
{
    public class SocialMediaFileSystemTests
    {
        [Fact]
        public void Load_Test_Success()
        {
            var fileSystem =
                new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files"));

            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.Equal(3, users.Count);
            Assert.Equal("YouTube", users[0].SocialMedia);
            Assert.Equal("YouTuber", users[0].Name);
            Assert.Equal(1, (int)users[0].ChannelId);
            Assert.Equal("Twitter", users[1].SocialMedia);
            Assert.Equal("Tweeter", users[1].Name);
            Assert.Equal(3, (int)users[1].ChannelId);
            Assert.Equal("Twitch", users[2].SocialMedia);
            Assert.Equal("Streamer", users[2].Name);
            Assert.Equal(2, (int)users[2].ChannelId);
        }

        [Fact]
        public void Load_Test_Fail()
        {
            var fileSystem = new SocialMediaFileSystem("fail");
            var users = fileSystem.Load();
            Assert.Equal(3, users.Count());
        }
    }
}
