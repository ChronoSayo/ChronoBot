using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Utilities.Tools;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;

namespace ChronoBot.Modules.Tools
{
    public class ReminderModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<ReminderModule> _logger;
        private readonly Reminder _reminder;

        public ReminderModule(ILogger<ReminderModule> logger, Reminder reminder)
        {
            _logger = logger;
            _reminder = reminder;
        }

        [Command("reminder")]
        [Alias("remindme", "rm")]
        public async Task SetReminderAsync(string message, DateTime dateTime, [Remainder] string options)
        {


            _logger.LogInformation($"{Context.User.Username} used {System.Reflection.MethodBase.GetCurrentMethod()?.Name} in {GetType().Name}.");
        }
    }
}
