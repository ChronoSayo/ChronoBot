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
    public class TwitterTests
    {
        private readonly Mock<DiscordSocketClient> _mockClient;
        private readonly Mock<IConfiguration> _config;

        public TwitterTests()
        {
            _config = new Mock<IConfiguration>();
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Discord")]).Returns("DiscordToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerKey")]).Returns("ConsumerKey");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:ConsumerSecret")]).Returns("ConsumerSecret");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Token")]).Returns("TwitterToken");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Tokens:Twitter:Secret")]).Returns("Secret");
            _config.SetupGet(x => x[It.Is<string>(y => y == "Debug")]).Returns("false");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:Guild")]).Returns("199");
            _config.SetupGet(x => x[It.Is<string>(y => y == "IDs:TextChannel")]).Returns("1");
            Statics.Config = _config.Object;
            
            _mockClient = new Mock<DiscordSocketClient>(MockBehavior.Loose);
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
        public void AddTwitter_Test_Duplicate_Fail()
        {
            var twitter = LoadTwitter(out _);

            string result = twitter.AddSocialMediaUser(123456789, 4, "Tweeter1").GetAwaiter().GetResult();

            Assert.Equal("Already added Tweeter1", result);
        }

        [Fact]
        public void AddTwitter_Test_NotFound_Fail()
        {
            var twitter = LoadTwitter(out _);

            string result = twitter.AddSocialMediaUser(123456789, 4, "NotFound").GetAwaiter().GetResult();

            Assert.Equal("Can't find NotFound", result);
        }

        [Fact]
        public void AutoUpdate_Test_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "Post Update", 2);

            twitter.AddSocialMediaUser(123456789, 5, "PostUpdate").GetAwaiter().GetResult();
            twitter.AddSocialMediaUser(123456789, 5, "PostUpdate2").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "PostUpdate");

            Assert.NotNull(user);
            Assert.Equal("0", user.Id);

            Thread.Sleep(2600);
            users = (List<SocialMediaUserData>)fileSystem.Load();
            user = users.Find(x => x.Name == "PostUpdate");

            Assert.NotNull(user);
            Assert.Equal("chirp", user.Id);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetTwitter_Test_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, "Tweeter1").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Tweeter1/status/chirp\n\n", result);
        }

        [Fact]
        public void GetTwitter_IncludingLikes_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, "Tweeter1").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Tweeter1/status/chirp\n\n", result);
        }

        [Fact]
        public void GetTwitter_OnlyPosts_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, "Tweeter4").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Tweeter4/status/chirp\n\n", result);
        }

        [Fact]
        public void GetTwitter_MultipleOptions_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, "Tweeter5").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Tweeter5/status/chirp\n\n", result);
        }

        [Fact]
        public void GetTwitter_OnlyLikes_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, "Tweeter6").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Tweeter6/status/123\n\n", result);
        }

        [Fact]
        public void GetTwitter_OnlyGifs_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, "Tweeter7").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Tweeter7/status/chirp\n\n", result);
        }

        [Fact]
        public void GetTwitter_Test_NoId_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(123456789, 5, "NoId", options: "p").GetAwaiter().GetResult();
            var result = twitter.GetSocialMediaUser(123456789, "NoId").GetAwaiter().GetResult();

            Assert.Equal("Could not retrieve Tweet.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetTwitter_Test_NotFindUser_Fail()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.GetSocialMediaUser(123456789, "Fail").GetAwaiter().GetResult();

            Assert.Equal("Could not find Twitter handle.", result);
        }

        [Fact]
        public void GetTwitter_Test_CouldNotRetrieve_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(123456789, 5, "Fail").GetAwaiter().GetResult();
            var result = twitter.GetSocialMediaUser(123456789, "Fail").GetAwaiter().GetResult();

            Assert.Equal("Could not retrieve Tweet.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetTwitter_Test_UnavailableOption_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            var result = twitter.AddSocialMediaUser(123456789, 5, "Fail", options:"f").GetAwaiter().GetResult();

            Assert.Equal($"Unrecognizable option: \"f\"", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitter_Test_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(123456789, 6, "Updated", 9).GetAwaiter().GetResult();
            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/Updated/status/kaw-kaw\n\n", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitter_Test_MultipleUsers_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(123456789, 8, "Fail").GetAwaiter().GetResult();
            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitter_Test_NoUpdate_Fail()
        {
            var twitter = LoadTwitter(out _);
            
            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);
        }

        [Fact]
        public void GetUpdatedTwitter_Test_MessageDisplayed_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(123456789, 6, "Tweeter", 9).GetAwaiter().GetResult();
            twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();
            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitter_Test_NoUsers_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "Empty");

            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No Twitter handles registered.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitter_Test_NoStatus_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "Empty");

            twitter.AddSocialMediaUser(123456789, 12, "NoStatus").GetAwaiter().GetResult();
            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void GetUpdatedTwitter_Test_EmptyStatus_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "Empty");

            twitter.AddSocialMediaUser(123456789, 12, "EmptyStatus").GetAwaiter().GetResult();
            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No updates since last time.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void ListSocialMedias_Test_Success()
        {
            var twitter = LoadTwitter(out _);
            CreateNewTwitter(out var fileSystem, "List");
            
            twitter.AddSocialMediaUser(987654321, 5, "NewGuildTweeter").GetAwaiter().GetResult();
            var result = twitter.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitter).GetAwaiter().GetResult();

            Assert.Equal("■ Tweeter1 \n■ Tweeter2 \n■ Tweeter3 \n■ Tweeter4 \n■ Tweeter5 \n■ Tweeter6 \n■ Tweeter7 \n", result);
            
            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654321.xml"));
        }

        [Fact]
        public void ListSocialMedias_Test_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "List");
            
            var result = twitter.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitter).GetAwaiter().GetResult();

            Assert.Equal("No Twitter handles registered.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654321.xml"));
        }

        [Fact]
        public void DeleteTwitter_Test_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "Delete");

            twitter.AddSocialMediaUser(1, 4, "DeleteTweeter").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            Assert.Single(users);
            Assert.NotNull(users.Find(x => x.Name == "DeleteTweeter"));
            Assert.Equal("DeleteTweeter", users.Find(x => x.Name == "DeleteTweeter")?.Name);
            var user = users.Find(x => x.Name == "DeleteTweeter");
            string result = twitter.DeleteSocialMediaUser(1, user?.Name, SocialMediaEnum.Twitter);
            users = (List<SocialMediaUserData>)fileSystem.Load();

            Assert.Empty(users);
            Assert.Equal("Successfully deleted DeleteTweeter", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        private Twitter CreateNewTwitter(out SocialMediaFileSystem fileSystem, string folderName = "New", int seconds = 60)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if(Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new SocialMediaFileSystem(path);

            return new Twitter(new FakeTwitterService(), _mockClient.Object, _config.Object,
                new List<SocialMediaUserData>(), new List<string>(), fileSystem, seconds);
        }

        private Twitter LoadTwitter(out SocialMediaFileSystem fileSystem)
        {
            fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Twitter(new FakeTwitterService(), _mockClient.Object, _config.Object,
                new List<SocialMediaUserData>(), new List<string>(), fileSystem);
        }
    }
}
