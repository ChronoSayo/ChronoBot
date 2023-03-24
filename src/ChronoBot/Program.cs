using System;
using System.Threading.Tasks;
using System.Threading;
using ChronoBot.Helpers;
using ChronoBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot
{
    class Program
    {
        private readonly IConfiguration _config;
        private DiscordSocketClient _client;
        private InteractionService _commands;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync(string[] args)
        {

        }

        public Program()
        {
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "appsettings.json");

            _config = _builder.Build();
            Statics.Config = _config;
        }

        public async Task MainAsync()
        {
            using (var services = GetConfigureServices())
            {
                _client = services.GetRequiredService<DiscordSocketClient>();
                _commands = services.GetRequiredService<InteractionService>();

                _client.Log += LogAsync;
                _commands.Log += LogAsync;
                _client.Ready += ReadyAsync;

                var f = _config[Statics.DiscordToken];

                await _client.LoginAsync(TokenType.Bot, _config[Statics.DiscordToken]);
                await _client.StartAsync();

                await services.GetRequiredService<CommandHandler>().InitializeAsync();

                await Task.Delay(Timeout.Infinite);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            await _commands.RegisterCommandsGloballyAsync(true);
            Console.WriteLine($"Connected as {_client.CurrentUser.Username}");
        }

        private ServiceProvider GetConfigureServices()
        {
            return ConfigureServices.RegisterServices();
        }
    }
}
