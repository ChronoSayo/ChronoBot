using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ChronoBot.Interfaces
{
    public interface IBotInteraction
    {
        DiscordSocketClient Client { get; }

        void MessageReceived(SocketMessage socketMessage);

        void LogToFile(LogSeverity severity, string message, Exception e = null,
            [CallerMemberName] string caller = null);
    }
}
