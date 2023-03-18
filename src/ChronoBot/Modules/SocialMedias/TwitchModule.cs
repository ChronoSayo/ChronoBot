using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    [Group("twitch", "Notifies when a Twitch streamer is broadcasting.")]
    public class TwitchModule : SocialMediaModule
    {
        public TwitchModule(DiscordSocketClient client, Twitch socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.Twitch;
        }

        [SlashCommand("twitch-options", "Choose an option on how to handle streamer.", runMode: RunMode.Async)]
        public override Task HandleOptions(Options option,
            [Summary("Streamer", "Insert streamer's name.")] string user,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
                [ChannelTypes(new[] { ChannelType.Text })] IChannel channel = null)
        {
            return base.HandleOptions(option, user, channel);
        }
    }
}
