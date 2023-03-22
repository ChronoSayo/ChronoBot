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

        [SlashCommand("countdown-get", "Get countdown.", runMode: RunMode.Async)]
        public override async Task GetDeadlineAsync(
            [Summary("Get", "Gets the specified entry based on the numbered list (see /List command).")] int num,
            [Summary("Channel", "Get an entry from specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            await base.GetDeadlineAsync(num, channel);
        }

        [SlashCommand("countdown-list", "List countdowns in channel.", runMode: RunMode.Async)]
        public override async Task ListDeadlinesAsync([Summary("List", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            await base.ListDeadlinesAsync(channel);
        }

        [SlashCommand("countdown-delete", "Delete countdown.", runMode: RunMode.Async)]
        public override async Task DeleteDeadlineAsync(
            [Summary("Delete", "Deletes the specified entry based on the numbered list (see /List command).")] int num,
            [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            await base.DeleteDeadlineAsync(num, channel);
        }

        [SlashCommand("countdown-delete-channel", "Delete countdowns in channel.", runMode: RunMode.Async)]
        public override async Task DeleteAllInChannelDeadlineAsync(
            [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)]
            IChannel channel = null)
        {
            await base.DeleteAllInChannelDeadlineAsync(channel);
        }

        [SlashCommand("countdown-delete-server", "Delete countdowns in server.", runMode: RunMode.Async)]
        public override async Task DeleteAllInGuildDeadlineAsync()
        {
            await base.DeleteAllInGuildDeadlineAsync();
        }
    }
}
