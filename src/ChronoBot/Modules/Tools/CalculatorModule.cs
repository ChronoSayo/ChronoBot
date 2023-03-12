using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Utilities.Tools;
using Discord.Commands;
using Discord.Interactions;
using Discord;
using Microsoft.Extensions.Logging;
using Discord.WebSocket;
using System.Linq;

namespace ChronoBot.Modules.Tools
{
    public class CalculatorModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private readonly Calculator _calculator;

        public CalculatorModule(Calculator calculator)
        {
            _calculator = calculator;
        }

        [SlashCommand("calculator", "Calculates calculations.")]
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
        }
    }
}
