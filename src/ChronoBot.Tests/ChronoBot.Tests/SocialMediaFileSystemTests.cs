using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using Xunit;

namespace ChronoBot.Tests
{
    public class SocialMediaFileSystemTests
    {
        private readonly string _path;

        public SocialMediaFileSystemTests()
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files");
        }

        [Fact]
        public void SetPath_Test_NoPath()
        {
            var fileSystem = new SocialMediaFileSystem();

            Assert.True(Directory.Exists(fileSystem.PathToSaveFile));

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }
        [Fact]
        public void SetPath_Test_WithPath()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(_path, "Set Path"));

            Assert.True(Directory.Exists(fileSystem.PathToSaveFile));

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }

        [Fact]
        public void Load_Test_Success()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(_path, "Load"));

            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml")));
            Assert.Equal(3, users.Count);
            Assert.Equal("YouTube", users[0].SocialMedia);
            Assert.Equal("YouTuber", users[0].Name);
            Assert.Equal(1, (int)users[0].ChannelId);
            Assert.Equal("wecylinder", users[0].Id);
            Assert.Equal("Twitter", users[1].SocialMedia);
            Assert.Equal("Tweeter", users[1].Name);
            Assert.Equal(3, (int)users[1].ChannelId);
            Assert.Equal("chirp", users[1].Id);
            Assert.Equal("Twitch", users[2].SocialMedia);
            Assert.Equal("Streamer", users[2].Name);
            Assert.Equal(2, (int)users[2].ChannelId);
            Assert.Equal("spasm", users[2].Id);
        }

        [Fact]
        public void Load_Test_Fail()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(_path, "Load Fail"));

            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(Path.Combine(fileSystem.PathToSaveFile, "fail.xml")));
            Assert.Empty(users);
        }

        [Fact]
        public void Save_Test_Success()
        {
            var tuple = SetUpSaveFileTests();
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> saveUsers = new List<SocialMediaUserData>()
            {
                new SocialMediaUserData{
                    Name = "Test",
                    SocialMedia = "Twitter",
                    ChannelId = 123456789,
                    GuildId = 987654321,
                    Id = "134679258"
                },

                new SocialMediaUserData{
                    Name = "Testing",
                    SocialMedia = "Twitch",
                    ChannelId = 11111,
                    GuildId = 987654321,
                    Id = "33333"
                },

                new SocialMediaUserData{
                    Name = "Testies",
                    SocialMedia = "YouTube",
                    ChannelId = 112233,
                    GuildId = 987654321,
                    Id = "778899"
                }
            };

            bool ok = false;
            foreach (SocialMediaUserData data in saveUsers)
                ok |= fileSystem.Save(data);
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal(3, users.Count);
            Assert.Equal("Twitter", users[1].SocialMedia);
            Assert.Equal("Test", users[1].Name);
            Assert.Equal((double)123456789, users[1].ChannelId);
            Assert.Equal("134679258", users[1].Id);
            Assert.Equal("Twitch", users[2].SocialMedia);
            Assert.Equal("Testing", users[2].Name);
            Assert.Equal((double)11111, users[2].ChannelId);
            Assert.Equal("33333", users[2].Id);
            Assert.Equal("YouTube", users[0].SocialMedia);
            Assert.Equal("Testies", users[0].Name);
            Assert.Equal((double)112233, users[0].ChannelId);
            Assert.Equal("778899", users[0].Id);
        }

        [Fact]
        public void Save_Test_Fail()
        {
            var tuple = SetUpSaveFileTests();
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            SocialMediaUserData user = new SocialMediaUserData();

            bool ok = fileSystem.Save(user);

            Assert.False(ok);
            Assert.False(File.Exists(file));
        }

        [Fact]
        public void Update_Test_Success()
        {
            var tuple = SetUpCopyOfFileTests("Update");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            users[0].Id = "update";
            bool ok = fileSystem.UpdateFile(users[0]);

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal("update", users[0].Id);

            File.Delete(file);
        }

        [Fact]
        public void Update_Test_Fail()
        {

        }

        [Fact]
        public void Delete_Test_Success()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;

        }

        [Fact]
        public void Delete_Test_Fail()
        {

        }

        private Tuple<SocialMediaFileSystem, string> SetUpSaveFileTests()
        {
            var fileSystem =
                new SocialMediaFileSystem(Path.Combine(_path, "Save"));
            string file = Path.Combine(fileSystem.PathToSaveFile, "987654321.xml");
            if (File.Exists(file))
                File.Delete(file);

            return new Tuple<SocialMediaFileSystem, string>(fileSystem, file);
        }

        private Tuple<SocialMediaFileSystem, string> SetUpCopyOfFileTests(string folderName)
        {
            var srcFile = Path.Combine(_path, "Load", "123456789.xml");
            var updatePath = Path.Combine(_path, folderName);
            if(Directory.Exists(updatePath))
                Directory.Delete(updatePath, true);
            var fileSystem = new SocialMediaFileSystem(updatePath);
            File.Copy(srcFile, Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
            var file = Path.Combine(_path, folderName, "123456789.xml");

            return new Tuple<SocialMediaFileSystem, string>(fileSystem, file);
        }
    }
}