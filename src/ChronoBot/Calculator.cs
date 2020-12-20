using System.Data;
using System.Text.RegularExpressions;
using Discord.WebSocket;

namespace ChronoBot
{
    class Calculator
    {
        private DiscordSocketClient _client;
        private const string COMMAND = Info.COMMAND_PREFIX + "calc";

        public Calculator(DiscordSocketClient client)
        {
            _client = client;
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot)
                return;

            Calculate(socketMessage);
        }

        private void Calculate(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString();
            if (!message.ToLower().Contains(COMMAND))
                return;

            string[] split = message.Split(' '); //0: Command. >1: calculations
            if (split[0] != COMMAND || split.Length == 1)
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
