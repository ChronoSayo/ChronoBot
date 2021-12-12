﻿using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Utilities.Tools;
using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.Tools
{
    public class CalculatorModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<CalculatorModule> _logger;
        private readonly Calculator _calculator;

        public CalculatorModule(ILogger<CalculatorModule> logger, Calculator calculator)
        {
            _logger = logger;
            _calculator = calculator;
        }

        [Command("calculator")]
        [Alias("calculate", "calc", "c", "calc", "math", "m")]
        public async Task CalculatorAsync([Remainder] string calc)
        {
            bool ok;
            string result = _calculator.Result(calc, out ok);

            if (ok)
            {
                var embed = new ChronoBotEmbedBuilder(result).Build();
                await ReplyAsync(embed: embed);
            }
            else
                await ReplyAsync(result);

            _logger.LogInformation($"{Context.User.Username} used {System.Reflection.MethodBase.GetCurrentMethod()?.Name} in {GetType().Name}.");
        }
    }
}
