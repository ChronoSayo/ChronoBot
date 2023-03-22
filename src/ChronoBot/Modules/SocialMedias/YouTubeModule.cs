using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    [Group("youtube", "Notifies when a YouTuber has uploaded a video.")]
    public class YouTubeModule : SocialMediaModule
    {
        public YouTubeModule(DiscordSocketClient client, YouTube socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.YouTube;
        }

        [SlashCommand("youtube", "Notifies when a YouTuber uploads a video.", runMode: RunMode.Async)]
        public override Task SetOptions(Options option,
            [Summary("YouTuber", "Insert YouTuber's channel.")] string user,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
                [ChannelTypes(new[] { ChannelType.Text })] IChannel channel = null)
        {
            return base.SetOptions(option, user, channel);
        }
    }
}
