using System.Data;
using System.Text.RegularExpressions;
using Discord.WebSocket;

namespace ChronoBot.Tools
{
    class Calculator
    {
        private const string Command = Info.COMMAND_PREFIX + "calc";

        public void MessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot)
                return;

            Calculate(socketMessage);
        }

        private void Calculate(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString();
            if (!message.ToLower().Contains(Command))
                return;

            string[] split = message.Split(' '); //0: Command. >1: calculations
            if (split[0] != Command || split.Length == 1)
                return;

            string calculations = message.Remove(0, split[0].Length);
            calculations = calculations.Replace(" ", string.Empty);
            if (Regex.IsMatch(calculations, @"[a-zA-Z]"))
                return;

            calculations = new DataTable().Compute(calculations, null).ToString();

            Info.SendMessageToChannel(socketMessage, calculations);
        }
    }
}
