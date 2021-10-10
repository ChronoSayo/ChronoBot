using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using Xunit;

namespace ChronoBot.Tests.SocialMedias
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
        public void Load_Test_SingleFile_Success()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(_path, "Load"));

            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml")));
            Assert.Equal(3, users.Count);
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal(SocialMediaEnum.Twitter, "Tweeter", 123456789, 3, "chirp", users[1]);
            Equal(SocialMediaEnum.Twitch, "Streamer", 123456789, 2, "spasm", users[2]);
        }
        [Fact]
        public void Load_Test_MultipleFiles_Success()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(_path, "Load"));
            var file1 = Path.Combine(fileSystem.PathToSaveFile, "123456789.xml");
            var file2 = Path.Combine(fileSystem.PathToSaveFile, "987654321.xml");
            
            if(File.Exists(file2))
                File.Delete(file2);
            File.Copy(file1, file2);
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file1));
            Assert.True(File.Exists(file2));
            Assert.Equal(6, users.Count);
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal(SocialMediaEnum.YouTube, "YouTuber", 987654321, 1, "wecylinder", users[1]);
            Equal(SocialMediaEnum.Twitter, "Tweeter", 123456789, 3, "chirp", users[2]);
            Equal(SocialMediaEnum.Twitter, "Tweeter", 987654321, 3, "chirp", users[3]);
            Equal(SocialMediaEnum.Twitch, "Streamer", 123456789, 2, "spasm", users[4]);
            Equal(SocialMediaEnum.Twitch, "Streamer", 987654321, 2, "spasm", users[5]);

            File.Delete(file2);
        }
        [Fact]
        public void Load_Test_MultipleFilesWithOneWorking_Success()
        {
            var fileSystem = new SocialMediaFileSystem(Path.Combine(_path, "Load"));
            var file1 = Path.Combine(fileSystem.PathToSaveFile, "123456789.xml");
            var file2 = Path.Combine(fileSystem.PathToSaveFile, "987654321.txt");

            if (File.Exists(file2))
                File.Delete(file2);
            File.Copy(file1, file2);
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file1));
            Assert.True(File.Exists(file2));
            Assert.Equal(3, users.Count);
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal(SocialMediaEnum.Twitter, "Tweeter", 123456789, 3, "chirp", users[1]);
            Equal(SocialMediaEnum.Twitch, "Streamer", 123456789, 2, "spasm", users[2]);

            File.Delete(file2);
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
                new()
                {
                    Name = "Test",
                    SocialMedia = SocialMediaEnum.Twitter,
                    ChannelId = 123456789,
                    GuildId = 987654321,
                    Id = "134679258"
                },

                new()
                {
                    Name = "Testing",
                    SocialMedia = SocialMediaEnum.Twitch,
                    ChannelId = 11111,
                    GuildId = 987654321,
                    Id = "33333"
                },

                new()
                {
                    Name = "Testies",
                    SocialMedia = SocialMediaEnum.YouTube,
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
            Equal(SocialMediaEnum.YouTube, "Testies", 987654321, 112233, "778899", users[0]);
            Equal(SocialMediaEnum.Twitter, "Test", 987654321, 123456789, "134679258", users[1]);
            Equal(SocialMediaEnum.Twitch, "Testing", 987654321, 11111, "33333", users[2]);
        }
        [Fact]
        public void Save_Test_N_Success()
        {
            var tuple = SetUpSaveFileTests();
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> saveUsers = new List<SocialMediaUserData>()
            {
                new()
                {
                    Name = "Test",
                    SocialMedia = SocialMediaEnum.Twitter,
                    ChannelId = 123456789,
                    GuildId = 987654321,
                    Id = "134679258"
                },

                new() 
                {
                    Name = "Testing",
                    SocialMedia = SocialMediaEnum.Twitch,
                    ChannelId = 11111,
                    GuildId = 987654321,
                    Id = "33333"
                },

                new()
                {
                    Name = "Testies",
                    SocialMedia = SocialMediaEnum.YouTube,
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
            Equal(SocialMediaEnum.YouTube, "Testies", 987654321, 112233, "778899", users[0]);
            Equal(SocialMediaEnum.Twitter, "Test", 987654321, 123456789, "134679258", users[1]);
            Equal(SocialMediaEnum.Twitch, "Testing", 987654321, 11111, "33333", users[2]);
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

            users[1].Id = "update";
            bool ok = fileSystem.UpdateFile(users[1]);
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal("update", users[1].Id);

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
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);

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
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);

            File.Delete(file);
        }

        [Fact]
        public void Delete_Test_Success()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<SocialMediaUserData> users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            bool ok = fileSystem.DeleteInFile(users[1]);
            users = fileSystem.Load().Cast<SocialMediaUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal(2, users.Count);
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal(SocialMediaEnum.Twitch, "Streamer", 123456789, 2, "spasm", users[1]);
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
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal(SocialMediaEnum.Twitter, "Tweeter", 123456789, 3, "chirp", users[1]);
            Equal(SocialMediaEnum.Twitch, "Streamer", 123456789, 2, "spasm", users[2]);
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
            Equal(SocialMediaEnum.YouTube, "YouTuber", 123456789, 1, "wecylinder", users[0]);
            Equal(SocialMediaEnum.Twitter, "Tweeter", 123456789, 3, "chirp", users[1]);
            Equal(SocialMediaEnum.Twitch, "Streamer", 123456789, 2, "spasm", users[2]);
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

        private void Equal(SocialMediaEnum expectedSocialMedia, string expectedName, ulong expectedGuildId,
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