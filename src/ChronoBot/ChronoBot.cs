using Discord;
using Discord.WebSocket;
using System.Timers;
using System.Threading.Tasks;
using System;

namespace ChronoBot
{
    class ChronoBot
    {
        private DiscordSocketClient _client;
        private Timer _delayInitiation;
        private SocialMedia _twitter, _twitch, _youtube, _instagram;
        private Remind _remind;
        private Selfie _selfie;
        private Calculator _calculator;

        ImageTest test;

        private const float _DELAY_INITIATION = 3;

        public ChronoBot(DiscordSocketClient client)
        {
            _client = client;

            Info.CLIENT = _client;

            DelayInitiation();
        }

        private void DelayInitiation()
        {
            _delayInitiation = new Timer(_DELAY_INITIATION * 1000); //3 seconds.
            _delayInitiation.Start();
            _delayInitiation.Elapsed += _delayInitiation_Elapsed;
        }

        private void _delayInitiation_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Social media
            _twitter = new Twitter(_client);
            _twitch = new Twitch(_client);
            _youtube = new YouTube(_client);
            //_instagram = new Instagram(_client);

            //Tools
            //_remind = new Remind(_client);
            _selfie = new Selfie(_client);
            _calculator = new Calculator(_client);

            //Debug
            test = new ImageTest(_client);

            _delayInitiation.Stop();

            HandleMessage();
        }

        private void HandleMessage()
        {
            _client.MessageReceived += _client_MessageReceived;
            //_client.MessageDeleted += _client_MessageDeleted;
            //_client.MessageUpdated += _client_MessageUpdated;
        }

        private Task _client_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            if (arg2 != null)
                MessageReceived(arg2);

            return Task.CompletedTask;
        }

        private Task _client_MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            return Task.CompletedTask;
        }

        private Task _client_MessageReceived(SocketMessage socketMessage)
        {
            try
            {
                MessageReceived(socketMessage);
            }
            catch(Exception e)
            {
                Program.LogFile(e.Message);
            }
            return Task.CompletedTask;
        }

        private void MessageReceived(SocketMessage socketMessage)
        {
            //if (socketMessage.Activity == null)
            //    return;

            _twitter.MessageReceived(socketMessage);
            _twitch.MessageReceived(socketMessage);
            _youtube.MessageReceived(socketMessage);

            //_remind.MessageReceived(socketMessage);
            _selfie.MessageReceived(socketMessage);
            _calculator.MessageReceived(socketMessage);


            test.MessageReceived(socketMessage);
        }
    }
}
