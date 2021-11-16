using ChronoBot.Common.Systems;
using ChronoBot.Utilities.Games;
using ChronoBot.Utilities.SocialMedias;
using ChronoBot.Utilities.Tools;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.DependencyInjection;
using TweetSharp;
using TwitchLib.Api;

namespace ChronoBot.Services
{
    public static class ConfigureServices
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddHostedService<CommandHandler>()
                .AddToolServiceCollection()
                .AddSocialMediaServiceCollection()
                .AddGamesServiceCollection();
        }

        private static IServiceCollection AddToolServiceCollection(this IServiceCollection services)
        {
            return services
                .AddSingleton<Calculator>();
        }

        private static IServiceCollection AddSocialMediaServiceCollection(this IServiceCollection services)
        {
            return services
                .AddSingleton<SocialMedia>()
                .AddSingleton<SocialMediaFileSystem>()
                .AddSingleton<TwitterService>()
                .AddSingleton<Twitter>()
                .AddSingleton<YouTubeService>()
                .AddSingleton<YouTube>()
                .AddSingleton<TwitchAPI>()
                .AddSingleton<Twitch>();
        }

        private static IServiceCollection AddGamesServiceCollection(this IServiceCollection services)
        {
            return services
                .AddSingleton<RpsFileSystem>()
                .AddSingleton<RockPaperScissors>();
        }
    }
}
