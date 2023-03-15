using System;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Utilities.Tools;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.Tools
{
    public class CalculatorModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Calculator _calculator;

        public CalculatorModule(Calculator calculator)
        {
            _calculator = calculator;
        }

        [SlashCommand("calculator", "Calculates calculations.")]
        public async Task CalculatorAsync(string calc)
        {
            bool ok;
            string result = _calculator.Result(calc, out ok);

            if (ok)
            {
                var embed = new ChronoBotEmbedBuilder(result).Build();
                await RespondAsync(embed: embed);
            }
            else
                await RespondAsync(result);
        }
    }
}
