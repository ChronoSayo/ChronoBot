using System;
using System.IO;
using System.Linq;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Utilities.Games;
using Discord;
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
            if (!File.Exists(Path.Join(_fileSystem.PathToSaveFile, $"{DefaultGuildId}.xml"))) 
                return;

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{DefaultGuildId}.xml"));
            _fileSystem = new RpsFileSystem();
            _rps = new RockPaperScissors(config.Object, _fileSystem);
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

        [Fact]
        public void PlayVsBot_Test_Draw_Success()
        {
            RpsPlayData player = CreatePlayer("r");

            _rps.Play(player, null, RpsActors.Rock);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, plays: 1, totalPlays: 1, draws: 1, rockChosen: 1);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_WinWithRandom_Success()
        {
            Random rand = new Random();
            RpsPlayData player = CreatePlayer("r");
            RpsUserData user;
            int plays = 0, rocks = 0, papers = 0, scissors = 0;

            while (true)
            {
                _rps.Play(player, null, RpsActors.Scissors);
                user = (RpsUserData) _fileSystem.Load().ElementAt(0);
                plays++;
                if (user.Wins > 0)
                {
                    string s;
                    int i = rand.Next(0, 3);
                    switch (i)
                    {
                        case 0:
                            rocks++;
                            s = "r";
                            break;
                        case 1:
                            papers++;
                            s = "p";
                            break;
                        default:
                            scissors++;
                            s = "s";
                            break;
                    }

                    player = CreatePlayer(s);
                    break;
                }
            }

            Equal(user, 345678912, plays: plays, totalPlays: plays, wins: 1, ratio: user.Ratio, currentStreak: 1,
                rockChosen: rocks, paperChosen: papers, scissorsChosen: scissors, coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_BonusStreak_Success()
        {
            RpsPlayData player = CreatePlayer("p");
            const int playsTimes = 11;
            int coins = 0;

            for (int i = 0; i < playsTimes; i++)
                _rps.Play(player, null, RpsActors.Rock);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);
            for (int i = 1; i < playsTimes + 1; i++)
            {
                int bonus = 1;
                if (i % 10 == 0)
                    bonus = i * 2;
                coins += (int)Math.Ceiling(bonus * 0.5f);
            }

            Equal(user, 345678912, plays: playsTimes, totalPlays: playsTimes, wins: playsTimes, ratio: 100,
                currentStreak: playsTimes, paperChosen: playsTimes, coins: coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_BestStreak_Success()
        {
            RpsPlayData player = CreatePlayer("p");
            const int playsTimes = 11;

            for (int i = 0; i < playsTimes; i++)
                _rps.Play(player, null, RpsActors.Rock);
            _rps.Play(player, null, RpsActors.Scissors);
            int totalPlayTimes = playsTimes + 1;
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, plays: totalPlayTimes, totalPlays: totalPlayTimes, wins: playsTimes, losses: 1,
                ratio: user.Ratio, bestStreak: playsTimes, paperChosen: totalPlayTimes, coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_WinRatio_Success()
        {
            RpsPlayData player = CreatePlayer("p");
            
            _rps.Play(player, null, RpsActors.Rock);
            _rps.Play(player, null, RpsActors.Scissors);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, plays: user.Plays, totalPlays: user.TotalPlays, wins: user.Wins, losses: user.Losses,
                ratio: 50, bestStreak: user.BestStreak, paperChosen: user.PaperChosen, coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_ResetsAndTotalPlays_Success()
        {
            RpsPlayData player = CreatePlayer("p");
            int totalPlays = 3;

            for (int i = 0; i < totalPlays; i++)
                _rps.Play(player, null, RpsActors.Rock);
            _rps.Options(CreatePlayer("r"));
            _rps.Play(CreatePlayer("p"), null, RpsActors.Rock);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, plays: 1, totalPlays: totalPlays + 1, wins: user.Wins,
                ratio: user.Ratio, resets: 1, currentStreak: user.CurrentStreak, bestStreak: user.BestStreak, paperChosen: user.PaperChosen, coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_ShowStatistics_Success()
        {
            RpsPlayData player = CreatePlayer("p");
            
            _rps.Play(player, null, RpsActors.Rock);
            Embed e = _rps.Options(CreatePlayer("s"));
            var fields = e.Fields;
            RpsUserData user = new RpsUserData()
            {
                Plays = int.Parse(fields.First(x => x.Name == "Plays").Value),
                TotalPlays = int.Parse(fields.First(x => x.Name == "Plays").Value),
            };

            Equal(user, 345678912, plays: 1, totalPlays: totalPlays + 1, wins: user.Wins,
                ratio: user.Ratio, resets: 1, currentStreak: user.CurrentStreak, bestStreak: user.BestStreak, paperChosen: user.PaperChosen, coins: user.Coins);

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
                Input = input,
                Mention = "Test123",
                ThumbnailIconUrl = "icon.png",
                Username = "Tester"
            };
        }
    }
}
