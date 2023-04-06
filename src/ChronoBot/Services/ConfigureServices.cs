using ChronoBot.Common.Systems;
using ChronoBot.Helpers;
using ChronoBot.Utilities.Games;
using ChronoBot.Utilities.SocialMedias;
using ChronoBot.Utilities.Tools;
using ChronoBot.Utilities.Tools.Deadlines;
using Discord.Interactions;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.DependencyInjection;
using TweetSharp;

namespace ChronoBot.Services
{
    public static class ConfigureServices
    {
        public static ServiceProvider RegisterServices()
        {
            return new ServiceCollection()
                .AddBaseServiceCollection()
                .AddToolServiceCollection()
                .AddSocialMediaServiceCollection()
                .AddGamesServiceCollection()
                .BuildServiceProvider();
        }

        private static IServiceCollection AddBaseServiceCollection(this IServiceCollection services)
        {
            return services
                .AddSingleton(Statics.Config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>();
        }

        private static IServiceCollection AddToolServiceCollection(this IServiceCollection services)
        {
            return services
                .AddSingleton<Calculator>()
                .AddDeadlineServiceCollection();
        }
        private static IServiceCollection AddDeadlineServiceCollection(this IServiceCollection services)
        {
            return services
                .AddSingleton<Deadline>()
                .AddSingleton<DeadlineFileSystem>()
                .AddSingleton<Reminder>()
                .AddSingleton<Countdown>()
                .AddSingleton<Repeater>();
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
                .AddSingleton<ChronoTwitch.ChronoTwitch>()
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
