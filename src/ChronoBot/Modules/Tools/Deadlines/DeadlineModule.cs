using Discord.Interactions;
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

        public DeadlineModule(DiscordSocketClient client, Countdown countdown)
        {
            Client = client;
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

        public virtual async Task GetDeadlineAsync(
            [Summary("Get", "Gets the specified entry based on the numbered list (see /List command).")] int num,
            [Summary("Channel", "Get an entry from specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            var result = Deadline.GetDeadlines(Context.Guild.Id, channel.Id, Context.User.Id, num,
                Context.User.Username, channel.Name, out Embed embed);
            if (result != "ok")
            {
                await SendMessage(Client, result);
                return;
            }

            await SendMessage(Client, embed);
        }

        public virtual async Task ListDeadlinesAsync([Summary("List", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            var result = Deadline.ListDeadlines(Context.Guild.Id, channel.Id, Context.User.Id,
                Context.User.Username, channel.Name, out Embed embed);
            if (result != "ok")
            {
                await SendMessage(Client, result);
                return;
            }

            await SendMessage(Client, embed);
        }

        public virtual async Task DeleteDeadlineAsync(
            [Summary("Delete", "Deletes the specified entry based on the numbered list (see /List command).")] int num, 
            [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            var result = Deadline.DeleteDeadline(Context.Guild.Id, channel.Id, Context.User.Id, num, channel.Name);

            await SendMessage(Client, result);
        }

        public virtual async Task DeleteAllInChannelDeadlineAsync(
            [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            var result = Deadline.DeleteAllInChannelDeadline(Context.Guild.Id, channel.Id, Context.User.Id, channel.Name);

            await SendMessage(Client, result);
        }

        public virtual async Task DeleteAllInGuildDeadlineAsync()
        {
            var result = Deadline.DeleteAllInGuildDeadline(Context.Guild.Id, Context.User.Id, Context.User.Username);

            await SendMessage(Client, result);
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
