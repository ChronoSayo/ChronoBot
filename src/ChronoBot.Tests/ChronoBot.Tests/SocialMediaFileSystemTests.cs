using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using Xunit;
using Xunit.Extensions;

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
            Equal("YouTube", "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal("Twitter", "Tweeter", 123456789, 3, "chirp", users[1]);
            Equal("Twitch", "Streamer", 123456789, 2, "spasm", users[2]);
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
            Equal("YouTube", "Testies", 987654321, 112233, "778899", users[0]);
            Equal("Twitter", "Test", 987654321, 123456789, "134679258", users[1]);
            Equal("Twitch", "Testing", 987654321, 11111, "33333", users[2]);
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
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal("update", users[0].Id);

            File.Delete(file);
        }
        [Fact]
        public void Update_Test_UserDataNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Update");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            users[0].Name = "Fail";
            bool ok = fileSystem.UpdateFile(users[0]);
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.False(ok);
            Equal("YouTube", "YouTuber", 123456789, 1, "wecylinder", users[0]);

            File.Delete(file);
        }
        [Fact]
        public void Update_Test_FileNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Update");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            users[0].GuildId = 2;
            bool ok = fileSystem.UpdateFile(users[0]);
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.False(ok);
            Equal("YouTube", "YouTuber", 123456789, 1, "wecylinder", users[0]);

            File.Delete(file);
        }

        [Fact]
        public void Delete_Test_Success()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            bool ok = fileSystem.DeleteInFile(users[0]);
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal(2, users.Count);
            Equal("Twitter", "Tweeter", 123456789, 3, "chirp", users[0]);
            Equal("Twitch", "Streamer", 123456789, 2, "spasm", users[1]);
        }
        [Fact]
        public void Delete_Test_UserDataNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            bool ok = fileSystem.DeleteInFile(new SocialMediaUserData { GuildId = 123456789, Name = "Fail" });
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.False(ok);
            Assert.Equal(3, users.Count);
            Equal("YouTube", "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal("Twitter", "Tweeter", 123456789, 3, "chirp", users[1]);
            Equal("Twitch", "Streamer", 123456789, 2, "spasm", users[2]);
        }
        [Fact]
        public void Delete_Test_FileNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            users[0].GuildId = 16;
            bool ok = fileSystem.DeleteInFile(users[0]);
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.False(ok);
            Assert.Equal(3, users.Count);
            Equal("YouTube", "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal("Twitter", "Tweeter", 123456789, 3, "chirp", users[1]);
            Equal("Twitch", "Streamer", 123456789, 2, "spasm", users[2]);
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

        private void Equal(string expectedSocialMedia, string expectedName, ulong expectedGuildId,
            ulong expectedChannelId, string expectedId, SocialMediaUserData actualUserData)
        {
            Assert.Equal(expectedSocialMedia, actualUserData.SocialMedia);
            Assert.Equal(expectedName, actualUserData.Name);
            Assert.Equal(expectedGuildId, actualUserData.GuildId);
            Assert.Equal(expectedChannelId, actualUserData.ChannelId);
            Assert.Equal(expectedId, actualUserData.Id);
        }
    }
}