using System;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.Tools.Deadlines;

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

    [SlashCommand("reminder-get", "Get reminder.", runMode: RunMode.Async)]
    public override async Task GetDeadlineAsync(
        [Summary("Get", "Gets the specified entry based on the numbered list (see /List command).")] int num,
        [Summary("Channel", "Get an entry from specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)] IChannel channel = null)
    {
        await base.GetDeadlineAsync(num, channel);
    }

    [SlashCommand("reminder-list", "List reminders in channel.", runMode: RunMode.Async)]
    public override async Task ListDeadlinesAsync([Summary("List", "Lists your entries in the specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)] IChannel channel = null)
    {
        await base.ListDeadlinesAsync(channel);
    }

    [SlashCommand("reminder-delete", "Delete reminder.", runMode: RunMode.Async)]
    public override async Task DeleteDeadlineAsync(
        [Summary("Delete", "Deletes the specified entry based on the numbered list (see /List command).")] int num,
        [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)] IChannel channel = null)
    {
        await base.DeleteDeadlineAsync(num, channel);
    }

    [SlashCommand("reminder-delete-channel", "Delete reminders in channel.", runMode: RunMode.Async)]
    public override async Task DeleteAllInChannelDeadlineAsync(
        [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
        [ChannelTypes(ChannelType.Text)]
        IChannel channel = null)
    {
        await base.DeleteAllInChannelDeadlineAsync(channel);
    }

    [SlashCommand("reminder-delete-server", "Delete reminders in server.", runMode: RunMode.Async)]
    public override async Task DeleteAllInGuildDeadlineAsync()
    {
        await base.DeleteAllInGuildDeadlineAsync();
    }
}