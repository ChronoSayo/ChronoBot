using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

            Assert.True(user.Id == "TestCountdown");
            Assert.True(user.Deadline == tomorrow);
            Assert.True(user.GuildId == 987654321);
            Assert.True(user.ChannelId == 134679);
            Assert.True(user.Name == "CountdownUser");
            Assert.True(user.UserId == 9001);
        }

        [Fact]
        public void GetCountdown_Test_Success()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 1,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.True(result == "ok");
            Assert.True(embed.Author.Value.ToString() == "Countdown1");
            Assert.True(embed.Description == "\"Countdown message 1\"");
            Assert.True(embed.Title == "COUNTDOWN");
        }

        [Fact]
        public void GetCountdown_NoneFound_Fail()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, 5, 69, 1,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.True(result == "Nothing found in ChannelName.");
        }

        [Fact]
        public void GetCountdown_ChosenNotFound_Fail()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 10,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.True(result == "Entry number 10 not found.");
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

            Assert.True(resultList == "ok");
            Assert.NotNull(countdown1);
            Assert.NotNull(countdown2);
            Assert.NotNull(countdown3);
            Assert.True(countdown1.Deadline == tomorrow);
            Assert.True(countdown2.Deadline == tomorrow);
            Assert.True(countdown3.Deadline == tomorrow);
            Assert.True(embed.Description.Contains("1."));
            Assert.True(embed.Description.Contains("2."));
            Assert.True(embed.Description.Contains("3."));
            Assert.True(embed.Author.Value.ToString() == "CountdownUser");
            Assert.True(embed.Title == "COUNTDOWN");
        }

        [Fact]
        public void ListCountdown_NoneFound_Fail()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.ListDeadlines(DefaultGuildId, DefaultChannelId, 8,
                "None", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.True(result == "Nothing found in ChannelName.");
        }

        [Fact]
        public void DeleteCountdown_Test_Success()
        {
            var deadline = CreateNewCountdown(out _, "Delete");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown2 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 666, 777,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            string result = deadline.DeleteDeadline(666, 777, 9001, 2, "ChannelName", DeadlineEnum.Countdown);
            
            Assert.True(result == "ok");
            Assert.NotNull(countdown1);
            Assert.Null(countdown2);
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
