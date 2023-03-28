using System;
using System.Collections.Generic;
using System.Text;
using ChronoBot.Enums;
using ChronoBot.Interfaces;

namespace ChronoBot.Common.UserDatas
{
    public class SocialMediaUserData : IUserData
    {
        public string Name { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Id { get; set; }
        public SocialMediaEnum SocialMedia { get; set; }
        public string Options { get; set; }
        public bool Live { get; set; }
    }
}
