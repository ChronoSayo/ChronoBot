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
    }
}
