using System;
using System.Reflection;
using System.Threading.Tasks;
using ChronoBot.Helpers;
using Discord.Interactions;
using Discord.WebSocket;

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

            var message = await socketInteraction.GetOriginalResponseAsync();
            if(message != null)
                await message.DeleteAsync();
        }
    }
}
