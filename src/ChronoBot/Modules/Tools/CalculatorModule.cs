using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Utilities.Tools;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.Tools
{
    [Group("calculator", "Solves math problems.")]
    public class CalculatorModule : ChronoInteractionModuleBase
    {
        private readonly DiscordSocketClient _client;
        private readonly Calculator _calculator;

        public CalculatorModule(DiscordSocketClient client, Calculator calculator)
        {
            _calculator = calculator;
            _client = client;
        }

        [SlashCommand("calculator", "Calculates calculations.")]
        public async Task CalculatorAsync([Summary("Calculation", "Insert what you want to be calculated.")] string calculation)
        {
            await DeferAsync();

            bool ok;
            string result = _calculator.Result(calculation, out ok);

            if (ok)
            {
                var embed = new ChronoBotEmbedBuilder(result).Build();
                await SendMessage(_client, embed);
            }
            else
                await SendMessage(_client, result);
        }
    }
}
