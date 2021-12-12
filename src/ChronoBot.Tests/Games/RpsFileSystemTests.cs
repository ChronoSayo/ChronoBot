using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using Xunit;

namespace ChronoBot.Tests.Games
{
    public class RpsFileSystemTests
    {
        private readonly string _path;

        public RpsFileSystemTests()
        {
            _path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name);
        }

        [Fact]
        public void SetPath_Test_NoPath_Success()
        {
            var fileSystem = new RpsFileSystem();

            Assert.True(Directory.Exists(fileSystem.PathToSaveFile));

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }
        [Fact]
        public void SetPath_Test_WithPath_Success()
        {
            var fileSystem = new RpsFileSystem(Path.Combine(_path, "Set Path"));

            Assert.True(Directory.Exists(fileSystem.PathToSaveFile));

            Directory.Delete(fileSystem.PathToSaveFile, true);
        }

        [Fact]
        public void Load_Test_SingleFile_Success()
        {
            var fileSystem = new RpsFileSystem(Path.Combine(_path, "Load"));

            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

            Assert.True(File.Exists(Path.Combine(fileSystem.PathToSaveFile, "123456789.xml")));
            Assert.Equal(2, users.Count);
            EqualUser1(users[0]);
            EqualUser2(users[1]);
        }
        [Fact]
        public void Load_Test_MultipleFiles_Success()
        {
            var fileSystem = new RpsFileSystem(Path.Combine(_path, "Load"));
            var file1 = Path.Combine(fileSystem.PathToSaveFile, "123456789.xml");
            var file2 = Path.Combine(fileSystem.PathToSaveFile, "987654321.xml");

            if (File.Exists(file2))
                File.Delete(file2);
            File.Copy(file1, file2);
            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

            Assert.True(File.Exists(file1));
            Assert.True(File.Exists(file2));
            Assert.Equal(4, users.Count);
            EqualUser1(users[0]);
            EqualUser2(users[1]);
            EqualUser1(users[2], guildId: 987654321);
            EqualUser2(users[3], guildId: 987654321);

            File.Delete(file2);
        }
        [Fact]
        public void Load_Test_MultipleFilesWithOneWorking_Success()
        {
            var fileSystem = new RpsFileSystem(Path.Combine(_path, "Load"));
            var file1 = Path.Combine(fileSystem.PathToSaveFile, "123456789.xml");
            var file2 = Path.Combine(fileSystem.PathToSaveFile, "987654321.txt");

            if (File.Exists(file2))
                File.Delete(file2);
            File.Copy(file1, file2);
            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

            Assert.True(File.Exists(file1));
            Assert.True(File.Exists(file2));
            Assert.Equal(2, users.Count);
            EqualUser1(users[0], guildId: 123456789);
            EqualUser2(users[1], guildId: 123456789);

            File.Delete(file2);
        }
        [Fact]
        public void Load_Test_Fail()
        {
            var fileSystem = new RpsFileSystem(Path.Combine(_path, "Load Fail"));

            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

            Assert.True(File.Exists(Path.Combine(fileSystem.PathToSaveFile, "fail.xml")));
            Assert.Empty(users);
        }

        [Fact]
        public void SaveUpdate_Test_Success()
        {
            var tuple = SetUpSaveFileTests();
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<RpsUserData> saveUsers = new List<RpsUserData>()
            {
                new RpsUserData
                {
                    UserId = 690,
                    ChannelId = 41,
                    GuildId = 987654321,
                    Plays = 1,
                    TotalPlays = 2,
                    Wins = 3,
                    Losses = 4,
                    Draws = 5,
                    Ratio = 6,
                    BestStreak = 7,
                    Coins = 8,
                    CurrentStreak = 9,
                    RockChosen = 10,
                    PaperChosen = 11,
                    ScissorsChosen = 12,
                    Resets = 13,
                    UserIdVs = 14,
                    Actor = RpsActors.Rock
                },

                new RpsUserData
                {
                    UserId = 42,
                    ChannelId = 41,
                    GuildId = 987654321,
                    Plays = 1,
                    TotalPlays = 2,
                    Wins = 3,
                    Losses = 4,
                    Draws = 5,
                    Ratio = 6,
                    BestStreak = 7,
                    Coins = 8,
                    CurrentStreak = 9,
                    RockChosen = 10,
                    PaperChosen = 11,
                    ScissorsChosen = 12,
                    Resets = 13,
                    UserIdVs = 14,
                    Actor = RpsActors.Scissors
                }
            };

            bool ok = false;
            foreach (RpsUserData data in saveUsers)
            {
                ok |= fileSystem.Save(data);
                ok |= fileSystem.UpdateFile(data);
            }
            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Equal(2, users.Count);
            EqualUser1(users[0], 690, 41, 987654321, 1, 2, 3, 4, 5, 6, 13, 10, 11, 12, 9, 7, 8, 14, "Rock");
            EqualUser1(users[1], 42, 41, 987654321, 1, 2, 3, 4, 5, 6, 13, 10, 11, 12, 9, 7, 8, 14, "Scissors");
        }

        [Fact]
        public void Save_Test_Fail()
        {
            var tuple = SetUpSaveFileTests();
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            RpsUserData user = new RpsUserData();

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
            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

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
            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

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
            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

            bool ok = fileSystem.DeleteInFile(users[0]);
            users = fileSystem.Load().Cast<RpsUserData>().ToList();

            Assert.True(File.Exists(file));
            Assert.True(ok);
            Assert.Single(users);
            EqualUser2(users[0]);
        }
        [Fact]
        public void Delete_Test_UserDataNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;

            bool ok = fileSystem.DeleteInFile(new RpsUserData { GuildId = 123456789, UserId = 1 });

            Assert.True(File.Exists(file));
            Assert.False(ok);
        }
        [Fact]
        public void Delete_Test_FileNotFound_Fail()
        {
            var tuple = SetUpCopyOfFileTests("Delete");
            var fileSystem = tuple.Item1;
            var file = tuple.Item2;
            List<RpsUserData> users = fileSystem.Load().Cast<RpsUserData>().ToList();

            users[0].GuildId = 16;
            bool ok = fileSystem.DeleteInFile(users[0]);

            Assert.True(File.Exists(file));
            Assert.False(ok);
        }

        private Tuple<RpsFileSystem, string> SetUpSaveFileTests()
        {
            var fileSystem =
                new RpsFileSystem(Path.Combine(_path, "Save"));
            string file = Path.Combine(fileSystem.PathToSaveFile, "987654321.xml");
            if (File.Exists(file))
                File.Delete(file);

            return new Tuple<RpsFileSystem, string>(fileSystem, file);
        }
        private Tuple<RpsFileSystem, string> SetUpCopyOfFileTests(string folderName)
        {
            var srcFile = Path.Combine(_path, "Load", "123456789.xml");
            var updatePath = Path.Combine(_path, folderName);
            if (Directory.Exists(updatePath))
                Directory.Delete(updatePath, true);
            var fileSystem = new RpsFileSystem(updatePath);
            File.Copy(srcFile, Path.Combine(fileSystem.PathToSaveFile, "123456789.xml"));
            var file = Path.Combine(_path, folderName, "123456789.xml");

            return new Tuple<RpsFileSystem, string>(fileSystem, file);
        }

        private void EqualUser1(RpsUserData ud, ulong userId = 69, ulong channelId = 4, ulong guildId = 123456789, 
            int plays = 10, int totalPlays = 10, int wins = 5, int losses = 4, int draws = 1, int ratio = 50, 
            int resets = 0, int rockChosen = 2, int paperChosen = 5, int scissorsChosen = 3, int currentStreak = 0, 
            int bestStreak = 2, int coins = 1, ulong userIdVs = 420, string actor = "Rock")
        {
            Assert.Equal((double)userId, ud.UserId);
            Assert.Equal((double)channelId, ud.ChannelId);
            Assert.Equal((double)guildId, ud.GuildId);
            Assert.Equal((double)userIdVs, ud.UserIdVs);
            Assert.Equal(actor, ud.Actor.ToString());
            Assert.Equal(plays, ud.Plays);
            Assert.Equal(totalPlays, ud.TotalPlays);
            Assert.Equal(wins, ud.Wins);
            Assert.Equal(losses, ud.Losses);
            Assert.Equal(draws, ud.Draws);
            Assert.Equal(ratio, ud.Ratio);
            Assert.Equal(currentStreak, ud.CurrentStreak);
            Assert.Equal(bestStreak, ud.BestStreak);
            Assert.Equal(resets, ud.Resets);
            Assert.Equal(rockChosen, ud.RockChosen);
            Assert.Equal(paperChosen, ud.PaperChosen);
            Assert.Equal(scissorsChosen, ud.ScissorsChosen);
            Assert.Equal(coins, ud.Coins);
        }

        private void EqualUser2(RpsUserData ud, ulong userId = 420, ulong channelId = 4, ulong guildId = 123456789,
            int plays = 100, int totalPlays = 15, int wins = 55, int losses = 44, int draws = 11, int ratio = 10,
            int resets = 2, int rockChosen = 22, int paperChosen = 55, int scissorsChosen = 33, int currentStreak = 44,
            int bestStreak = 25, int coins = 15, ulong userIdVs = 69, string actor = "Paper")
        {
            Assert.Equal((double)userId, ud.UserId);
            Assert.Equal((double)channelId, ud.ChannelId);
            Assert.Equal((double)guildId, ud.GuildId);
            Assert.Equal((double)userIdVs, ud.UserIdVs);
            Assert.Equal(actor, ud.Actor.ToString());
            Assert.Equal(plays, ud.Plays);
            Assert.Equal(totalPlays, ud.TotalPlays);
            Assert.Equal(wins, ud.Wins);
            Assert.Equal(losses, ud.Losses);
            Assert.Equal(draws, ud.Draws);
            Assert.Equal(ratio, ud.Ratio);
            Assert.Equal(currentStreak, ud.CurrentStreak);
            Assert.Equal(bestStreak, ud.BestStreak);
            Assert.Equal(resets, ud.Resets);
            Assert.Equal(rockChosen, ud.RockChosen);
            Assert.Equal(paperChosen, ud.PaperChosen);
            Assert.Equal(scissorsChosen, ud.ScissorsChosen);
            Assert.Equal(coins, ud.Coins);
        }
    }
}