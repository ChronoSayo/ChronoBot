using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.SocialMedias
{
    public class TwitterModule : SocialMediaModule
    {
        private readonly ILogger<SocialMediaModule> _logger;
        private const string SocialMediaCommand = "twitter";

        public TwitterModule(DiscordSocketClient client, ILogger<SocialMediaModule> logger, Twitter socialMedia) : base(client, logger, socialMedia)
        {
            _logger = logger;
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.Twitter;
        }

        [SlashCommand(SocialMediaCommand, "Gets a user's Twitter activity.", runMode: RunMode.Async)]
        public override Task ActionTwitter(Actions actions, 
            string user, 
            [Choice("Posts", "Shows only posts from user.")] string posts, 
            [Choice("Retweets", "Shows only retweets from user.")] string retweets, 
            [Choice("Likes", "Shows only likes from user.")] string likes, 
            [Choice("QuoteTweets", "Shows only quote tweets from user.")] string quoteTweets, 
            [Choice("AllMedia", "Shows all media from user.")] string allMedia, 
            [Choice("Pictures", "Shows only pictures from user.")] string pictures, 
            [Choice("GIF", "Shows only GIF's from user.")] string gifs, 
            [Choice("Video", "Shows only videos from user.")] string videos, 
            [Choice("All", "Shows everything.")] string all)
        {
            return base.ActionTwitter(actions, user, posts, retweets, likes, quoteTweets, allMedia, pictures, gifs, videos, all);
        }

        [SlashCommand("Show video", "Use this if embedded video didn't work from the tweet.", runMode: RunMode.Async)]
        public async Task PostVideoAsync()
        {
            string video =
                await ((Twitter) SocialMedia).PostVideo(Context.Guild.Id, Context.Channel.Id,
                    Context.Message.ToString());
            if(!string.IsNullOrEmpty(video))
                await SendMessage(video);
        }

        //[Command("e", true, RunMode = RunMode.Async)]
        //public async Task PostEmbedAsync()
        //{
        //    Embed embed =
        //        await ((Twitter)SocialMedia).PostEmbed(Context.Guild.Id, Context.Channel.Id,
        //            Context.Message.ToString());
        //    if (embed != null)
        //    {
        //        if (!embed.Author.HasValue)
        //            await SendMessage(embed.Description);
        //        else
        //        {
        //            await SendMessage(embed);
        //            string video =
        //                await ((Twitter)SocialMedia).PostVideo(Context.Guild.Id, Context.Channel.Id,
        //                    embed.Fields.ToList().Find(x => x.Name == "Twitter").Value);
        //            if (!string.IsNullOrEmpty(video))
        //                await SendMessage(video);
        //        }
        //    }
        //}
    }
}
