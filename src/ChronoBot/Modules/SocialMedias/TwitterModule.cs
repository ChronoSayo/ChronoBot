using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    [Group("twitter", "Notifies when a Tweeter has tweeted.")]
    public class TwitterModule : SocialMediaModule
    {
        public TwitterModule(DiscordSocketClient client, Twitter socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.Twitter;
        }

        [SlashCommand("twitter-options", "Choose an option on how to handle Tweeter.", runMode: RunMode.Async)]
        public override Task AddTwitterUser(string user, IChannel channel = null, string filter = "")
        {
            return base.AddTwitterUser(user, channel, filter);
        }

        [SlashCommand("show-twitter-video", "Use this if embedded video didn't work from the tweet.", runMode: RunMode.Async)]
        public async Task PostVideoAsync()
        {
            var message = await GetOriginalResponseAsync();
            if(message == null)
            {
                await SendMessage(Client, "No Twitter video discovered.");
                return;
            }

            string video =
                await ((Twitter) SocialMedia).PostVideo(Context.Guild.Id, Context.Channel.Id, message.Source.ToString());
            if(!string.IsNullOrEmpty(video))
                await SendMessage(Client, video);
        }
    }
}
