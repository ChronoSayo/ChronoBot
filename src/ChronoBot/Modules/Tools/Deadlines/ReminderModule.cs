using System;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.Tools.Deadlines
{
    [Group("reminder", "Notifies you of your reminder.")]
    public class ReminderModule : DeadlineModule
    {
        public ReminderModule(DiscordSocketClient client, Reminder deadline) : base(client, deadline)
        {
            Deadline = deadline;
            DeadlineType = DeadlineEnum.Reminder;
        }

        [SlashCommand("reminder", "Set reminder.", runMode: RunMode.Async)]
        public override Task SetDeadlineAsync(string message,
            [Summary("When", "yyyy-mm-dd or reverse. Time also works: hh:mm")] DateTime time,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            return base.SetDeadlineAsync(message, time, channel);
        }
    }
}
