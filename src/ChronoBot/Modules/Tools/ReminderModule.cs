using System.Threading.Tasks;
using ChronoBot.Utilities.Tools;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Discord;
using System.Linq;

namespace ChronoBot.Modules.Tools
{
    public class ReminderModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<ReminderModule> _logger;
        private readonly Reminder _reminder;

        public ReminderModule(DiscordSocketClient client, ILogger<ReminderModule> logger, Reminder reminder)
        {
            _logger = logger;
            _reminder = reminder;
            _client = client;
        }

        [Command("remindme")]
        [Alias("remind", "reminder", "rm")]
        public async Task SetReminderUserAsync(string message, DateTime dateTime)
        {
            bool ok = _reminder.SetReminder(message, dateTime, Context.Message.Author.Id, 
                Context.Guild.Id, Context.Channel.Id, Context.Message.Author.Username);
            await HandleSendMessage(Context.Message.Author.Username, Context.Message.Author.Mention, ok, message, dateTime);
        }

        [Command("remindhere")]
        [Alias("remindus", "remindchannel", "rh", "ru", "rc")]
        public async Task SetReminderChannelAsync(string message, DateTime dateTime)
        {
            ulong channel = Context.Message.Channel.Id;
            bool ok = _reminder.SetReminder(message, dateTime, channel,
                Context.Guild.Id, channel, Context.Message.Author.Username);
            await HandleSendMessage(Context.Message.Author.Username, Context.Message.Author.Mention, ok, message, dateTime);
        }

        [Command("remindall")]
        [Alias("remindserver", "remindguild", "remindeveryone", "ra", "rs", "rg", "re")]
        public async Task SetReminderGuildAsync(string message, DateTime dateTime)
        {
            ulong guild = Context.Guild.Id;
            bool ok = _reminder.SetReminder(message, dateTime, guild,
                guild, Context.Channel.Id, Context.Message.Author.Username);
            await HandleSendMessage(Context.Message.Author.Username, Context.Message.Author.Mention, ok, message, dateTime);
        }

        [Command("remindin")]
        [Alias("remindat", "remindon", "rat", "ron")]
        public async Task SetReminderInChannelsync(string message, DateTime dateTime, string where)
        {
            ulong channel = Context.Message.MentionedChannels.Count > 0 ? 
                Context.Message.MentionedChannels.FirstOrDefault()!.Id : 
                Context.Message.Channel.Id;

            ulong guild = Context.Guild.Id;
            bool ok = _reminder.SetReminder(message, dateTime, channel,
                Context.Guild.Id, channel, Context.Message.Author.Username);
            await HandleSendMessage(Context.Message.Author.Username, Context.Message.Author.Mention, ok, message, dateTime);
        }

        private async Task HandleSendMessage(string user, string remindee, bool ok, string message, DateTime dateTime)
        {
            if (ok)
                await SendMessage($"I will remind {remindee} of \"{message}\" in {dateTime}");
            else
                await SendMessage("Something went wrong. Try \"!remindme <message> <date>");

            _logger.LogInformation($"{user} used {System.Reflection.MethodBase.GetCurrentMethod()?.Name} in {GetType().Name}.");
        }

        private async Task SendMessage(string result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(_client, result);
            else
                await ReplyAsync(result);
        }
        private async Task SendMessage(Embed result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(_client, result);
            else
                await ReplyAsync(embed: result);
        }
    }
}
