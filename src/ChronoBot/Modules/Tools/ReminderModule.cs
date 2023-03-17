﻿using System.Threading.Tasks;
using ChronoBot.Utilities.Tools;
using System;
using Discord.WebSocket;
using Discord;
using Discord.Interactions;
using ChronoBot.Common;
using System.Drawing;
using Google.Apis.YouTube.v3.Data;

namespace ChronoBot.Modules.Tools
{
    [Group("reminder", "Notifies you of your reminder.")]
    public class ReminderModule : ChronoInteractionModuleBase
    {
        private readonly DiscordSocketClient _client;
        private readonly Reminder _reminder;

        public ReminderModule(DiscordSocketClient client, Reminder reminder)
        {
            _reminder = reminder;
            _client = client;
        }

        [SlashCommand("reminder", "Set reminder.", runMode: RunMode.Async)]
        public async Task SetReminderAsync(string message,
            [Summary("When", "yyyy-mm-dd or reverse. Time also works: hh:mm")] DateTime time,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")] 
                [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            bool ok = _reminder.SetReminder(message, time, Context.User.Id, Context.Guild.Id, channel.Id, Context.User.Username);
            await HandleSendMessage(Context.User.Username, ok, message, time, channel.Name);
        }


        private async Task HandleSendMessage(string remindee, bool ok, string message, DateTime dateTime, string channelName)
        {
            if (ok)
                await SendMessage(_client, RemindMessage($"A reminder has been created.\n\"{message}\"", remindee, dateTime, channelName));
            else
                await SendMessage(_client, "Something went wrong.");
        }

        public static Embed RemindMessage(string message, string remindee, DateTime dateTime, string channelName)
        {
            return new EmbedBuilder()
                .WithDescription(message)
                .WithAuthor(remindee)
                .WithTitle("REMINDER")
                .WithFields(new EmbedFieldBuilder { IsInline = true, Name = "Date", Value = dateTime })
                .WithFields(new EmbedFieldBuilder { IsInline = true, Name = "In", Value = channelName })
                .WithColor(Discord.Color.Green)
                .Build();
        }
    }
}
