using System.Threading.Tasks;
using ChronoBot.Utilities.Tools;
using Microsoft.Extensions.Logging;
using System;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Discord;
using System.Linq;
using Discord.Interactions;
using ChronoBot.Common;

namespace ChronoBot.Modules.Tools
{
    public class ReminderModule : ChronoInteractionModuleBase
    {
        private readonly DiscordSocketClient _client;
        private readonly Reminder _reminder;

        public ReminderModule(DiscordSocketClient client, Reminder reminder)
        {
            _reminder = reminder;
            _client = client;
        }

        [SlashCommand("Reminder", "Set reminder.", runMode: RunMode.Async)]
        public async Task SetReminderAsync(string message, DateTime date, [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            bool ok = _reminder.SetReminder(message, date, Context.User.Id, Context.Guild.Id, channel.Id, Context.User.Username);
            await HandleSendMessage(Context.User.Mention, ok, message, date);
        }


        private async Task HandleSendMessage(string remindee, bool ok, string message, DateTime dateTime)
        {
            if (ok)
                await SendMessage(_client, $"I will remind {remindee} of \"{message}\" in {dateTime}");
            else
                await SendMessage(_client, "Something went wrong. Try \"!remindme <message> <date>");
        }
    }
}
