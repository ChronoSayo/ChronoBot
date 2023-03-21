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

        public DeadlineModule(DiscordSocketClient client)
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
            var getEntries = Deadline.ListDeadlines(Context.Guild.Id, channel.Id, Context.User.Id);
            string message = $"Nothing found in {channel.Name}.";
            if (getEntries != null)
            {
                if (getEntries[num] == null)
                {
                    await SendMessage(Client, $"Entry number {num} not found.");
                    return;
                }

                message = string.Empty;
                var ud = getEntries[num];
                string deadlineType = ud.DeadlineType.ToString().ToUpper();
                message += $"\"{ud.Id}\"";

                if (ud.DeadlineType == DeadlineEnum.Countdown && message.Contains(Countdown.Key))
                {
                    string daysLeft = message.Split(Countdown.Key)[^1];
                    message = message.Replace($"{Countdown.Key}{daysLeft}", "");
                }

                var embed = new EmbedBuilder()
                    .WithAuthor(Context.User.Username)
                    .WithDescription(message)
                    .WithTitle(deadlineType)
                    .WithColor(Color.Green)
                    .Build();
                await SendMessage(Client, embed);
            }
            else
                await SendMessage(Client, message);
        }

        public virtual async Task ListDeadlinesAsync([Summary("List", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            var getEntries = Deadline.ListDeadlines(Context.Guild.Id, channel.Id, Context.User.Id);
            string message = $"Nothing found in {channel.Name}.";
            if (getEntries != null)
            {
                string deadlineType = getEntries[0].DeadlineType.ToString().ToUpper();
                for (int i = 0; i < getEntries.Count; i++)
                {
                    var ud = getEntries[i];
                    message += $"{i+1}. \"{ud.Id}\" - {ud.Deadline} \n";
                }
                message = message.TrimEnd();

                var embed = new EmbedBuilder()
                    .WithAuthor(Context.User.Username)
                    .WithDescription(message)
                    .WithTitle(deadlineType)
                    .WithColor(Color.Green)
                    .Build();
                await SendMessage(Client, embed);
            }
            else
                await SendMessage(Client, message);
        }

        public virtual async Task DeleteDeadlineAsync(
            [Summary("Delete", "Deletes the specified entry based on the numbered list (see /List command).")] int num, 
            [Summary("Channel", "Lists your entries in the specified channel. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            channel ??= Context.Channel;
            var getEntries = Deadline.ListDeadlines(Context.Guild.Id, channel.Id, Context.User.Id);
            string message = $"Nothing found in {channel.Name}.";
            if (getEntries != null)
            {
                if (getEntries[num] == null)
                {
                    await SendMessage(Client, $"Entry number {num} not found.");
                    return;
                }

                message = string.Empty;
                var ud = getEntries[num];
                string deadlineType = ud.DeadlineType.ToString().ToUpper();
                message += $"\"{ud.Id}\"";

                if (ud.DeadlineType == DeadlineEnum.Countdown && message.Contains(Countdown.Key))
                {
                    string daysLeft = message.Split(Countdown.Key)[^1];
                    message = message.Replace($"{Countdown.Key}{daysLeft}", "");
                }

                var embed = new EmbedBuilder()
                    .WithAuthor(Context.User.Username)
                    .WithDescription(message)
                    .WithTitle(deadlineType)
                    .WithColor(Color.Red)
                    .Build();
                await SendMessage(Client, embed);
            }
            else
                await SendMessage(Client, message);
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
