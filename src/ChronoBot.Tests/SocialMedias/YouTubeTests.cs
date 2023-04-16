using System.Collections.Generic;
using System.IO;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Tests.Fakes;
using ChronoBot.Utilities.SocialMedias;
using Discord.WebSocket;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ChronoBot.Tests.SocialMedias
{
    public class YouTubeTests
    {
        private readonly FakeYouTubeService _fakeYouTube;
        private readonly Mock<DiscordSocketClient> _mockClient;
        private readonly Mock<IConfiguration> _config;

        public YouTubeTests()
        {
            _config = new Mock<IConfiguration>();
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Discord")]).Returns("DiscordToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:YouTube")]).Returns("ApiKey");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Debug")]).Returns("false");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:Guild")]).Returns("199");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:TextChannel")]).Returns("1");
            Statics.Config = _config.Object;

            _fakeYouTube = new FakeYouTubeService(new BaseClientService.Initializer
                { ApiKey = _config.Object[Statics.YouTubeApiKey], ApplicationName = "UnitTest" });
            _mockClient = new Mock<DiscordSocketClient>(MockBehavior.Loose);
        }

        //[Fact]
        //public void AddYouTube_Test_Success()
        //{
        //    var youTube = CreateNewYouTube(out var fileSystem);

        //    youTube.AddSocialMediaUser(1, 4, "YouTuber").GetAwaiter().GetResult();
        //    var users = (List<SocialMediaUserData>)fileSystem.Load();
        //    var user = users.Find(x => x.Name == "YouTuber");

        //    Assert.NotNull(user);
        //    Assert.Equal("YouTuber", user.Name);

        //    File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        //}

        [Fact]
        public void ListSocialMedias_Test_Success()
        {
            var youtube = LoadYouTubeService();
            var result = youtube.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.YouTube).GetAwaiter().GetResult();
            
            Assert.Equal("■ YouTuber1 (https://www.youtube.com/@YouTuber1) \n" +
                         "■ YouTuber2 (https://www.youtube.com/@YouTuber2) \n" +
                         "■ YouTuber3 (https://www.youtube.com/@YouTuber3) \n", result);
        }

        [Fact]
        public void DeleteYouTube_Test_Success()
        {
            var youtube = LoadCopyYouTubeService(out var fileSystem);
            
            string result = youtube.DeleteSocialMediaUser(123456789, "YouTuber2", SocialMediaEnum.YouTube);
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            users.RemoveAll(x => x.SocialMedia != SocialMediaEnum.YouTube);

            Assert.Equal(2, users.Count);
            Assert.Equal("Successfully deleted YouTuber2", result);

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }

        private YouTube LoadYouTubeService()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new YouTube(_fakeYouTube, _mockClient.Object, _config.Object, new List<SocialMediaUserData>(),
                fileSystem);
        }

        private YouTube CreateNewYouTube(out SocialMediaFileSystem fileSystem, string folderName = "New", int seconds = 60)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new SocialMediaFileSystem(path);

            return new YouTube(_fakeYouTube, _mockClient.Object, _config.Object,
                new List<SocialMediaUserData>(), fileSystem, seconds);
        }

        private YouTube LoadCopyYouTubeService(out SocialMediaFileSystem fileSystem)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Update");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load", "123456789.xml"), 
                Path.Combine(path, "123456789.xml"));
            fileSystem = new SocialMediaFileSystem(path);

            return new YouTube(_fakeYouTube, _mockClient.Object, _config.Object, new List<SocialMediaUserData>(),
                fileSystem);
        }
    }
}
