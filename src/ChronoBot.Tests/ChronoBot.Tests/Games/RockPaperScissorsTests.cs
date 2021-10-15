using System;
using System.Collections.Generic;
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
        public void PlayVsBot_Test_Win()
        {
            RpsPlayData player = CreatePlayer("r");

            _rps.Play(player, null, RpsActors.Scissors);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, 1, 1, 1, ratio:100, currentStreak:1, rockChosen:1, coins:1);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_Lose()
        {
            RpsPlayData player = CreatePlayer("r");

            _rps.Play(player, null, RpsActors.Paper);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, 1, 1, losses: 1, rockChosen: 1);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_Draw()
        {
            RpsPlayData player = CreatePlayer("r");

            _rps.Play(player, null, RpsActors.Rock);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, 1, 1, draws: 1, rockChosen: 1);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_WinWithRandom()
        {
            Random rand = new Random();
            RpsPlayData player = CreatePlayer("r");
            RpsUserData user;
            int plays = 0, rocks = 0, papers = 0, scissors = 0;

            rocks++;
            do
            {
                _rps.Play(player, null);
                user = (RpsUserData)_fileSystem.Load().ElementAt(0);
                plays++;
                if(user.Wins > 0)
                    break;
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

            } while (user.Wins <= 0);

            Equal(user, 345678912, plays, plays, 1, user.Losses, user.Draws, user.Ratio, 1, rockChosen: rocks,
                paperChosen: papers, scissorsChosen: scissors, coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_BonusStreak()
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

            Equal(user, 345678912, playsTimes, playsTimes, playsTimes, ratio: 100, currentStreak: playsTimes,
                paperChosen: playsTimes, coins: coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_BestStreak()
        {
            RpsPlayData player = CreatePlayer("p");
            const int playsTimes = 11;

            for (int i = 0; i < playsTimes; i++)
                _rps.Play(player, null, RpsActors.Rock);
            _rps.Play(player, null, RpsActors.Scissors);
            int totalPlayTimes = playsTimes + 1;
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, totalPlayTimes, totalPlayTimes, playsTimes, 1, ratio: user.Ratio,
                bestStreak: playsTimes, paperChosen: totalPlayTimes, coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsBot_Test_WinRatio()
        {
            RpsPlayData player = CreatePlayer("p");
            
            _rps.Play(player, null, RpsActors.Rock);
            _rps.Play(player, null, RpsActors.Scissors);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, user.Plays, user.TotalPlays, user.Wins, user.Losses,
                ratio: 50, bestStreak: user.BestStreak, paperChosen: user.PaperChosen, coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void PlayVsPlayer_Test_WinLose()
        {
            RpsPlayData player1 = CreatePlayer("p");
            RpsPlayData player2 = CreatePlayer("s", "Testa", "Test321", 147258369);

            _rps.Play(player1, player2);
            _rps.Play(player2, player1);
            var users = (List<RpsUserData>) _fileSystem.Load();
            RpsUserData user1 = users.Find(x => x.UserId == 345678912);
            RpsUserData user2 = users.Find(x => x.UserId == 147258369);

            Equal(user1, 345678912, 1, 1, losses: 1, paperChosen: 1);
            Equal(user2, 147258369, 1, 1, 1, ratio: 100, currentStreak: 1, scissorsChosen: 1, coins: 1);
        }

        [Fact]
        public void Options_Test_ResetAndTotalPlays()
        {
            RpsPlayData player = CreatePlayer("p");
            int totalPlays = 3;

            for (int i = 0; i < totalPlays; i++)
                _rps.Play(player, null, RpsActors.Rock);
            _rps.Options(CreatePlayer("r"));
            _rps.Play(CreatePlayer("p"), null, RpsActors.Rock);
            RpsUserData user = (RpsUserData)_fileSystem.Load().ElementAt(0);

            Equal(user, 345678912, 1, totalPlays + 1, user.Wins, ratio: user.Ratio, resets: 1,
                currentStreak: user.CurrentStreak, bestStreak: user.BestStreak, paperChosen: user.PaperChosen,
                coins: user.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void Options_Test_NewUserReset()
        {
            RpsPlayData player = CreatePlayer("r");

            Embed e = _rps.Options(player);

            Assert.Equal($"Stats for {player.Username} has been reset.", e.Title);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void Options_Test_ShowStatistics()
        {
            RpsPlayData player = CreatePlayer("r");

            _rps.Play(player, null, RpsActors.Rock);
            Embed e = _rps.Options(CreatePlayer("s"));
            var fields = e.Fields;
            RpsUserData actualUser = (RpsUserData)_fileSystem.Load().ElementAt(0);
            RpsUserData expectedUser = new RpsUserData
            {
                Plays = int.Parse(fields.First(x => x.Name == "Plays").Value),
                TotalPlays = int.Parse(fields.First(x => x.Name == "Total Plays").Value),
                Wins = int.Parse(fields.First(x => x.Name == "Wins").Value),
                Losses = int.Parse(fields.First(x => x.Name == "Losses").Value),
                Draws = int.Parse(fields.First(x => x.Name == "Draws").Value),
                Ratio = int.Parse(fields.First(x => x.Name == "Win Ratio").Value),
                CurrentStreak = int.Parse(fields.First(x => x.Name == "Current Streak").Value),
                BestStreak = int.Parse(fields.First(x => x.Name == "Best Streak").Value),
                Resets = int.Parse(fields.First(x => x.Name == "Resets").Value),
                RockChosen = int.Parse(fields.First(x => x.Name == ":rock:").Value),
                PaperChosen = int.Parse(fields.First(x => x.Name == ":roll_of_paper:").Value),
                ScissorsChosen = int.Parse(fields.First(x => x.Name == ":scissors:").Value),
                Coins = int.Parse(fields.First(x => x.Name == "Rings").Value)
            };

            Equal(actualUser, 345678912, expectedUser.Plays, expectedUser.TotalPlays, expectedUser.Wins,
                expectedUser.Losses, expectedUser.Draws, expectedUser.Ratio,
                expectedUser.CurrentStreak, expectedUser.BestStreak,
                expectedUser.Resets, expectedUser.RockChosen, expectedUser.PaperChosen,
                expectedUser.ScissorsChosen, expectedUser.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void Options_Test_NewUserShowStatistics()
        {
            RpsPlayData player = CreatePlayer("r");
            
            Embed e = _rps.Options(CreatePlayer("s"));
            var fields = e.Fields;
            RpsUserData actualUser = (RpsUserData)_fileSystem.Load().ElementAt(0);
            RpsUserData expectedUser = new RpsUserData
            {
                Plays = int.Parse(fields.First(x => x.Name == "Plays").Value),
                TotalPlays = int.Parse(fields.First(x => x.Name == "Total Plays").Value),
                Wins = int.Parse(fields.First(x => x.Name == "Wins").Value),
                Losses = int.Parse(fields.First(x => x.Name == "Losses").Value),
                Draws = int.Parse(fields.First(x => x.Name == "Draws").Value),
                Ratio = int.Parse(fields.First(x => x.Name == "Win Ratio").Value),
                CurrentStreak = int.Parse(fields.First(x => x.Name == "Current Streak").Value),
                BestStreak = int.Parse(fields.First(x => x.Name == "Best Streak").Value),
                Resets = int.Parse(fields.First(x => x.Name == "Resets").Value),
                RockChosen = int.Parse(fields.First(x => x.Name == ":rock:").Value),
                PaperChosen = int.Parse(fields.First(x => x.Name == ":roll_of_paper:").Value),
                ScissorsChosen = int.Parse(fields.First(x => x.Name == ":scissors:").Value),
                Coins = int.Parse(fields.First(x => x.Name == "Rings").Value)
            };

            Equal(actualUser, 345678912, expectedUser.Plays, expectedUser.TotalPlays, expectedUser.Wins,
                expectedUser.Losses, expectedUser.Draws, expectedUser.Ratio,
                expectedUser.CurrentStreak, expectedUser.BestStreak,
                expectedUser.Resets, expectedUser.RockChosen, expectedUser.PaperChosen,
                expectedUser.ScissorsChosen, expectedUser.Coins);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        [Fact]
        public void Options_Test_WrongInput()
        {
            RpsPlayData player = CreatePlayer("f");

            Embed e = _rps.Options(player);

            Assert.Equal("Wrong input. \nType stats/s to show your statistics.\nType reset/r to reset the statistics.", e.Description);

            File.Delete(Path.Join(_fileSystem.PathToSaveFile, $"{player.GuildId}.xml"));
        }

        private void Equal(RpsUserData ud, ulong userId = 0, int plays = 0, int totalPlays = 0,
            int wins = 0, int losses = 0, int draws = 0, int ratio = 0, int currentStreak = 0, int bestStreak = 0,
            int resets = 0, int rockChosen = 0, int paperChosen = 0, int scissorsChosen = 0, int coins = 0)
        {
            Assert.Equal((double) userId, ud.UserId);
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

        private RpsPlayData CreatePlayer(string input, string username = "Tester", string mention = "Test123", ulong userId = 345678912)
        {
            return new()
            {
                ChannelId = 123456789,
                GuildId = 234567891,
                UserId = userId,
                Input = input,
                Mention = mention,
                ThumbnailIconUrl = "icon.png",
                Username = username
            };
        }
    }
}
