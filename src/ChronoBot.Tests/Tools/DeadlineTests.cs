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
using Moq;
using Xunit;

namespace ChronoBot.Tests.Tools
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
            Assert.Equal((int)user.GuildId, 987654321);
            Assert.Equal((int)user.ChannelId, 134679);
            Assert.Equal(user.Name, "CountdownUser");
            Assert.Equal((double)user.UserId, 9001);
        }

        [Fact]
        public void GetCountdown_Test_Success()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 1,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.Equal("ok", result);
            Assert.Equal("Countdown1", embed.Author.Value.ToString());
            Assert.Equal("\"Countdown message 1\"", embed.Description);
            Assert.Equal("COUNTDOWN", embed.Title);
        }

        [Fact]
        public void Countdown_DaysLeft_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DaysLeft", 1);
            var tomorrow = DateTime.Now.AddDays(1);
            var user = deadline.SetDeadline("TestCountdown", tomorrow, 987654324, 134679,
                "CountdownUser", 9001, DeadlineEnum.Countdown);

            Thread.Sleep(1500);
            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser = users.Find(x => x.UserId == user.UserId && x.Id == user.Id && x.GuildId == user.GuildId);

            Assert.Equal(1, actualUser.DaysLeft);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654324.xml"));
        }

        [Fact]
        public void Countdown_CountingDown_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "CheckDaysLeft", 1);
            var overmorrow = DateTime.Now.AddDays(2);
            var user = deadline.SetDeadline("TestCountdown", overmorrow, 987654324, 134679,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            user.DaysLeft = 1;

            Thread.Sleep(1500);
            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser = users.Find(x => x.UserId == user.UserId && x.Id == user.Id && x.GuildId == user.GuildId);

            Assert.Equal(2, actualUser.DaysLeft);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654324.xml"));
        }

        [Fact]
        public void Countdown_CountedDown_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "CountedDown", 1);
            var yesterday = DateTime.Now.AddDays(-1);
            var user = deadline.SetDeadline("TestCountdown", yesterday, 987654324, 134679,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            user.DaysLeft = 1;

            Thread.Sleep(1500);
            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser = users.Find(x => x.UserId == user.UserId && x.Id == user.Id && x.GuildId == user.GuildId);

            Assert.Equal(null, actualUser);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654324.xml"));
        }

        [Fact]
        public void Countdown_ReminderUser_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "ReminderInCountdown", 1);
            var tomorrow = DateTime.Now.AddDays(1);
            var reminderUser = deadline.SetDeadline("TestCountdown", tomorrow, 987654324, 134679,
                "CountdownUser", 9001, DeadlineEnum.Reminder);
            var countdownUser = deadline.SetDeadline("TestCountdown", tomorrow, 987654324, 134679,
                "CountdownUser", 9001, DeadlineEnum.Countdown);

            Thread.Sleep(1500);

            Assert.Equal(DeadlineEnum.Reminder, reminderUser.DeadlineType);
            Assert.Equal(DeadlineEnum.Countdown, countdownUser.DeadlineType);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654324.xml"));
        }

        [Fact]
        public void GetCountdown_NoneFound_Fail()
        {
            var deadline = LoadCountdown(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, 5, 69, 1,
                "Countdown1", "ChannelName", DeadlineEnum.Countdown, out Embed embed);

            Assert.Equal("Nothing found in ChannelName.", result);
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
            var deadline = CreateNewCountdown(out var fileSystem, "List");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestCountdown List 1", tomorrow, 456, 1,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown2 = deadline.SetDeadline("TestCountdown List 2", tomorrow, 456, 1,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var countdown3 = deadline.SetDeadline("TestCountdown List 3", tomorrow, 456, 1,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var resultList = deadline.ListDeadlines(456, 1, 9001,
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

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "456.xml"));
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

            Assert.Equal(result, "Countdown has been deleted.");

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

            Assert.Equal("Entry number 2 not found.", result);

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

            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser1 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 1" && x.GuildId == 666);
            var actualUser2 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 2" && x.GuildId == 666);
            var actualUser3 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 3" && x.GuildId == 646);
            var actualUser4 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 4" && x.GuildId == 646);

            Assert.Equal("All countdowns have been deleted from ChannelName.", result);
            Assert.Equal(2, users.Count);
            Assert.Equal(countdown1.Id, actualUser1.Id);
            Assert.Equal(countdown2.Id, actualUser2.Id);
            Assert.Equal(null, actualUser3);
            Assert.Equal(null, actualUser4);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "646.xml"));
        }

        [Fact]
        public void DeleteCountdown_NotFoundAnyChannels_Fail()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteNotFoundAnyChannels");
            string result = deadline.DeleteAllInChannelDeadline(666, 777, 9001, "ChannelName", DeadlineEnum.Countdown);

            Assert.Equal("Nothing found in ChannelName.", result);

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
            var actualUser1 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 1" && x.GuildId == 666);
            var actualUser2 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 2" && x.GuildId == 666);
            var actualUser3 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 3" && x.GuildId == 666);
            var actualUser4 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 4" && x.GuildId == 666);
            var actualUser5 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 5" && x.GuildId == 646);
            var actualUser6 = users.Find(x => x.UserId == 9001 && x.Id == "TestCountdown List 6" && x.GuildId == 646);

            Assert.Equal("All countdowns have been deleted from GuildName.", result);
            Assert.Equal(2, users.Count);
            Assert.Equal(null, actualUser1);
            Assert.Equal(null, actualUser2);
            Assert.Equal(null, actualUser3);
            Assert.Equal(null, actualUser4);
            Assert.Equal(countdown5.Id, actualUser5.Id);
            Assert.Equal(countdown6.Id, actualUser6.Id);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "646.xml"));
        }

        [Fact]
        public void DeleteCountdown_NotFoundInGuild_Fail()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "DeleteNotFoundInGuild");
            string result = deadline.DeleteAllInGuildDeadline(666, 9001, "GuildName", DeadlineEnum.Countdown);

            Assert.Equal("Nothing found in GuildName.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        private Countdown CreateNewCountdown(out DeadlineFileSystem fileSystem, string folderName = "New", int seconds = 60)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new DeadlineFileSystem(path);

            return new Countdown(_mockClient.Object, fileSystem, new List<DeadlineUserData>(), seconds);
        }

        private Countdown LoadCountdown(out DeadlineFileSystem fileSystem, int seconds = 60)
        {
            fileSystem = new DeadlineFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Countdown(_mockClient.Object, fileSystem, new List<DeadlineUserData>(), seconds);
        }

        [Fact]
        public void SetReminder_Test_Success()
        {
            var deadline = CreateNewReminder(out _);
            var tomorrow = DateTime.Now.AddDays(1);
            var user = deadline.SetDeadline("TestReminder", tomorrow, 741852963, 147258,
                "ReminderUser", 9001, DeadlineEnum.Reminder);

            Assert.Equal("TestReminder", user.Id);
            Assert.Equal(tomorrow, user.Deadline);
            Assert.Equal(741852963, (int)user.GuildId);
            Assert.Equal(147258, (int) user.ChannelId);
            Assert.Equal("ReminderUser", user.Name);
            Assert.Equal(9001, (int) user.UserId);
        }

        [Fact]
        public void GetReminder_Test_Success()
        {
            var deadline = LoadReminder(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 1,
                "Reminder1", "ChannelName", DeadlineEnum.Reminder, out Embed embed);

            Assert.Equal("ok", result);
            Assert.Equal("Reminder1", embed.Author.Value.ToString());
            Assert.Equal("\"Remind message 1\"", embed.Description);
            Assert.Equal("REMINDER", embed.Title);
        }
        [Fact]
        public void Reminder_NotReminded_Success()
        {
            var deadline = CreateNewReminder(out var fileSystem, "NotReminded", 1);
            var tomorrow = DateTime.Now.AddDays(1);
            var user = deadline.SetDeadline("TestReminder", tomorrow, 987654324, 134679,
                "ReminderUser", 9001, DeadlineEnum.Reminder);

            Thread.Sleep(1500);
            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser = users.Find(x => x.UserId == user.UserId && x.Id == user.Id && x.GuildId == user.GuildId);

            Assert.Equal((ulong)987654324, actualUser.GuildId);
            Assert.Equal((ulong)134679, actualUser.ChannelId);
            Assert.Equal((ulong)9001, actualUser.UserId);
            Assert.Equal("TestReminder", actualUser.Id);
            Assert.Equal("ReminderUser", actualUser.Name);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654324.xml"));
        }

        [Fact]
        public void Reminder_Reminded_Success()
        {
            var deadline = CreateNewReminder(out var fileSystem, "Reminded", 1);
            var now = DateTime.Now;
            var user = deadline.SetDeadline("TestReminder", now, 987654324, 134679,
                "ReminderUser", 9001, DeadlineEnum.Reminder);

            Thread.Sleep(1500);
            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser = users.Find(x => x.UserId == user.UserId && x.Id == user.Id && x.GuildId == user.GuildId);

            Assert.Equal(null, actualUser);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654324.xml"));
        }

        [Fact]
        public void Reminder_CountdownUser_Success()
        {
            var deadline = CreateNewCountdown(out var fileSystem, "CountdownInReminder", 1);
            var tomorrow = DateTime.Now.AddDays(1);
            var countdownUser = deadline.SetDeadline("TestReminder", tomorrow, 987654324, 134679,
                "CountdownUser", 9001, DeadlineEnum.Countdown);
            var reminderUser = deadline.SetDeadline("TestReminder", tomorrow, 987654324, 134679,
                "ReminderUser", 9001, DeadlineEnum.Reminder);

            Thread.Sleep(1500);

            Assert.Equal(DeadlineEnum.Reminder, reminderUser.DeadlineType);
            Assert.Equal(DeadlineEnum.Countdown, countdownUser.DeadlineType);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "987654324.xml"));
        }

        [Fact]
        public void GetReminder_NoneFound_Fail()
        {
            var deadline = LoadReminder(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, 5, 69, 1,
                "Reminder1", "ChannelName", DeadlineEnum.Reminder, out Embed embed);

            Assert.Equal("Nothing found in ChannelName.", result);
        }

        [Fact]
        public void GetReminder_ChosenNotFound_Fail()
        {
            var deadline = LoadReminder(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 10,
                "Reminder1", "ChannelName", DeadlineEnum.Reminder, out Embed embed);

            Assert.Equal(result, "Entry number 10 not found.");
        }

        [Fact]
        public void ListReminder_Test_Success()
        {
            var deadline = CreateNewReminder(out var fileSystem, "List");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestReminder List 1", tomorrow, 456, 1,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown2 = deadline.SetDeadline("TestReminder List 2", tomorrow, 456, 1,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown3 = deadline.SetDeadline("TestReminder List 3", tomorrow, 456, 1,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var resultList = deadline.ListDeadlines(456, 1, 9001,
                "ReminderUser", "ChannelName", DeadlineEnum.Reminder, out Embed embed);

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
            Assert.Equal(embed.Author.Value.ToString(), "ReminderUser");
            Assert.Equal(embed.Title, "REMINDER");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "456.xml"));
        }

        [Fact]
        public void ListReminder_NoneFound_Fail()
        {
            var deadline = LoadReminder(out _);
            var result = deadline.ListDeadlines(DefaultGuildId, DefaultChannelId, 8,
                "None", "ChannelName", DeadlineEnum.Reminder, out Embed embed);

            Assert.Equal(result, "Nothing found in ChannelName.");
        }

        [Fact]
        public void DeleteReminder_Test_Success()
        {
            var deadline = CreateNewReminder(out var fileSystem, "Delete");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestReminder List 1", tomorrow, 666, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown2 = deadline.SetDeadline("TestReminder List 1", tomorrow, 666, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            string result = deadline.DeleteDeadline(666, 777, 9001, 2, "ChannelName", DeadlineEnum.Reminder);

            Assert.Equal(result, "Reminder has been deleted.");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteReminder_NotFound_Fail()
        {
            var deadline = CreateNewReminder(out var fileSystem, "DeleteNotFound");
            string result = deadline.DeleteDeadline(666, 777, 9001, 2, "ChannelName", DeadlineEnum.Reminder);

            Assert.Equal(result, "Nothing found in ChannelName.");

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteReminder_ChosenNotFound_Fail()
        {
            var deadline = CreateNewReminder(out var fileSystem, "DeleteNotFound");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestReminder List 1", tomorrow, 666, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            string result = deadline.DeleteDeadline(666, 777, 9001, 2, "ChannelName", DeadlineEnum.Reminder);

            Assert.Equal("Entry number 2 not found.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteReminder_AllInChannel_Success()
        {
            var deadline = CreateNewReminder(out var fileSystem, "DeleteInChannel");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestReminder List 1", tomorrow, 666, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown2 = deadline.SetDeadline("TestReminder List 2", tomorrow, 666, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown3 = deadline.SetDeadline("TestReminder List 3", tomorrow, 646, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown4 = deadline.SetDeadline("TestReminder List 4", tomorrow, 646, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            string result = deadline.DeleteAllInChannelDeadline(646, 777, 9001, "ChannelName", DeadlineEnum.Reminder);

            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser1 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 1" && x.GuildId == 666);
            var actualUser2 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 2" && x.GuildId == 666);
            var actualUser3 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 3" && x.GuildId == 646);
            var actualUser4 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 4" && x.GuildId == 646);

            Assert.Equal("All reminders have been deleted from ChannelName.", result);
            Assert.Equal(2, users.Count);
            Assert.Equal(countdown1.Id, actualUser1.Id);
            Assert.Equal(countdown2.Id, actualUser2.Id);
            Assert.Equal(null, actualUser3);
            Assert.Equal(null, actualUser4);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "646.xml"));
        }

        [Fact]
        public void DeleteReminder_NotFoundAnyChannels_Fail()
        {
            var deadline = CreateNewReminder(out var fileSystem, "DeleteNotFoundAnyChannels");
            string result = deadline.DeleteAllInChannelDeadline(666, 777, 9001, "ChannelName", DeadlineEnum.Reminder);

            Assert.Equal("Nothing found in ChannelName.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        [Fact]
        public void DeleteReminder_AllInGuild_Success()
        {
            var deadline = CreateNewReminder(out var fileSystem, "DeleteInGuild");
            DateTime tomorrow = DateTime.Now.AddDays(1);
            var countdown1 = deadline.SetDeadline("TestReminder List 1", tomorrow, 666, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown2 = deadline.SetDeadline("TestReminder List 2", tomorrow, 666, 777,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown3 = deadline.SetDeadline("TestReminder List 3", tomorrow, 666, 767,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown4 = deadline.SetDeadline("TestReminder List 4", tomorrow, 666, 767,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown5 = deadline.SetDeadline("TestReminder List 5", tomorrow, 646, 767,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            var countdown6 = deadline.SetDeadline("TestReminder List 6", tomorrow, 646, 767,
                "ReminderUser", 9001, DeadlineEnum.Reminder);
            string result = deadline.DeleteAllInGuildDeadline(666, 9001, "GuildName", DeadlineEnum.Reminder);

            var users = (List<DeadlineUserData>)fileSystem.Load();
            var actualUser1 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 1" && x.GuildId == 666);
            var actualUser2 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 2" && x.GuildId == 666);
            var actualUser3 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 3" && x.GuildId == 666);
            var actualUser4 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 4" && x.GuildId == 666);
            var actualUser5 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 5" && x.GuildId == 646);
            var actualUser6 = users.Find(x => x.UserId == 9001 && x.Id == "TestReminder List 6" && x.GuildId == 646);

            Assert.Equal("All reminders have been deleted from GuildName.", result);
            Assert.Equal(2, users.Count);
            Assert.Equal(null, actualUser1);
            Assert.Equal(null, actualUser2);
            Assert.Equal(null, actualUser3);
            Assert.Equal(null, actualUser4);
            Assert.Equal(countdown5.Id, actualUser5.Id);
            Assert.Equal(countdown6.Id, actualUser6.Id);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "646.xml"));
        }

        [Fact]
        public void DeleteReminder_NotFoundInGuild_Fail()
        {
            var deadline = CreateNewReminder(out var fileSystem, "DeleteNotFoundInGuild");
            string result = deadline.DeleteAllInGuildDeadline(666, 9001, "GuildName", DeadlineEnum.Reminder);

            Assert.Equal("Nothing found in GuildName.", result);

            File.Delete(Path.Combine(fileSystem.PathToSaveFile, "666.xml"));
        }

        private Reminder CreateNewReminder(out DeadlineFileSystem fileSystem, string folderName = "New", int seconds = 60)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new DeadlineFileSystem(path);

            return new Reminder(_mockClient.Object, fileSystem, new List<DeadlineUserData>(), seconds);
        }

        private Reminder LoadReminder(out DeadlineFileSystem fileSystem, int seconds = 60)
        {
            fileSystem = new DeadlineFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Reminder(_mockClient.Object, fileSystem, new List<DeadlineUserData>(), seconds);
        }
    }
}
