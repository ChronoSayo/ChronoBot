using ChronoBot.Enums;
using ChronoBot.Interfaces;
using System;

namespace ChronoBot.Common.UserDatas
{
    public class DeadlineUserData : IUserData
    {
        public string Name { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Id { get; set; }
        public DateTime Deadline { get; set; }
        public ulong UserId { get; set; }
        public DeadlineEnum DeadlineType { get; set; }
        public int DaysLeft { get; set; }
    }
}
