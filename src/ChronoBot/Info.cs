using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Discord;

namespace ChronoBot
{
    class Info
    {
        public static DiscordSocketClient CLIENT;
        public static bool DEBUG = false;
        public static int DELAY_TIMER = 3 * 1000;
        public static string ID_PREFIX = "%";
        public static string BULLET_LIST = "■";
        public static ulong DEBUG_GUILD_ID = 386545577258778634;
        public static ulong DEBUG_CHANNEL_ID = 386545577258778637;
        public static ulong DebugVoiceChannelId = 386545580593250306;
        public static ulong BotId = 432972357586649088;
        public static ulong MY_ID = 171262429857185793;
        public static ulong NO_GUILD_ID = 0;


        public const string COMMAND_PREFIX = "!";

        private static readonly string _SHRUG = @"¯\_(ツ)_/¯";

        private static readonly Random _RANDOM = new Random();

        public static string BotUsername => CLIENT.CurrentUser.Username;

        public static int GetRandom(int min, int max)
        {
            return _RANDOM.Next(min, max);
        }

        public static void SendMessageToChannel(ulong guildId, ulong channelId, string message)
        {
            if (DEBUG)
                DebugSendMessageToChannel(message);
            else
                CLIENT.GetGuild(guildId).GetTextChannel(channelId).SendMessageAsync(message);
        }
        public static void SendMessageToChannel(SocketMessage socketMessage, string message)
        {
            if (DEBUG)
                DebugSendMessageToChannel(message);
            else
                socketMessage.Channel.SendMessageAsync(message);
        }

        public static void SendMessageToChannel(SocketMessage socketMessage, string message, Color color)
        {
            if (DEBUG)
                DebugSendEmbeddedMessageToChannel(BuildEmbed(message, color));
            else
                socketMessage.Channel.SendMessageAsync(string.Empty, embed: BuildEmbed(message, color));
        }
        public static void SendMessageToChannelSuccess(SocketMessage socketMessage, string message)
        {
            Color color = Color.Green;
            if (DEBUG)
                DebugSendEmbeddedMessageToChannel(BuildEmbed(message, color));
            else
                socketMessage.Channel.SendMessageAsync(string.Empty, embed: BuildEmbed(message, color));
        }
        public static void SendMessageToChannelFail(SocketMessage socketMessage, string message)
        {
            Color color = Color.Red;
            if (DEBUG)
                DebugSendEmbeddedMessageToChannel(BuildEmbed(message, color));
            else
                socketMessage.Channel.SendMessageAsync(string.Empty, embed: BuildEmbed(message, color));
        }
        public static void SendMessageToChannelOther(SocketMessage socketMessage, string message)
        {
            Color color = Color.Gold;
            if (DEBUG)
                DebugSendEmbeddedMessageToChannel(BuildEmbed(message, color));
            else
                socketMessage.Channel.SendMessageAsync(string.Empty, embed: BuildEmbed(message, color));
        }
        public static void SendMessageToChannel(SocketMessage socketMessage, Embed embed)
        {
            if (DEBUG)
                DebugSendEmbeddedMessageToChannel(embed);
            else
                socketMessage.Channel.SendMessageAsync(string.Empty, embed: embed);
        }
        public static void SendMessageToChannel(ulong guildId, ulong channelId, string message, Color color)
        {
            if (DEBUG)
                DebugSendEmbeddedMessageToChannel(BuildEmbed(message, color));
            else
                CLIENT.GetGuild(guildId).GetTextChannel(channelId).SendMessageAsync(string.Empty, embed: BuildEmbed(message, color));
        }
        public static void EditMessageInChannel(SocketMessage socketMessage, string message)
        {
            if (DEBUG)
                DebugEditMessageInChannel(socketMessage.Id, message);
            else
                socketMessage.Channel.ModifyMessageAsync(socketMessage.Id, x => x.Content = message);
        }
        public static void DeleteMessageInChannel(SocketMessage socketMessage)
        {
            if (DEBUG)
                DebugDeleteMessageInChannel(socketMessage.Id);
            else
                socketMessage.Channel.DeleteMessageAsync(socketMessage.Id);
        }
        public static void SendMessageToUser(SocketUser socketUser, string message)
        {
            if (DEBUG)
                DebugSendMessageToMyself(message);
            else
                socketUser.GetOrCreateDMChannelAsync().Result.SendMessageAsync(message);
        }

        public static void SendFileToChannel(SocketMessage socketMessage, string filePath, string message)
        {
            CLIENT.GetGuild(GetGuildIDFromSocketMessage(socketMessage)).
                GetTextChannel(socketMessage.Channel.Id).SendFileAsync(filePath, message);
        }
        public static void SendFileToChannel(SocketMessage socketMessage, string filePath, Embed embed)
        {
            CLIENT.GetGuild(GetGuildIDFromSocketMessage(socketMessage)).
                GetTextChannel(socketMessage.Channel.Id).SendFileAsync(filePath, string.Empty, embed: embed);
        }

        //Send message to my test channel.
        private static void DebugSendMessageToChannel(string message)
        {
            ulong serverID = 386545577258778634;
            ulong channelID = 386545577258778637;
            CLIENT.GetGuild(serverID).GetTextChannel(channelID).SendMessageAsync(message);
        }    

        //Send embedded message to my test channel.
        private static void DebugSendEmbeddedMessageToChannel(Embed embed)
        {
            ulong serverID = 386545577258778634;
            ulong channelID = 386545577258778637;
            CLIENT.GetGuild(serverID).GetTextChannel(channelID).SendMessageAsync(string.Empty, embed: embed);
        }
        //Send embedded message to my test channel.
        private static void DebugSendEmbeddedMessageToChannel(string message, Color color)
        {
            ulong serverID = 386545577258778634;
            ulong channelID = 386545577258778637;
            CLIENT.GetGuild(serverID).GetTextChannel(channelID).SendMessageAsync(string.Empty, false, BuildEmbed(message, color));
        }

        //Edit message to my test channel.
        private static void DebugEditMessageInChannel(ulong messageId, string message)
        {
            ulong serverID = 386545577258778634;
            ulong channelID = 386545577258778637;
            CLIENT.GetGuild(serverID).GetTextChannel(channelID).ModifyMessageAsync(messageId, x => x.Content = message);
        }
        //Send message to my test channel.
        private static void DebugDeleteMessageInChannel(ulong messageId)
        {
            ulong serverID = 386545577258778634;
            ulong channelID = 386545577258778637;
            CLIENT.GetGuild(serverID).GetTextChannel(channelID).DeleteMessageAsync(messageId);
        }
        //Send message to myself.
        private static void DebugSendMessageToMyself(string message)
        {
            ulong id = 171262429857185793;
            CLIENT.GetUser(id).GetOrCreateDMChannelAsync().Result.SendMessageAsync(message);
        }

        public static void Shrug(SocketMessage socket)
        {
            if (DEBUG)
                DebugShrug();
            else
                SendMessageToChannel(socket, _SHRUG, Color.Red);
        }
        private static void DebugShrug()
        {
            DebugSendEmbeddedMessageToChannel(_SHRUG, Color.Red);
        }

        private static Embed BuildEmbed(string message, Color color)
        {
            EmbedBuilder eb = new EmbedBuilder
            {
                Color = color,
                Description = message
            };

            return eb.Build();
        }

        public static ulong GetGuildIDFromSocketMessage(SocketMessage socketMessage)
        {
            SocketGuildChannel sgc = socketMessage.Channel as SocketGuildChannel;
            ulong guildID = NO_GUILD_ID;
            if(sgc != null)
                guildID = DEBUG ? DEBUG_GUILD_ID : sgc.Guild.Id;
            return guildID;
        }
        public static bool NoGuildID(ulong guildID)
        {
            return guildID == NO_GUILD_ID;
        }
    }
}
