using Discord.Interactions;
using Discord;
using System;
using System.Threading.Tasks;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord.WebSocket;

namespace ChronoBot.Modules.Tools.Deadlines;

[Group("repeater", "Notifies you the chosen day of the week.")]
public class RepeaterModule : DeadlineModule
{
    public RepeaterModule(DiscordSocketClient client, Deadline deadline) : base(client, deadline)
    {
    }

    [SlashCommand("repeater", "Set repeater.", runMode: RunMode.Async)]
    public override Task SetDeadlineAsync(string message,
        [Summary("When", "Name or number of the day. Sunday = 0.")] DateTime time,
        [Summary("Where", "To which channel should this be posted. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)] IChannel channel = null)
    {
        return base.SetDeadlineAsync(message, time, channel);
    }

    [SlashCommand("repeater-get", "Get repeater.", runMode: RunMode.Async)]
    public override async Task GetDeadlineAsync(
        [Summary("Get", "Gets the specified entry based on the numbered list (see /List command).")] int num,
        [Summary("Channel", "Get an entry from specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)] IChannel channel = null)
    {
        await base.GetDeadlineAsync(num, channel);
    }

    [SlashCommand("repeater-list", "List repeaters in channel.", runMode: RunMode.Async)]
    public override async Task ListDeadlinesAsync([Summary("List", "Lists your entries in the specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)] IChannel channel = null)
    {
        await base.ListDeadlinesAsync(channel);
    }

    [SlashCommand("repeater-delete", "Delete repeater.", runMode: RunMode.Async)]
    public override async Task DeleteDeadlineAsync(
        [Summary("Delete", "Deletes the specified entry based on the numbered list (see /List command).")] int num,
        [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)] IChannel channel = null)
    {
        await base.DeleteDeadlineAsync(num, channel);
    }

    [SlashCommand("repeater-delete-channel", "Delete repeaters in channel.", runMode: RunMode.Async)]
    public override async Task DeleteAllInChannelDeadlineAsync(
        [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)]
        IChannel channel = null)
    {
        await base.DeleteAllInChannelDeadlineAsync(channel);
    }

    [SlashCommand("repeater-delete-server", "Delete repeaters in server.", runMode: RunMode.Async)]
    public override async Task DeleteAllInGuildDeadlineAsync()
    {
        await base.DeleteAllInGuildDeadlineAsync();
    }
}