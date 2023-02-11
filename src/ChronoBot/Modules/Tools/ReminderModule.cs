using System.Threading.Tasks;
using ChronoBot.Utilities.Tools;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Discord;

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
            string user = Context.Message.Author.Mention;
            bool ok = _reminder.SetReminder(message, dateTime, user);

            if(ok)
                await SendMessage($"I will remind {user} of \"{message}\" in {dateTime}");
            else
                await SendMessage("Something went wrong. Try \"!remindme <message> <date>");
            
            _logger.LogInformation($"{user} used {System.Reflection.MethodBase.GetCurrentMethod()?.Name} in {GetType().Name}.");
        }

        [Command("remindhere")]
        [Alias("remindus", "remindchannel", "rh", "ru", "rc")]
        public async Task SetReminderChannelAsync(string message, DateTime dateTime)
        {



        }

        [Command("remindall")]
        [Alias("remindserver", "remindguild", "remindeveryone", "ra", "rs", "rg", "re")]
        public async Task SetReminderGuildAsync(string message, DateTime dateTime)
        {



        }

        [Command("remindin")]
        [Alias("remindat", "remindon", "rat", "ron")]
        public async Task SetReminderInChannelsync(string message, DateTime dateTime, string where)
        {



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
