using System;
using System.Collections.Generic;
using System.Text;
using ChronoBot.Enums;
using ChronoBot.Interfaces;

namespace ChronoBot.Common.UserDatas
{
    public class RpsUserData : IUserData
    {
        public string Name { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Id { get; set; }

        public ulong UserId;
        public ulong UserIdVs;
        public int Plays;
        public int TotalPlays;
        public int Wins;
        public int Losses;
        public int Draws;
        public int Ratio;
        public int CurrentStreak;
        public int BestStreak;
        public int Resets;
        public int RockChosen;
        public int PaperChosen;
        public int ScissorsChosen;
        public int Coins;
        public DateTime DateVs;
        public RpsActors Actor;
    }
}
