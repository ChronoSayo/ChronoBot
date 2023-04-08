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

        [SlashCommand("youtube-add", "Adds YouTuber to the list of updates.", runMode: RunMode.Async)]
        public override Task AddSocialMediaUser(
            [Summary("Youtuber", "Insert YouTuber's name.")] string user,
            [Summary("Where", "To which channel should this be updated to. Default is this channel.")]
                [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            return base.AddSocialMediaUser(user, channel);
        }

        [SlashCommand("youtube-delete", "Deletes YouTuber to the list of updates.", runMode: RunMode.Async)]
        public override Task DeleteSocialMediaUser(
            [Summary("Youtuber", "Insert YouTuber's name.")] string user)
        {
            return base.DeleteSocialMediaUser(user);
        }
    }
}
