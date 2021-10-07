﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Helpers
{
    public static class Statics
    {
        private static readonly Random Random = new Random();
        
        public static IConfiguration Config { get; set; }
        public static bool Debug => bool.TryParse(Config["Debug"], out bool debug) && debug;
        public static string Prefix => Config["Prefix"];
        public static ulong DebugGuildId => ulong.TryParse(Config["IDs:Guild"], out ulong guildId) ? guildId : 0;
        public static ulong DebugChannelId => ulong.TryParse(Config["IDs:TextChannel"], out ulong channelId) ? channelId : 0;
        public static readonly string Shrug = @"¯\_(ツ)_/¯";
        
        public static string DiscordToken => "Tokens:Discord";
        public static string TwitterConsumerKey => "Tokens:Twitter:ConsumerKey";
        public static string TwitterConsumerSecret => "Tokens:Twitter:ConsumerSecret";
        public static string TwitterToken => "Tokens:Twitter:Token";
        public static string TwitterSecret => "Tokens:Twitter:Secret";

        public static int GetRandom(int min, int max)
        {
            return Random.Next(min, max);
        }

        //Send message to my test channel.
        public static async Task DebugSendMessageToChannelAsync(DiscordSocketClient client, string message)
        {
            await client.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendMessageAsync(message);
        }
        //Send message to my test channel.
        public static async Task DebugSendMessageToChannelAsync(DiscordSocketClient client, Embed message)
        {
            await client.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendMessageAsync(embed: message);
        }
        public static async Task DebugSendFileToChannelAsync(DiscordSocketClient client, Embed message, string file)
        {
            await client.GetGuild(DebugGuildId).GetTextChannel(DebugChannelId).SendFileAsync(file, null, embed: message);
        }
    }
}
