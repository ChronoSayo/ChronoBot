using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Common;
using Discord.WebSocket;

namespace ChronoBot.Utilities.Tools
{
    public class Calculator
    {
        public string Result(string calc, out bool ok)
        {
            object result;
            try
            {

                var dt = new DataTable();
                result = dt.Compute(calc, null);
                ok = true;
            }
            catch
            {
                ok = false;
                return $"Unable to calculate {calc}";
            }
            return calc + " = " + result;
        }
        public async Task ResultAsync(SocketSlashCommand command)
        {
            object result;
            int calc = Convert.ToInt32(command.Data.Options.First().Value);
            var dt = new DataTable();
            result = dt.Compute((calc / 2).ToString(), null);
            await command.RespondAsync(result.ToString());
        }
    }
}
