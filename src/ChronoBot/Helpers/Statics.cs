using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Helpers
{
    public static class Statics
    {
        private static DiscordSocketClient _client;
        private static IConfiguration _config;
        private static readonly Random Random = new Random();

        public static bool Debug => bool.TryParse(_config["Debug"], out bool debug) && debug;
        public static string Prefix => _config["Prefix"];
        public static ulong DebugGuildId => ulong.TryParse(_config["IDs:Guild"], out ulong guildId) ? guildId : 0;
        public static ulong DebugChannelId => ulong.TryParse(_config["IDs:TextChannel"], out ulong channelId) ? channelId : 0;
        public static readonly string Shrug = @"¯\_(ツ)_/¯";

        public static void Init(DiscordSocketClient client, IConfiguration config)
        {
            _client = client;
            _config = config;
        }

        public static int GetRandom(int min, int max)
        {
            return Random.Next(min, max);
        }

        //Send message to my test channel.
        public static async Task DebugSendMessageToChannelAsync(string message)
        {
            await _client.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendMessageAsync(message);
        }
        //Send message to my test channel.
        public static async Task DebugSendMessageToChannelAsync(Embed message)
        {
            await _client.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendMessageAsync(embed: message);
        }
        public static async Task DebugSendFileToChannelAsync(Embed message, string file)
        {
            await _client.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendFileAsync(file, null, embed: message);
        }
    }
}
