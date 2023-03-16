using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    public class YouTubeModule : SocialMediaModule
    {
        public YouTubeModule(DiscordSocketClient client, YouTube socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.YouTube;
        }

        [SlashCommand("youtube", "Notifies when a YouTuber uploads a video.", runMode: RunMode.Async)]
        public override Task HandleOptions(Options option, string user, [ChannelTypes(new[] { ChannelType.Text })] IChannel channel = null)
        {
            return base.HandleOptions(option, user, channel);
        }
    }
}
