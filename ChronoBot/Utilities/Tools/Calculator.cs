using System;
using System.Data;
using System.Threading.Tasks;
using ChronoBot.Common;

namespace ChronoBot.Utilities.Tools
{
    public class Calculator
    {
        public async Task<Discord.Embed> Result(string calc)
        {
            var dt = new DataTable();
            var result = dt.Compute(calc, null);
            string showResult = calc + " = " + result;

            var embed = new ChronoBotEmbedBuilder(showResult).Build();
            
            return await Task.FromResult(embed);
        }
    }
}
