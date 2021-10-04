using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Helpers;
using ChronoBot.Services;
using ChronoBot.Utilities.SocialMedias;
using ChronoBot.Utilities.Tools;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChronoBot
{
    class Program
    {
        static async Task Main()
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", false, true)
                        .Build();

                    x.AddConfiguration(configuration);
                })
                .ConfigureLogging(x =>
                {
                    x.AddConsole();
                    x.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureDiscordHost((context, config) =>
                {
                    config.SocketConfig = new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Debug,
                        AlwaysDownloadUsers = true,
                        MessageCacheSize = 200
                    };

                    var s = context.Properties;
                    Statics.Config = context.Configuration;
                    config.Token = context.Configuration[Statics.DiscordToken];
                })
                .UseCommandService((context, config) =>
                {
                    config.CaseSensitiveCommands = false;
                    config.LogLevel = LogSeverity.Debug;
                    config.DefaultRunMode = RunMode.Async;
                })
                .ConfigureServices((context, services) =>
                {
                    var s = services.First(x => x.ServiceType.Name == "DiscordSocketClient");
                    var g = s.ServiceType;
                    //Statics.DiscordClient = 
                    services
                        .AddHostedService<CommandHandler>()
                        .AddSingleton<Calculator>()
                        .AddSingleton<SocialMedia>()
                        .AddSingleton<Twitter>();
                })
                .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
