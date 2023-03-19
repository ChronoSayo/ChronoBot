using ChronoBot.Enums;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using System;
using System.Threading.Tasks;

namespace ChronoBot.Modules.Tools.Deadlines
{
    [Group("countdown", "Counts down until the day of the message.")]
    public class CountdownModule : DeadlineModule
    {
        public CountdownModule(DiscordSocketClient client, Countdown deadline) : base(client, deadline)
        {
            Deadline = deadline;
            DeadlineType = DeadlineEnum.Countdown;
        }

        [SlashCommand("countdown", "Set countdown.", runMode: RunMode.Async)]
        public override Task SetDeadlineAsync(string message,
            [Summary("When", "yyyy-mm-dd or reverse.")] DateTime time,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            return base.SetDeadlineAsync(message, time, channel);
        }
    }
}
