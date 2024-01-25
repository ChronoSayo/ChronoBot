using System;
using System.Reflection;
using System.Threading.Tasks;
using ChronoBot.Helpers;
using ChronoBot.Utilities.SocialMedias;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using TwitchLib.Communication.Interfaces;

namespace ChronoBot.Services
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _command;

        public CommandHandler(DiscordSocketClient client, IServiceProvider provider, InteractionService service)
        {
            _client = client;
            _provider = provider;
            _command = service;
        }

        public async Task InitializeAsync()
        {
            await _command.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            _client.InteractionCreated += HandleInteraction;
            _client.MessageReceived += ClientOnMessageReceived; 
        }

        private async Task ClientOnMessageReceived(SocketMessage arg)
        {
            if (Statics.FetchX)
            {
                string fx = Twitter.GlobalAddFx(arg.Content);
                if (fx.Contains("https://fx"))
                    await arg.Channel.SendMessageAsync(fx);
            }
        }

        private async Task HandleInteraction(SocketInteraction socketInteraction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, socketInteraction);
                await _command.ExecuteCommandAsync(context, _provider);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await Statics.SendMessageToLogChannel(_client, ex.Message);
            }
        }
    }
}
