﻿using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChronoBot.Helpers;
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
        private readonly IConfiguration _config;
        private readonly ILogger<DiscordClientService> _logger;

        public CommandHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, 
            IServiceProvider provider, CommandService service, IConfiguration config) : base(client, logger)
        {
            _client = client;
            _provider = provider;
            _service = service;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.MessageReceived += MessageReceived;
            _service.CommandExecuted += CommandExecuted;
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private async Task CommandExecuted(Optional<CommandInfo> commandInfo, ICommandContext commandContext, IResult result)
        {
            var embed = new EmbedBuilder()
                .WithAuthor(commandContext.User.Username)
                .WithTitle($"Channel {commandContext.Channel.Name} in Guild {commandContext.Guild.Name}")
                .WithDescription(commandContext.Message.Content)
                .AddField("User Id", commandContext.User.Id, true)
                .AddField("User Discr.", commandContext.User.Discriminator, true)
                .AddField("Channel Id", commandContext.Channel.Id, true)
                .AddField("Guild ID", commandContext.Guild.Id, true);

            if (result.IsSuccess)
            {
                await _client.GetGuild(Statics.DebugGuildId).GetTextChannel(Statics.DebugLogsChannelId)
                    .SendMessageAsync(embed: embed.Build()); 
                return;
            }

            await commandContext.Channel.SendMessageAsync(result.ErrorReason);

            embed.WithDescription(result.ErrorReason);
            await _client.GetGuild(Statics.DebugGuildId).GetTextChannel(Statics.DebugLogsChannelId)
                .SendMessageAsync(embed: embed.Build());
        }

        private async Task MessageReceived(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message) || message.Source == MessageSource.Bot) 
                return;

            var argPos = 0;
            string s = "Prefix";
            if(!message.HasStringPrefix(_config[s], ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);
            await _service.ExecuteAsync(context, argPos, _provider);

            var embed = new EmbedBuilder()
                .WithAuthor(socketMessage.Author.Username)
                .WithTitle($"Channel {socketMessage.Channel.Name}")
                .WithDescription(socketMessage.Content)
                .Build();
            await _client.GetGuild(Statics.DebugGuildId).GetTextChannel(Statics.DebugLogsChannelId)
                .SendMessageAsync(embed: embed);
        }
    }
}
