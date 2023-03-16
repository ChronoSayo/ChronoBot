using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    public class TwitterModule : SocialMediaModule
    {
        public TwitterModule(DiscordSocketClient client, Twitter socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.Twitter;
        }

        [SlashCommand("twitter", "Gets a user's Twitter activity.", runMode: RunMode.Async)]
        public override Task HandleTwitterOption(Options option, 
            string user, 
            [ChannelTypes(ChannelType.Text)] IChannel channel = null,
            [Choice("Posts", "p")]
            [Choice("Retweets", "r")]
            [Choice("Likes", "l")]
            [Choice("QuoteTweets", "q")]
            [Choice("AllMedia", "m")]
            [Choice("Pictures", "mp")]
            [Choice("GIF", "mg")]
            [Choice("Video", "mv")]
            [Choice("All", "")] string filter = "")
        {
            return base.HandleTwitterOption(option, user, channel, filter);
        }

        [SlashCommand("show-twitter-video", "Use this if embedded video didn't work from the tweet.", runMode: RunMode.Async)]
        public async Task PostVideoAsync()
        {
            var message = await GetOriginalResponseAsync();
            string video =
                await ((Twitter) SocialMedia).PostVideo(Context.Guild.Id, Context.Channel.Id, message.Source.ToString());
            if(!string.IsNullOrEmpty(video))
                await SendMessage(Client, video);
        }
    }
}
