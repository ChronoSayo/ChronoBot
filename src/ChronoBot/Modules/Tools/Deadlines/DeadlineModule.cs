﻿using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using System;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.Tools.Deadlines;
using ChronoBot.Common;
using ChronoBot.Common.UserDatas;

namespace ChronoBot.Modules.Tools.Deadlines
{
    public class DeadlineModule : ChronoInteractionModuleBase
    {
        protected readonly DiscordSocketClient Client;
        protected Deadline Deadline;
        protected DeadlineEnum DeadlineType;

        public DeadlineModule(DiscordSocketClient client, Deadline deadline)
        {
            Client = client;
            Deadline = deadline;
        }
        
        public virtual async Task SetDeadlineAsync(string message,
            [Summary("When", "yyyy-mm-dd or reverse. Time also works: hh:mm")] DateTime time,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
                [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            DeadlineUserData newEntry = Deadline.SetDeadline(message, time, Context.Guild.Id, channel.Id, Context.User.Username, Context.User.Id);
            await HandleSendMessage(newEntry, $"Created a {newEntry.DeadlineType}.\n{newEntry.Id}");
        }

        protected virtual async Task HandleSendMessage(DeadlineUserData ud, string description)
        {
            if (ud != null)
                await SendMessage(Client, DeadlineEmbed(ud, description, Client));
            else
                await SendMessage(Client, "Something went wrong.");
        }

        public static Embed DeadlineEmbed(DeadlineUserData ud, string description, DiscordSocketClient client)
        {
            var channel = client.GetGuild(ud.GuildId).GetTextChannel(ud.ChannelId);
            return new EmbedBuilder()
                .WithDescription(description)
                .WithAuthor(channel.GetUser(ud.UserId).Username)
                .WithTitle(ud.DeadlineType.ToString().ToUpper())
                .WithFields(new EmbedFieldBuilder { IsInline = true, Name = "Date", Value = ud.Deadline })
                .WithColor(Color.Green)
                .Build();
        }
    }
}