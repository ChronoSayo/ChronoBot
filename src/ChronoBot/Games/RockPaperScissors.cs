using System;
using Discord.WebSocket;
using System.Timers;

namespace ChronoBot.Games
{
    class RockPaperScissors
    {
        private Timer _timerVs;

        /// <summary>
        /// R < P < S < R
        /// </summary>
        public enum Actor
        {
            Rock, Paper, Scissors, Max
        }

        public RockPaperScissors()
        {
            VersusTimer();
        }

        //Timer to check the dates of last player response. 
        private void VersusTimer()
        {
            _timerVs = new Timer(60 * 60 * 1000)
            {
                AutoReset = false, 
                Enabled = false
            }; 
            //_timerVs.Elapsed += ;
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            StartPlaying(socketMessage);
        }

        private void StartPlaying(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString().ToLowerInvariant();
            if (!message.StartsWith(Info.COMMAND_PREFIX + "rps"))
                return;

            Actor actor = Actor.Max;
            try
            {
                var action = message.Remove(0, (Info.COMMAND_PREFIX + "rps").Length).Replace(" ", string.Empty);
                actor = ConvertPlayerDecisionToActor(action);
            }
            catch
            {
                // ignore
            }
            if(actor == Actor.Max)
                Info.SendMessageToChannel(socketMessage, "Wrong input. Choose either rock(r), paper(p), or scissors(s)");
            else
                ProcessChosenActors(actor, socketMessage);
        }

        private void ProcessChosenActors(Actor playerActor, SocketMessage socketMessage)
        {
            Random random = new Random();
            int bot = random.Next(0, (int)Actor.Max);
            int player = (int) playerActor;
            string userMention = "You";
            if (socketMessage.Author.Mention != null)
                userMention = socketMessage.Author.Mention;

            string botRespond =
                $"{userMention} chose {ConvertActorToEmoji(playerActor)}\nBot chose {ConvertActorToEmoji((Actor) bot)}\n";

            if ((bot + 1) % (int)Actor.Max == player)
            {
                Info.SendMessageToChannel(socketMessage, botRespond + $"{socketMessage.Author.Mention} wins!");
            }
            else if ((player + 1) % (int)Actor.Max == bot)
            {
                Info.SendMessageToChannel(socketMessage, botRespond + $"{socketMessage.Author.Mention} lost...");
            }
            else
            {
                Info.SendMessageToChannel(socketMessage, botRespond + $"Draw game with {socketMessage.Author.Mention}.");
            }
        }

        private Actor ConvertPlayerDecisionToActor(string actorString)
        {
            switch (actorString.ToLowerInvariant())
            {
                case "r":
                case "rock":
                    return Actor.Rock;
                case "p":
                case "paper":
                    return Actor.Paper;
                case "s":
                case "scissors":
                    return Actor.Scissors;
            }

            return Actor.Max;
        }

        private string ConvertActorToEmoji(Actor a)
        {
            string s = string.Empty;
            switch (a)
            {
                case Actor.Rock:
                    return ":rock:";
                case Actor.Paper:
                    return ":roll_of_paper:";
                case Actor.Scissors:
                    return ":scissors:";
            }
            return s;
        }
    }
}
