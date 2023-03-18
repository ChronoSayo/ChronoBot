using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChronoBot.Interfaces;

namespace ChronoBot.Common.UserDatas
{
    public class CountdownUserData : IUserData
    {
        public string Name { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string Id { get; set; }
        public DateTime Deadline { get; set; }
        public ulong UserId { get; set; }
    }
}
