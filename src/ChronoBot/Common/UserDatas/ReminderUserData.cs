using ChronoBot.Interfaces;
using System;

namespace ChronoBot.Common.UserDatas
{
    public class ReminderUserData : IUserData
    {
        public string Name { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Id { get; set; }
        public DateTime Deadline { get; set; }
        public bool DailyReminder { get; set; }
    }
}
