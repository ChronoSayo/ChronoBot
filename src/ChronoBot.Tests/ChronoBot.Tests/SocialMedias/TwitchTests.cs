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
            var user = users.Find(x => x.Name == "Streamer");

            Assert.NotNull(user);
            Assert.Equal("Streamer", user.Name);

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
        public void GetTwitch_Test_Success()
        {
            var twitch = LoadTwitch(out _);

            var result = twitch.GetSocialMediaUser(123456789, false, "Streamer1").GetAwaiter().GetResult();

            Assert.Equal("https://Twitch.com/Streamer1/status/chirp\n\n", result);
        }

        [Fact]
        public void AutoUpdate_Test_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "Post Update", 2);

            twitch.AddSocialMediaUser(123456789, 5, "postupdate1").GetAwaiter().GetResult();
            twitch.AddSocialMediaUser(123456789, 5, "postupdate2").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "PostUpdate1");

            Assert.NotNull(user);
            Assert.Equal("offline", user.Id);

            Thread.Sleep(2500);
            users = (List<SocialMediaUserData>)fileSystem.Load();
            user = users.Find(x => x.Name == "PostUpdate1");

            Assert.NotNull(user);
            Assert.Equal("online", user.Id);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetTwitch_Test_NoId_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 5, "NoId").GetAwaiter().GetResult();
            var result = twitch.GetSocialMediaUser(123456789, true, "NoId").GetAwaiter().GetResult();

            Assert.Equal("Could not retrieve Tweet.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetTwitch_Test_NotFindUser_Fail()
        {
            var twitch = LoadTwitch(out _);

            var result = twitch.GetSocialMediaUser(123456789, false, "Fail").GetAwaiter().GetResult();

            Assert.Equal("Could not find Twitch handle.", result);
        }

        [Fact]
        public void GetTwitch_Test_CouldNotRetrieve_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 5, "Fail").GetAwaiter().GetResult();
            var result = twitch.GetSocialMediaUser(123456789, false, "Fail").GetAwaiter().GetResult();

            Assert.Equal("Could not retrieve Tweet.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 6, "Updated", 9).GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("https://Twitch.com/Updated/status/kaw-kaw\n\n", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_MultipleUsers_Success()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 8, "Fail").GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_NoUpdate_Fail()
        {
            var twitch = LoadTwitch(out _);

            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);
        }

        [Fact]
        public void GetUpdatedTwitch_Test_MessageDisplayed_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem);

            twitch.AddSocialMediaUser(123456789, 6, "Streamer", 9).GetAwaiter().GetResult();
            twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_NoUsers_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "Empty");

            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No Twitch handles registered.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_NoStatus_Fail()
        {
            var Twitch = CreateNewTwitch(out var fileSystem, "Empty");

            Twitch.AddSocialMediaUser(123456789, 12, "NoStatus").GetAwaiter().GetResult();
            var result = Twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitch_Test_EmptyStatus_Fail()
        {
            var twitch = CreateNewTwitch(out var fileSystem, "Empty");

            twitch.AddSocialMediaUser(123456789, 12, "EmptyStatus").GetAwaiter().GetResult();
            var result = twitch.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void ListSocialMedias_Test_Success()
        {
            var twitch = LoadTwitch(out _);
            CreateNewTwitch(out var fileSystem, "List");

            twitch.AddSocialMediaUser(987654321, 5, "NewGuildStreamer").GetAwaiter().GetResult();
            var result = twitch.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitch).GetAwaiter().GetResult();

            Assert.Equal("■ Streamer1 \n■ Streamer2 \n■ Streamer3 \n", result);

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

            return new Twitch(new FakeChronoTwitch(), _mockClient.Object, _config.Object, new List<SocialMediaUserData>(), fileSystem, seconds);
        }

        private Twitch LoadTwitch(out SocialMediaFileSystem fileSystem)
        {
            fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Twitch(new FakeChronoTwitch(), _mockClient.Object, _config.Object, new List<SocialMediaUserData>(), fileSystem);
        }
    }
}
