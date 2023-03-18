using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Utilities.Tools;
using Discord.Interactions;

namespace ChronoBot.Modules.Tools
{
    [Group("calculator", "Solves math problems.")]
    public class CalculatorModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Calculator _calculator;

        public CalculatorModule(Calculator calculator)
        {
            _calculator = calculator;
        }

        [SlashCommand("calculator", "Calculates calculations.")]
        public async Task CalculatorAsync([Summary("Calculation", "Insert what you want to be calculated.")] string calculation)
        {
            bool ok;
            string result = _calculator.Result(calculation, out ok);

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
