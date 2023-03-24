using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord.WebSocket;
using Moq;
using Xunit;

namespace ChronoBot.Tests.Tools.Deadlines
{
    public class CountdownTests
    {
        private const ulong DefaultGuildId = 234567891;

        public CountdownTests()
        {
        }

        [Fact]
        public void Countdown_SetCountdown_Success()
        {
            //ulong guildId = 789;
            //var entry = CreateCountdownEntry(guildId);
            //var countdown = entry.Item1;
            //var fileSystem = entry.Item2;
            //var tomorrow = DateTime.Now.AddDays(1);

            //countdown.SetDeadline("TestCountdown", tomorrow, guildId,
            //guildId, "Test", 420);

            //var users = (List<DeadlineUserData>)fileSystem.Load();
            //var user = users.Find(x => x.Id == "TestCountdown");

            //Assert.True(user is { Name: "Test" });
            //Assert.True(user.Deadline.Day == tomorrow.Day);
            //Assert.True(user.GuildId == guildId);
            //Assert.True(user.ChannelId == guildId);
            //Assert.True(user.Id == "TestCountdown");
            //Assert.True(user.UserId == 420);
        }

        [Fact]
        public void Countdown_CountingDown_Success()
        {
            //ulong guildId = 456;
            //var entry = CreateCountdownEntry(guildId);
            //var countdown = entry.Item1;
            //var future = DateTime.Now.AddDays(5);
            //var user = countdown.SetDeadline("TestCountingDown",
            //    future,
            //    guildId,
            //    guildId,
            //    "CountingDown",
            //    69);

            //Thread.Sleep(2000);

            //Assert.True(user.Name == "CountingDown");
            //Assert.True(user.Deadline.Day == future.Day);
            //Assert.True(user.Deadline.Hour == future.Hour);
            //Assert.True(user.Deadline.Minute == future.Minute);
            //Assert.True(user.GuildId == guildId);
            //Assert.True(user.ChannelId == guildId);
            //Assert.Contains("€", user.Id);
            //Assert.True(user.UserId == 69);
        }

        [Fact]
        public void Countdown_CountedDown_Success()
        {
            //ulong guildId = 123;
            //var entry = CreateCountdownEntry(guildId);
            //var countdown = entry.Item1;
            //var fileSystem = entry.Item2;
            //countdown.SetDeadline("TestCountedDown",
            //    DateTime.Now.AddSeconds(1),
            //    DefaultGuildId,
            //    guildId,
            //    "CountedDown",
            //    69);

            //Thread.Sleep(1500);

            //var users = (List<DeadlineUserData>)fileSystem.Load();
            //var user = users.Find(x => x.Id == "TestCountedDown");

            //Assert.Null(user);
        }

        private Tuple<Countdown, DeadlineFileSystem> CreateCountdownEntry(ulong guildId)
        {
            var mockClient = new Mock<DiscordSocketClient>(MockBehavior.Loose);
            var fileSystem = new DeadlineFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name));
            var countdown = new Countdown(mockClient.Object, fileSystem, new List<DeadlineUserData>());
            if (!File.Exists(Path.Join(fileSystem.PathToSaveFile, GetType().Name + guildId + ".xml")))
                return new Tuple<Countdown, DeadlineFileSystem>(countdown, fileSystem);

            File.Delete(Path.Join(fileSystem.PathToSaveFile, GetType().Name + guildId + ".xml"));
            fileSystem = new DeadlineFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name));
            countdown = new Countdown(mockClient.Object, fileSystem, new List<DeadlineUserData>());

            return new Tuple<Countdown, DeadlineFileSystem>(countdown, fileSystem);
        }
    }
}
