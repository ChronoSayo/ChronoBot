using System;
using System.Data;
using System.Threading.Tasks;
using ChronoBot.Common;

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
    }
}
