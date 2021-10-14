using Discord;
using Discord.WebSocket;
using System.Timers;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using ChronoBot.Games;
using ChronoBot.SocialMedias;
using ChronoBot.Tests;
using ChronoBot.Tools;
using SharpLink;

namespace ChronoBot
{
    class ChronoBot
    {
        private readonly DiscordSocketClient _client;
        private readonly LavalinkManager _lavalinkManager;
        private Timer _delayInitiation;
        private SocialMedia _twitter, _twitch, _youtube, _instagram;
        private Remind _remind;
        private Selfie _selfie;
        private Calculator _calculator;
        private RockPaperScissors _rps;
        private Music _music;

        private const float _DELAY_INITIATION = 3;

        public ChronoBot(DiscordSocketClient client, LavalinkManager lavalinkManager)
        {
            _client = client;
            _lavalinkManager = lavalinkManager;

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
            //Social medias
            _twitter = new Twitter(_client);
            _twitch = new Twitch(_client);
            _youtube = new YouTube(_client);
            //_instagram = new Instagram(_client);

            //Tools
            //_remind = new Remind(_client);
            _selfie = new Selfie(_client);
            _calculator = new Calculator(_client);
            //_music = new Music(_client, _lavalinkManager);

            //Games
            _rps = new RockPaperScissors();

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
                StackTrace st = new StackTrace();
                MethodBase mb = st.GetFrame(1).GetMethod();
                Program.Logger(new LogMessage(LogSeverity.Error, mb.ReflectedType + "." + mb,
                    "Error receiving message.", e));
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
            //_music.MessageReceived(socketMessage);

            _rps.MessageReceived(socketMessage);
        }
    }
}
