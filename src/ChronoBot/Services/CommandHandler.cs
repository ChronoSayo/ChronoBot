using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ChronoBot.Common;
using ChronoBot.Helpers;
using Discord;
using Discord.Interactions;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using ChronoBot.Utilities.Tools;
using ChronoBot.Modules.Tools;

namespace ChronoBot.Services
{
    public class CommandHandler
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _command;
        private readonly IConfiguration _config;
        private readonly Calculator _calculator;
        private System.Timers.Timer _timer;

        public CommandHandler(DiscordSocketClient client, IServiceProvider provider, InteractionService service, IConfiguration config,
            Calculator calculator)
        {
            _client = client;
            _provider = provider;
            _command = service;
            _config = config;
            _calculator = calculator;

            _timer = new System.Timers.Timer()
            {
                AutoReset = false,
                Enabled = true,
                Interval = 3000
            };
            _timer.Elapsed += AddCommandsTimer;
        }

        private async void AddCommandsTimer(object sender, ElapsedEventArgs e)
        {
            var guildCommand = new SlashCommandBuilder()
            .WithName("ffafsf")
            .WithDescription("half the number")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("num")
                .WithType(ApplicationCommandOptionType.Number)
                .WithDescription("add num")
                .WithRequired(true)
                .AddChoice("Terrible", 1)
                .AddChoice("Meh", 2)
                .AddChoice("Good", 3)
                .AddChoice("Lovely", 4)
                .AddChoice("Excellent!", 5));

            var commands = await _client.GetGlobalApplicationCommandsAsync();
            foreach (var c in commands)
            {
                if (c.Name != "half")
                    await _client.GetGuild(Statics.DebugGuildId).CreateApplicationCommandAsync(guildCommand.Build());
            }
        }

        public async Task InitializeAsync()
        {
            await _command.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);

            _client.InteractionCreated += HandleInteraction;
            _client.SlashCommandExecuted += SlashCommandExecuted;
        }

        private async Task SlashCommandExecuted(SocketSlashCommand command)
        {
            switch(command.Data.Name)
            {
                case "calculator":
                    break;
                case "half":
                    await _calculator.ResultAsync(command);
                    break;
            }
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, arg);
                await _command.ExecuteCommandAsync(ctx, _provider);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // if a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                {
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }
        }
    }
}
