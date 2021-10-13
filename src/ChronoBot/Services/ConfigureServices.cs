using ChronoBot.Common.Systems;
using ChronoBot.Utilities.Games;
using ChronoBot.Utilities.SocialMedias;
using ChronoBot.Utilities.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace ChronoBot.Services
{
    public static class ConfigureServices
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddHostedService<CommandHandler>()
                .AddSingleton<Calculator>()
                .AddSingleton<SocialMedia>()
                .AddSingleton<SocialMediaFileSystem>()
                .AddSingleton<Twitter>()
                .AddSingleton<RockPaperScissors>()
                .AddSingleton<RpsFileSystem>();
        }
    }
}
