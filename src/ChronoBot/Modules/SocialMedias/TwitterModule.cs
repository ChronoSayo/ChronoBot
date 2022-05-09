using System.Collections.Immutable;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Commands;
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

        [Command(SocialMediaCommand + "add", RunMode = RunMode.Async)]
        public override async Task AddAsync(string user, [Remainder] string option = "")
        {
            await base.AddAsync(user, option);
        }

        [Command(SocialMediaCommand + "delete", RunMode = RunMode.Async), Alias(SocialMediaCommand + "del", SocialMediaCommand + "remove")]
        public override async Task DeleteAsync(string user)
        {
            await base.DeleteAsync(user);
        }

        [Command(SocialMediaCommand + "get", RunMode = RunMode.Async), Alias(SocialMediaCommand)]
        public override async Task GetAsync(string user)
        {
            await base.GetAsync(user);
        }

        [Command(SocialMediaCommand + "list", RunMode = RunMode.Async), Alias(SocialMediaCommand + "all")]
        public override async Task ListAsync()
        {
            await base.ListAsync();
        }

        [Command(SocialMediaCommand + "update", RunMode = RunMode.Async), Alias(SocialMediaCommand + "latest")]
        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();
        }

        [Command(SocialMediaCommand + "?", RunMode = RunMode.Async), Alias(SocialMediaCommand + "howto", SocialMediaCommand)]
        public override async Task HowToUseAsync()
        {
            var embed = HowToText(SocialMediaCommand);
            await SendMessage(embed);
        }

        [Command("v", true, RunMode = RunMode.Async)]
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
