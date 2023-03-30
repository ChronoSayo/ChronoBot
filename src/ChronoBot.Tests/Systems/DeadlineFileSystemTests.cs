using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ChronoBot.Tests.Systems
{
    public class DeadlineFileSystemTests
    {
        private readonly string _path;

        public DeadlineFileSystemTests()
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name);
        }

        [Fact]
        public void SetPath_Test_NoPath_Success()
        {
            var fileSystem = new DeadlineFileSystem();

            Assert.True(Directory.Exists(fileSystem.PathToSaveFile));

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }
        [Fact]
        public void SetPath_Test_WithPath_Success()
        {
            var fileSystem = new DeadlineFileSystem(Path.Combine(_path, "Set Path"));

            Assert.True(Directory.Exists(fileSystem.PathToSaveFile));

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }

        [Fact]
        public void Load_Test_SingleFile_Success()
        {
            var fileSystem = new DeadlineFileSystem(Path.Combine(_path, "Load"));

            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            Assert.True(File.Exists(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml")));
            Assert.Equal(5, users.Count);
            EqualUser1(users[0]);
            EqualUser2(users[1]);
        }
        [Fact]
        public void Load_Test_MultipleFiles_Success()
        {
            var fileSystem = new DeadlineFileSystem(Path.Combine(_path, "Load"));
            var file1 = Path.Combine(fileSystem.PathToSaveFile, "123456789.xml");
            var file2 = Path.Combine(fileSystem.PathToSaveFile, "987654321.xml");

            if (File.Exists(file2))
                File.Delete(file2);
            File.Copy(file1, file2);
            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            Assert.True(File.Exists(file1));
            Assert.True(File.Exists(file2));
            Assert.Equal(10, users.Count);
            EqualUser1(users[0]);
            EqualUser2(users[1]);
            EqualUser1(users[2], guildId: 987654321);
            EqualUser2(users[3], guildId: 987654321);

            File.Delete(file2);
        }
        [Fact]
        public void Load_Test_MultipleFilesWithOneWorking_Success()
        {
            var fileSystem = new DeadlineFileSystem(Path.Combine(_path, "Load"));
            var file1 = Path.Combine(fileSystem.PathToSaveFile, "123456789.xml");
            var file2 = Path.Combine(fileSystem.PathToSaveFile, "987654321.txt");

            if (File.Exists(file2))
                File.Delete(file2);
            File.Copy(file1, file2);
            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            Assert.True(File.Exists(file1));
            Assert.True(File.Exists(file2));
            Assert.Equal(5, users.Count);
            EqualUser1(users[0], guildId: 123456789);
            EqualUser2(users[1], guildId: 123456789);

            File.Delete(file2);
        }
        [Fact]
        public void Load_Test_Fail()
        {
            var fileSystem = new DeadlineFileSystem(Path.Combine(_path, "Load Fail"));

            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            Assert.True(File.Exists(Path.Combine(fileSystem.PathToSaveFile, "fail.xml")));
            Assert.Empty(users);
        }

        [Fact]
        public void SaveUpdate_Test_Success()
        {
            var tuple = SetUpSaveFileTests();
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<DeadlineUserData> saveUsers = new List<DeadlineUserData>()
            {
                new()
                {
                    Name = "TesterRemind1",
                    UserId = 690,
                    ChannelId = 41,
                    GuildId = 987654321,
                    Id = "Remind message 1"
                },

                new()
                {
                    Name = "TesterRemind2",
                    UserId = 42,
                    ChannelId = 41,
                    GuildId = 987654321,
                    Id = "Remind message 2"
                }
            };

            bool ok = false;
            foreach (DeadlineUserData data in saveUsers)
            {
                ok |= fileSystem.Save(data);
                ok |= fileSystem.UpdateFile(data);
            }
            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal(2, users.Count);
            EqualUser1(users[0], "TesterRemind1", 690, 41, 987654321);
            EqualUser1(users[1], "TesterRemind2", 42, 41, 987654321);
        }

        [Fact]
        public void Save_Test_Fail()
        {
            var tuple = SetUpSaveFileTests();
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            DeadlineUserData user = new DeadlineUserData();

            bool ok = fileSystem.Save(user);

            Assert.False(ok);
            Assert.False(File.Exists(file));
        }

        [Fact]
        public void Update_Test_UserDataNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Update");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            users[0].UserId = 1;
            bool ok = fileSystem.UpdateFile(users[0]);

            Assert.True(File.Exists(file));
            Assert.False(ok);

            File.Delete(file);
        }
        [Fact]
        public void Update_Test_FileNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Update");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            users[0].GuildId = 2;
            bool ok = fileSystem.UpdateFile(users[0]);

            Assert.True(File.Exists(file));
            Assert.False(ok);

            File.Delete(file);
        }

        [Fact]
        public void Delete_Test_Success()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            bool ok = fileSystem.DeleteInFile(users[0]);
            users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal(4, users.Count);
            EqualUser2(users[0]);
        }
        [Fact]
        public void Delete_Test_UserDataNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;

            bool ok = fileSystem.DeleteInFile(new DeadlineUserData { GuildId = 123456789, UserId = 1, Id = "Fail" });

            Assert.True(File.Exists(file));
            Assert.False(ok);
        }
        [Fact]
        public void Delete_Test_FileNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<DeadlineUserData> users = fileSystem.Load().Cast<DeadlineUserData>().ToList();

            users[0].GuildId = 16;
            bool ok = fileSystem.DeleteInFile(users[0]);

            Assert.True(File.Exists(file));
            Assert.False(ok);
        }

        private Tuple<DeadlineFileSystem, string> SetUpSaveFileTests()
        {
            var fileSystem =
                new DeadlineFileSystem(Path.Combine(_path, "Save"));
            string file = Path.Combine(fileSystem.PathToSaveFile, "987654321.xml");
            if (File.Exists(file))
                File.Delete(file);

            return new Tuple<DeadlineFileSystem, string>(fileSystem, file);
        }
        private Tuple<DeadlineFileSystem, string> SetUpCopyOfFileTests(string folderName)
        {
            var srcFile = Path.Combine(_path, "Load", "123456789.xml");
            var updatePath = Path.Combine(_path, folderName);
            if (Directory.Exists(updatePath))
                Directory.Delete(updatePath, true);
            var fileSystem = new DeadlineFileSystem(updatePath);
            File.Copy(srcFile, Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
            var file = Path.Combine(_path, folderName, "123456789.xml");

            return new Tuple<DeadlineFileSystem, string>(fileSystem, file);
        }

        private void EqualUser1(DeadlineUserData ud, string username = "Reminder1", ulong userId = 69,
            ulong channelId = 4, ulong guildId = 123456789, string id = "Remind message 1")
        {
            Assert.Equal(username.ToString(), ud.Name);
            Assert.Equal((double)userId, ud.UserId);
            Assert.Equal((double)channelId, ud.ChannelId);
            Assert.Equal((double)guildId, ud.GuildId);
        }

        private void EqualUser2(DeadlineUserData ud, string username = "Reminder2", ulong userId = 420,
            ulong channelId = 4, ulong guildId = 123456789, string id = "Remind message 2")
        {
            EqualUser1(ud, username, userId, channelId, guildId);
        }
    }
}
