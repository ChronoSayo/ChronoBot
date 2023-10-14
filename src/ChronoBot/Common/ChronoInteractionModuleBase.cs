﻿using ChronoBot.Helpers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace ChronoBot.Common
{
    public class ChronoInteractionModuleBase : InteractionModuleBase<SocketInteractionContext>
    {
        protected virtual async Task SendMessage(DiscordSocketClient client, string result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(client, result);
            else
                await ReplyAsync(result);
        }
        protected virtual async Task SendMessage(DiscordSocketClient client, Embed result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(client, result);
            else
                await ReplyAsync(embed: result);
        }
        protected virtual async Task SendMessage(DiscordSocketClient client, string result, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(client, result);
            else if (sendToChannel != 0)
                await client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(result);
            else
                await ReplyAsync(result);
        }
        protected virtual async Task SendMessage(DiscordSocketClient client, Embed result, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(client, result);
            else if (sendToChannel != 0)
                await client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(embed: result);
            else
                await ReplyAsync("", embed: result);
        }
        protected virtual async Task SendMessage(DiscordSocketClient client, string result, Embed resultEmbed, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(client, result, resultEmbed);
            else if (sendToChannel != 0)
                await client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(embed: resultEmbed);
            else
                await ReplyAsync(result, embed: resultEmbed);
        }
        protected virtual async Task SendFile(DiscordSocketClient client, Embed result, string file)
        {
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(client, result, file);
            else
                await FollowupWithFileAsync(file, embed: result);
        }
    }
}
