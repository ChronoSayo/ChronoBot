using System.Collections.Generic;
using System.IO;
using System.Threading;
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
using Xunit;

namespace ChronoBot.Tests.SocialMedias
{
    public class TwitchTests
    {
        private readonly Mock<DiscordSocketClient> _mockClient;
        private readonly Mock<IConfiguration> _config;

        public TwitchTests()
        {
            _config = new Mock<IConfiguration>();
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Discord")]).Returns("DiscordToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitch:ClientID")]).Returns("ConsumerKey");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitch:Secret")]).Returns("Secret");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitch:AccessToken")]).Returns("AccessToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Debug")]).Returns("false");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:Guild")]).Returns("199");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:TextChannel")]).Returns("1");
            Statics.Config = _config.Object;
            
            _mockClient = new Mock<DiscordSocketClient>(MockBehavior.Loose);
        }

        [Fact]
        public void AddTwitch_Test_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(1, 4, "streamer").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "streamer");

            Assert.NotNull(user);
            Assert.Equal("streamer", user.Name);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        [Fact]
        public void AddTwitch_Test_Duplicate_Fail()
        {
            var twitch = LoadTwitch(out _);

            string result = twitch.AddSocialMediaUser(123456789, 4, "Streamer1").GetAwaiter().GetResult();

            Assert.Equal("Already added Streamer1", result);
        }

        [Fact]
        public void AddTwitch_Test_NotFound_Fail()
        {
            var twitch = LoadTwitch(out _);

            string result = twitch.AddSocialMediaUser(123456789, 4, "NotFound").GetAwaiter().GetResult();

            Assert.Equal("Can't find NotFound", result);
        }

        [Fact]
        public void AddTwitch_Test_Channel0_Success()
        {
            var twitch = LoadTwitch(out _);

            string result = twitch.AddSocialMediaUser(123456789, 0, "NotFound").GetAwaiter().GetResult();

            Assert.Equal("Can't find NotFound", result);
        }

        [Fact]
        public void GetTwitch_Test_Offline_Success()
        {
            var twitch = LoadTwitch(out _);

            var result = twitch.GetSocialMediaUser(123456789, 2, "streamer2").GetAwaiter().GetResult();

            Assert.Equal("https://www.twitch.com/streamer2", result);
        }

        [Fact]
        public void GetTwitch_Test_Online_Success()
        {
            var twitch = LoadTwitch(out _);

            var result = twitch.GetSocialMediaUser(123456789, 3, "streamer3").GetAwaiter().GetResult();

            Assert.Equal("Streamer3 is playing The Game\nhttps://www.twitch.com/streamer3\n\n", result);
        }

        [Fact]
        public void AutoUpdate_Test_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "Post Update", 2);

            twitch.AddSocialMediaUser(123456789, 5, "postupdate1").GetAwaiter().GetResult();
            twitch.AddSocialMediaUser(123456789, 5, "postupdate2").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "postupdate1");

            Assert.NotNull(user);
            Assert.Equal("offline", user.Id);

            Thread.Sleep(2500);
            users = (List<SocialMediaUserData>)fileSystem.Load();
            user = users.Find(x => x.Name == "postupdate1");

            Assert.NotNull(user);
            Assert.Equal("online", user.Id);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetTwitch_Test_NotFindUser_Fail()
        {
            var twitch = LoadTwitch(out _);

            var result = twitch.GetSocialMediaUser(123456789, 5, "Fail").GetAwaiter().GetResult();

            Assert.Equal("Can't find streamer.", result);
        }

        [Fact]
        public void GetUpdatedTwitch_Test_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 6, "updated", 9).GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("Updated is playing Play\nhttps://www.twitch.com/updated\n\n", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_MultipleGuilds_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 6, "updated", 9).GetAwaiter().GetResult();
            twitch.AddSocialMediaUser(987654321, 6, "updated", 9).GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("Updated is playing Play\nhttps://www.twitch.com/updated\n\n", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_MessageDisplayed_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 6, "Streamer", 9).GetAwaiter().GetResult();
            twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No streamers are broadcasting.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_NoUsers_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "Empty");

            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No streamers registered.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_NoOnline_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "Empty");

            twitch.AddSocialMediaUser(123456789, 12, "EmptyStatus").GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No streamers are broadcasting.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void ListSocialMedias_Test_Success()
        {
            var twitch = LoadTwitch(out _);
            CreateNewTwitch(out var fileSystem, "List");

            twitch.AddSocialMediaUser(987654321, 5, "NewGuildStreamer").GetAwaiter().GetResult();
            var result = twitch.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitch).GetAwaiter().GetResult();

            Assert.Equal("■ streamer1 \n■ streamer2 \n■ streamer3 \n", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654321.xml"));
        }

        [Fact]
        public void ListSocialMedias_Test_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "EmptyList");
            
            var result = twitch.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitch).GetAwaiter().GetResult();

            Assert.Equal("No streamers registered.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654321.xml"));
        }

        [Fact]
        public void DeleteTwitch_Test_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "Delete");

            twitch.AddSocialMediaUser(1, 4, "DeleteStreamer").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            Assert.Single(users);
            Assert.NotNull(users.Find(x => x.Name == "DeleteStreamer"));
            Assert.Equal("DeleteStreamer", users.Find(x => x.Name == "DeleteStreamer")?.Name);
            var user = users.Find(x => x.Name == "DeleteStreamer");
            string result = twitch.DeleteSocialMediaUser(1, user?.Name, SocialMediaEnum.Twitch);
            users = (List<SocialMediaUserData>)fileSystem.Load();

            Assert.Empty(users);
            Assert.Equal("Successfully deleted DeleteStreamer", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        private Twitch CreateNewTwitch(out SocialMediaFileSystem fileSystem, string folderName = "New", int seconds = 60)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new SocialMediaFileSystem(path);

            return new Twitch(new FakeChronoTwitch(), _mockClient.Object, _config.Object,
                new List<SocialMediaUserData>(), new List<string>(), fileSystem, seconds);
        }

        private Twitch LoadTwitch(out SocialMediaFileSystem fileSystem)
        {
            fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Twitch(new FakeChronoTwitch(), _mockClient.Object, _config.Object,
                new List<SocialMediaUserData>(), new List<string>(), fileSystem);
        }
    }
}
