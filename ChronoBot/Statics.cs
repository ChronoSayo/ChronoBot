using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ChronoBot
{
    public class Statics
    {
        public static DiscordSocketClient CLIENT;
        public static bool DEBUG = true;
        public static int DELAY_TIMER = 3 * 1000;
        public static string ID_PREFIX = "%";
        public static string BULLET_LIST = "■";
        public static ulong DEBUG_GUILD_ID = 882343748535730246;
        public static ulong DEBUG_CHANNEL_ID = 882343749198438523;
        public static ulong DebugVoiceChannelId = 386545580593250306;
        public static ulong BotId = 432972357586649088;
        public static ulong MY_ID = 171262429857185793;
        public static ulong NO_GUILD_ID = 0;

        public const string CommandPrefix = "!";

        private static readonly string _SHRUG = @"¯\_(ツ)_/¯";

        private static readonly Random _RANDOM = new Random();
        public static string BotUsername => CLIENT.CurrentUser.Username; 
        
        public static int GetRandom(int min, int max)
        {
            return _RANDOM.Next(min, max);
        }        
        
        //Send message to my test channel.
        public static async Task DebugSendMessageToChannel(string message)
        {
            await CLIENT.GetGuild(DEBUG_GUILD_ID).GetTextChannel(DEBUG_CHANNEL_ID).SendMessageAsync(message);
        }        
        //Send message to my test channel.
        public static async Task DebugSendMessageToChannel(Embed message)
        {
            await CLIENT.GetGuild(DEBUG_GUILD_ID).GetTextChannel(DEBUG_CHANNEL_ID).SendMessageAsync(embed: message);
        }
    }
}
