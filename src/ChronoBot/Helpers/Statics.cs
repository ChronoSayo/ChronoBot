using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Helpers
{
    public static class Statics
    {
        private static readonly Random Random = new Random();

        public static DiscordSocketClient DiscordClient { get; set; }
        public static IConfiguration Config { get; set; }
        public static bool Debug => bool.TryParse(Config["Debug"], out bool debug) && debug;
        public static string Prefix => Config["Prefix"];
        public static ulong DebugGuildId => ulong.TryParse(Config["IDs:Guild"], out ulong guildId) ? guildId : 0;
        public static ulong DebugChannelId => ulong.TryParse(Config["IDs:TextChannel"], out ulong channelId) ? channelId : 0;
        public static readonly string Shrug = @"¯\_(ツ)_/¯";
        
        public static string DiscordToken => Config["Tokens:Discord"];
        public static string TwitterConsumerKey => Config["Tokens:Twitter:ConsumerKey"];
        public static string TwitterConsumerSecret => Config["Tokens:Twitter:ConsumerSecret"];
        public static string TwitterToken => Config["Tokens:Twitter:Token"];
        public static string TwitterSecret => Config["Tokens:Twitter:Secret"];

        public static int GetRandom(int min, int max)
        {
            return Random.Next(min, max);
        }

        //Send message to my test channel.
        public static async Task DebugSendMessageToChannelAsync(string message)
        {
            await DiscordClient.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendMessageAsync(message);
        }
        //Send message to my test channel.
        public static async Task DebugSendMessageToChannelAsync(Embed message)
        {
            await DiscordClient.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendMessageAsync(embed: message);
        }
        public static async Task DebugSendFileToChannelAsync(Embed message, string file)
        {
            await DiscordClient.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendFileAsync(file, null, embed: message);
        }
    }
}
