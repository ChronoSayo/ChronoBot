using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
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
            try
            {
                var embed = await _calculator.Result(calc);
                await ReplyAsync(embed: embed);
            }
            catch
            {
                await ReplyAsync("Could not perform calculation.");
            }
            _logger.LogInformation($"{Context.User.Username} used {System.Reflection.MethodBase.GetCurrentMethod()?.Name} in {GetType().Name}.");
        }
    }
}
