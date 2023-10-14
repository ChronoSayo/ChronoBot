using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        public void SearchTwitter_Multiple_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(1, 4, "Multiple").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "Multiple");

            Assert.NotNull(user);
            Assert.Equal("Multiple", user.Name);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        [Fact]
        public void SearchTwitter_MultipleSlightlyDifferent_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(1, 4, "MultipleSlightlyDifferent").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "MultipleSlightlyDifferent");

            Assert.NotNull(user);
            Assert.Equal("MultipleSlightlyDifferent", user.Name);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
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
        public void AddTwitter_WithUrl_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(1, 4, "https://twitter.com/TwitterUrl").GetAwaiter().GetResult();
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "TwitterUrl");

            Assert.NotNull(user);
            Assert.Equal("TwitterUrl", user.Name);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "1.xml"));
        }

        [Fact]
        public void AddTwitter_AnotherChannel_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(123456789, 5, "AnotherChannel", 6).GetAwaiter().GetResult();
            var result = twitter.GetSocialMediaUser(123456789, "AnotherChannel").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/AnotherChannel/status/chirp\n\n", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }


        [Fact]
        public void AddTwitter_AnotherChannelAndOption_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem);

            twitter.AddSocialMediaUser(123456789, 5, "AnotherChannelAndPost", 6, "p").GetAwaiter().GetResult();
            var result = twitter.GetSocialMediaUser(123456789, "AnotherChannelAndPost").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/AnotherChannelAndPost/status/chirp\n\n", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
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
        public void AutoUpdate_Test_NoTwitterUsers_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "Empty", 2);

            Thread.Sleep(2600);
            var result = twitter.GetUpdatedSocialMediaUsers(123456789).GetAwaiter().GetResult();

            Assert.Equal("No Twitter handles registered.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
        }

        [Fact]
        public void AutoUpdate_LoopCurrentUser_Success()
        {
            LoadTwitter(out var fileSystem, 1);
            
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            users = users.FindAll(x => x.SocialMedia == SocialMediaEnum.Twitter);

            for (int i = 0; i < users.Count + 1; i++)
            {
                int index = i;
                if (i == users.Count)
                    index = 0;

                var user = users[index];

                Assert.NotNull(user);

                switch (i)
                {
                    case 7:
                    case 0:
                        Assert.Equal("Tweeter1", user.Name);
                        Assert.Equal("1", user.ChannelId.ToString());
                        Assert.Equal("chirp", user.Id);
                        break;
                    case 1:
                        Assert.Equal("Tweeter2", user.Name);
                        Assert.Equal("2", user.ChannelId.ToString());
                        Assert.Equal("chirps", user.Id);
                        break;
                    case 2:
                        Assert.Equal("Tweeter3", user.Name);
                        Assert.Equal("3", user.ChannelId.ToString());
                        Assert.Equal("chirpf", user.Id);
                        break;
                    case 3:
                        Assert.Equal("Tweeter4", user.Name);
                        Assert.Equal("4", user.ChannelId.ToString());
                        Assert.Equal("chirp", user.Id);
                        Assert.Equal("p", user.Options);
                        break;
                    case 4:
                        Assert.Equal("Tweeter5", user.Name);
                        Assert.Equal("5", user.ChannelId.ToString());
                        Assert.Equal("chirp", user.Id);
                        Assert.Equal("q r", user.Options);
                        break;
                    case 5:
                        Assert.Equal("Tweeter6", user.Name);
                        Assert.Equal("6", user.ChannelId.ToString());
                        Assert.Equal("789", user.Id);
                        Assert.Equal("l", user.Options);
                        break;
                    case 6:
                        Assert.Equal("Tweeter7", user.Name);
                        Assert.Equal("7", user.ChannelId.ToString());
                        Assert.Equal("chirp", user.Id);
                        Assert.Equal("mg", user.Options);
                        break;
                }

                Thread.Sleep(1000);
            }
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
        public void GetTwitter_AddLikeHistory_Success()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "AddLike", 1);

            twitter.AddSocialMediaUser(111111111, 5, "AddLike", options: "l").GetAwaiter().GetResult();
            var result = twitter.GetSocialMediaUser(111111111, "AddLike").GetAwaiter().GetResult();

            Assert.Equal("https://twitter.com/AddLike/status/123\n\n", result);

            Thread.Sleep(1500);
            var users = (List<SocialMediaUserData>)fileSystem.Load();
            var user = users.Find(x => x.Name == "AddLike");

            Assert.NotNull(user);
            Assert.Equal("987", user.Id);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "111111111.xml"));
        }

        [Fact]
        public void GetTwitter_NoLike_Fail()
        {
            var twitter = CreateNewTwitter(out var fileSystem, "NoLike");

            twitter.AddSocialMediaUser(123456789, 5, "NoLike", options: "l").GetAwaiter().GetResult();
            var result = twitter.GetSocialMediaUser(123456789, "NoLike").GetAwaiter().GetResult();

            Assert.Equal("Could not retrieve Tweet.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
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
        public void PostVideo_PostVideo_Success()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.PostVideo("https://twitter.com/Video/status/1").Result;

            Assert.Equal("https://video.twimg.com/ext_tw_video/2/pu/vid/1280x720/swhi5fpAMRc-fzJp.mp4?tag=12", result);
        }

        [Fact]
        public void PostVideo_IncorrectVideoFormat_Fail()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.PostVideo("https://twitter.com/Video/status/2").Result;

            Assert.Equal("", result);
        }

        [Fact]
        public void PostVideo_NoTweets_Fail()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.PostVideo("https://twitter.com/Video/status/3").Result;

            Assert.Equal("", result);
        }

        [Fact]
        public void PostVideo_WrongMedia_Fail()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.PostVideo("https://twitter.com/Video/status/4").Result;

            Assert.Equal("", result);
        }

        [Fact]
        public void PostVideo_NoExtendedTweets_Fail()
        {
            var twitter = LoadTwitter(out _);

            var result = twitter.PostVideo("https://twitter.com/Video/status/5").Result;

            Assert.Equal("", result);
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
            var twitter1 = LoadTwitter(out _);
            var twitter2 = CreateNewTwitter(out var fileSystem, "List");
            
            twitter2.AddSocialMediaUser(987654321, 5, "NewGuildTweeter").GetAwaiter().GetResult();
            var result = twitter1.ListSavedSocialMediaUsers(123456789, SocialMediaEnum.Twitter).GetAwaiter().GetResult();

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
                new List<SocialMediaUserData>(), fileSystem, seconds);
        }

        private Twitter LoadTwitter(out SocialMediaFileSystem fileSystem, int seconds = 10)
        {
            fileSystem = new SocialMediaFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Twitter(new FakeTwitterService(), _mockClient.Object, _config.Object,
                new List<SocialMediaUserData>(), fileSystem, seconds);
        }
    }
}
