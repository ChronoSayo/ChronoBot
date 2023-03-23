using System;
using System.Collections.Generic;
using System.IO;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Tests.Fakes;
using ChronoBot.Utilities.SocialMedias;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord;
using Discord.WebSocket;
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
        public void Deadline_SetCountdown_Success()
        {
            var deadline = LoadDeadline(out _);
            var tomorrow = DateTime.Now.AddDays(1);
            var user = deadline.SetDeadline("TestDeadline", tomorrow, DefaultGuildId, DefaultGuildId,
                "DeadlineUser", 9001);
            
            Assert.Null(user);
        }

        [Fact]
        public void Deadline_GetDeadline_Countdown_Success()
        {
            var deadline = LoadDeadline(out _);
            var result = deadline.GetDeadlines(DefaultGuildId, DefaultChannelId, 69, 1,
                "Countdown1", "ChannelName", out Embed embed);

            Assert.True(result == "ok");
            Assert.True(embed.Author.Value.ToString() == "Countdown1");
            Assert.True(embed.Description == "Countdown message 1");
            Assert.True(embed.Title == "COUNTDOWN");
        }

        private Countdown CreateNewDeadline(string type, out DeadlineFileSystem fileSystem, string folderName = "New")
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Test Files", type, folderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            fileSystem = new DeadlineFileSystem(path);

            return new Countdown(_mockClient.Object, fileSystem, new List<DeadlineUserData>());
        }

        private Countdown LoadDeadline(out DeadlineFileSystem fileSystem)
        {
            fileSystem = new DeadlineFileSystem(Path.Combine(Directory.GetCurrentDirectory(), "Test Files", GetType().Name, "Load"));

            return new Countdown(_mockClient.Object, fileSystem, new List<DeadlineUserData>());
        }
    }
}
