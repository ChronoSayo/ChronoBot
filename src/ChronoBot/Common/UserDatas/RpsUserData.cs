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
        /// <summary>
        /// User input, in this case.
        /// </summary>
        public string Id { get; set; }
        public ulong UserId { get; set; }
        public ulong UserIdVs { get; set; }
        public int Plays { get; set; }
        public int TotalPlays { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int Ratio { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int Resets { get; set; }
        public int RockChosen { get; set; }
        public int PaperChosen { get; set; }
        public int ScissorsChosen { get; set; }
        public int Coins { get; set; }
        public DateTime DateVs { get; set; }
        public RpsActors Actor { get; set; }
        public string Mention { get; set; }
        public string ThumbnailIconUrl { get; set; }
    }
}
