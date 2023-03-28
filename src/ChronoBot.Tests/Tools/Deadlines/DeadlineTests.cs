using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord;
using Discord.WebSocket;
using Google.Apis.YouTube.v3.Data;
using Moq;
using Xunit;

namespace ChronoBot.Tests.Tools.Deadlines
{
    public class DeadlineTests
    {
        private readonly Mock<DiscordSocketClient> _mockClient;
        private const ulong DefaultGuildId = 123456789;
        private const ulong DefaultChannelId = 4;

        public DeadlineTests()
        {
            _mockClient = new Mock<DiscordSocketClient>(MockBehavior.Loose);
        }

        [Fact]
        public void SetCountdown_Test_Success()
        {
            var deadline = CreateNewCountdown(out _);
            var tomorrow = DateTime.Now.AddDays(1);
            var user = deadline.SetDeadline("TestCountdown", tomorrow, 987654321, 134679,
                "CountdownUser", 9001, DeadlineEnum.Countdown);

            Assert.Equal(user.Id, "TestCountdown");
            Assert.Equal(user.Deadline, tomorrow);
            Assert.Equal((int) user.GuildId, 987654321);
            Assert.Equal((int) user.ChannelId, 134679);
            Assert.Equal(user.Name, "CountdownUser");
            Assert.Equal((double)user.UserId, 9001);
        }

        [Fact]
        public void GetCountdown_Test_Success()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 1,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.Equal(result, "ok");
            Assert.Equal(embed.Author.Value.ToString(), "Countdown1");
            Assert.Equal(embed.Description, "\"Countdown message 1\"");
            Assert.Equal(embed.Title, "COUNTDOWN");
        }

        [Fact]
        public void Countdown_DaysLeft_Success()
        {
            var deadline = CreateNewCountdown(out _);
            var tomorrow = DateTime.Now.AddDays(1);
            var user = deadline.SetDeadline("TestCountdown", tomorrow, 987654324, 134679,
                "CountdownUser", 9001, DeadlineEnum.Countdown);

            Thread.Sleep(3000);

            Assert.Equal(user.DaysLeft, 1);
        }

        [Fact]
        public void GetCountdown_NoneFound_Fail()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, 5, 69, 1,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.Equal(result, "Nothing found in ChannelName.");
        }

        [Fact]
        public void GetCountdown_ChosenNotFound_Fail()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 10,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.Equal(result, "Entry number 10 not found.");
        }

        [Fact]
        public void ListCountdown_Test_Success()
        {
            var deadline = CreateNewCountdown(out _, "List");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 456, 1,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown2 = deadline.SetDeadline("TestCountdown List 2", tomorrow, 456, 1,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown3 = deadline.SetDeadline("TestCountdown List 3", tomorrow, 456, 1,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var resultList = deadline.ListDeadlines(456,  1, 9001,
                "CountdownUser", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.Equal(resultList, "ok");
            Assert.NotNull(countdown1);
            Assert.NotNull(countdown2);
            Assert.NotNull(countdown3);
            Assert.Equal(countdown1.Deadline, tomorrow);
            Assert.Equal(countdown2.Deadline, tomorrow);
            Assert.True(countdown3.Deadline == tomorrow);
            Assert.True(embed.Description.Contains("1."));
            Assert.True(embed.Description.Contains("2."));
            Assert.True(embed.Description.Contains("3."));
            Assert.Equal(embed.Author.Value.ToString(), "CountdownUser");
            Assert.Equal(embed.Title, "COUNTDOWN");
        }

        [Fact]
        public void ListCountdown_NoneFound_Fail()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.ListDeadlines(DefaultGuildId, DefaultChannelId, 8,
                "None", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.Equal(result, "Nothing found in ChannelName.");
        }

        [Fact]
        public void DeleteCountdown_Test_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "Delete");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown2 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            string result = deadline.DeleteDeadline(666, 777, 9001, 2, "ChannelName", DeadlineEnum.Countdown);
            
            Assert.Equal(result, "ok");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteCountdown_NotFound_Fail()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteNotFound");
            string result = deadline.DeleteDeadline(666, 777, 9001, 2, "ChannelName", DeadlineEnum.Countdown);

            Assert.Equal(result, "Nothing found in ChannelName.");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteCountdown_ChosenNotFound_Fail()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteNotFound");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            string result = deadline.DeleteDeadline(666, 777, 9001, 2, "ChannelName", DeadlineEnum.Countdown);

            Assert.Equal(result, "Entry number 2 not found.");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteCountdown_AllInChannel_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteInChannel");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown2 = deadline.SetDeadline("TestCountdown List 2", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown3 = deadline.SetDeadline("TestCountdown List 3", tomorrow, 646, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown4 = deadline.SetDeadline("TestCountdown List 4", tomorrow, 646, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            string result = deadline.DeleteAllInChannelDeadline(646, 777, 9001, "ChannelName", DeadlineEnum.Countdown);

            var users = (List<DeadlineUserData>) fileSystem.Load();
            var expectUser1 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 1" && x.GuildId == 666);
            var expectUser2 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 2" && x.GuildId == 666);
            var expectUser3 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 3" && x.GuildId == 646);
            var expectUser4 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 4" && x.GuildId == 646);

            Assert.Equal(result, "All countdowns have been deleted from ChannelName.");
            Assert.Equal(users.Count, 2);
            Assert.Equal(expectUser1.Id, countdown1.Id);
            Assert.Equal(expectUser2.Id, countdown2.Id);
            Assert.Equal(expectUser3, null);
            Assert.Equal(expectUser4, null);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "646.xml"));
        }

        [Fact]
        public void DeleteCountdown_NotFoundAnyChannels_Fail()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteNotFoundAnyChannels");
            string result = deadline.DeleteAllInChannelDeadline(666, 777, 9001,"ChannelName", DeadlineEnum.Countdown);

            Assert.Equal(result, "Nothing found in ChannelName.");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteCountdown_AllInGuild_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteInGuild");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown2 = deadline.SetDeadline("TestCountdown List 2", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown3 = deadline.SetDeadline("TestCountdown List 3", tomorrow, 666, 767,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown4 = deadline.SetDeadline("TestCountdown List 4", tomorrow, 666, 767,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown5 = deadline.SetDeadline("TestCountdown List 5", tomorrow, 646, 767,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown6 = deadline.SetDeadline("TestCountdown List 6", tomorrow, 646, 767,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            string result = deadline.DeleteAllInGuildDeadline(666, 9001, "GuildName", DeadlineEnum.Countdown);

            var users = (List<DeadlineUserData>)fileSystem.Load();
            var expectUser1 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 1" && x.GuildId == 666);
            var expectUser2 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 2" && x.GuildId == 666);
            var expectUser3 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 3" && x.GuildId == 666);
            var expectUser4 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 4" && x.GuildId == 666);
            var expectUser5 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 5" && x.GuildId == 646);
            var expectUser6 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 6" && x.GuildId == 646);

            Assert.Equal(result, "All countdowns have been deleted from GuildName.");
            Assert.Equal(users.Count, 2);
            Assert.Equal(expectUser1, null);
            Assert.Equal(expectUser2, null);
            Assert.Equal(expectUser3, null);
            Assert.Equal(expectUser4, null);
            Assert.Equal(expectUser5.Id, countdown5.Id);
            Assert.Equal(expectUser6.Id, countdown6.Id);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "646.xml"));
        }

        [Fact]
        public void DeleteCountdown_NotFoundInGuild_Fail()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteNotFoundInGuild");
            string result = deadline.DeleteAllInGuildDeadline(666, 9001, "GuildName", DeadlineEnum.Countdown);

            Assert.Equal(result, "Nothing found in GuildName.");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        private Countdown CreateNewCountdown(out DeadlineFileSystem fileSystem, string folderName = "New")
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new DeadlineFileSystem(path);

            return new Countdown(_mockClient.Object, fileSystem, new List<DeadlineUserData>());
        }

        private Countdown LoadCountdown(out DeadlineFileSystem fileSystem)
        {
            fileSystem = new DeadlineFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Countdown(_mockClient.Object, fileSystem, new List<DeadlineUserData>());
        }

        [Fact]
        public void SetReminder_Test_Success()
        {
            var deadline = CreateNewCountdown(out _);
            var tomorrow = DateTime.Now.AddDays(1);
            var user = deadline.SetDeadline("TestReminder", tomorrow, 741852963, 147258,
                "ReminderUser", 9001, DeadlineEnum.Reminder);

            Assert.True(user.Id == "TestReminder");
            Assert.True(user.Deadline == tomorrow);
            Assert.True(user.GuildId == 741852963);
            Assert.True(user.ChannelId == 147258);
            Assert.True(user.Name == "ReminderUser");
            Assert.True(user.UserId == 9001);
        }

        [Fact]
        public void GetReminder_Test_Success()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 1,
                "Reminder1", "ChannelName", DeadlineEnum.Reminder, out Embed embed);

            Assert.True(result == "ok");
            Assert.True(embed.Author.Value.ToString() == "Reminder1");
            Assert.True(embed.Description == "\"Remind message 1\"");
            Assert.True(embed.Title == "REMINDER");
        }

        private Reminder CreateNewReminder(out DeadlineFileSystem fileSystem, string folderName = "New")
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new DeadlineFileSystem(path);

            return new Reminder(_mockClient.Object, fileSystem, new List<DeadlineUserData>());
        }

        private Reminder LoadReminder(out DeadlineFileSystem fileSystem)
        {
            fileSystem = new DeadlineFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Reminder(_mockClient.Object, fileSystem, new List<DeadlineUserData>());
        }
    }
}
