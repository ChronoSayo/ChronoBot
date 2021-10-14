using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Utilities.Games;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ChronoBot.Tests.Games
{
    public class RockPaperScissorsTests
    {
        private readonly RockPaperScissors _rps;
        private readonly RpsFileSystem _fileSystem;
        private const ulong DefaultGuildId = 234567891;

        public RockPaperScissorsTests()
        {
            var config = new Mock<IConfiguration>();
            config.SetupGet(x => x[It.Is<string>(y => y == "Images:RPS:Win")]).Returns("win.png");
            config.SetupGet(x => x[It.Is<string>(y => y == "Images:RPS:Lose")]).Returns("lose.png");
            config.SetupGet(x => x[It.Is<string>(y => y == "Images:RPS:Draw")]).Returns("draw.png");

            _fileSystem = new RpsFileSystem();
            _rps = new RockPaperScissors(config.Object, _fileSystem);
            if(File.Exists(Path.Join(_fileSystem.PathToSaveFile, $"{DefaultGuildId}.xml")))
                File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{DefaultGuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_Win_Success()
        {
            RpsPlayData player = CreatePlayer("r");

            _rps.Play(player, null, RpsActors.Scissors);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, plays:1, totalPlays:1, wins:1, ratio:100, currentStreak:1, rockChosen:1, coins:1);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_Lose_Success()
        {
            RpsPlayData player = CreatePlayer("r");

            _rps.Play(player, null, RpsActors.Paper);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, plays: 1, totalPlays: 1, losses: 1, rockChosen: 1);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        private void Equal(RpsUserData ud, ulong userId = 0, ulong userIdVs = 0, int plays = 0, int totalPlays = 0,
            int wins = 0, int losses = 0, int draws = 0, int ratio = 0, int currentStreak = 0, int bestStreak = 0,
            int resets = 0, int rockChosen = 0, int paperChosen = 0, int scissorsChosen = 0, int coins = 0,
            DateTime dateVs = default(DateTime))
        {
            Assert.Equal((double) userId, ud.UserId);
            Assert.Equal((double) userIdVs, ud.UserIdVs);
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
            if(dateVs == default)
                dateVs = DateTime.Now;
            Assert.Equal(dateVs.Day, ud.DateVs.Day);
        }

        private RpsPlayData CreatePlayer(string input)
        {
            return new()
            {
                ChannelId = 123456789,
                GuildId = 234567891,
                UserId = 345678912,
                Input = "r",
                Mention = "Test123",
                ThumbnailIconUrl = "icon.png",
                Username = "Tester"
            };
        }
    }
}
