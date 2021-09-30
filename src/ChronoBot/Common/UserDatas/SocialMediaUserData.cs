using System;
using System.Collections.Generic;
using System.Text;
using ChronoBot.Interfaces;

namespace ChronoBot.Common.UserDatas
{
    public class SocialMediaUserData : IUserData
    {
        public string Name { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Id { get; set; }
        public string SocialMedia { get; set; }
    }
}
