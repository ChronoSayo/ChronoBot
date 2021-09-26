using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Services
{
    public class CommandHandler : DiscordClientService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _service;
        private readonly IConfiguration _configuration;

        public CommandHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, 
            IServiceProvider provider, CommandService service, IConfiguration configuration) : base(client, logger)
        {
            _client = client;
            _provider = provider;
            _service = service;
            _configuration = configuration;

            Statics.CLIENT = _client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.MessageReceived += MessageReceived;
            _service.CommandExecuted += CommandExecuted;
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private async Task CommandExecuted(Optional<CommandInfo> commandInfo, ICommandContext commandContext, IResult result)
        {
            if (result.IsSuccess)
                return;

            await commandContext.Channel.SendMessageAsync(result.ErrorReason);
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message) || message.Source == MessageSource.Bot) 
                return;

            var argPos = 0;
            if(!message.HasStringPrefix(_configuration["Prefix"], ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);
            await _service.ExecuteAsync(context, argPos, _provider);
        }
    }
}
