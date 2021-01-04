using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoBot.Interfaces
{
    interface IUserData
    {
        string Name { get; set; }
        ulong GuildId { get; set; }
        ulong ChannelId { get; set; }
        string Id { get; set; }
    }
}
