using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    public class TwitchModule : SocialMediaModule
    {
        public TwitchModule(DiscordSocketClient client, Twitch socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.Twitch;
        }

        [SlashCommand("twitch", "Notifies when a Twitch streamer is broadcasting.", runMode: RunMode.Async)]
        public override Task HandleOptions(Options option, string user, [ChannelTypes(new[] { ChannelType.Text })] IChannel channel = null)
        {
            return base.HandleOptions(option, user, channel);
        }
    }
}
