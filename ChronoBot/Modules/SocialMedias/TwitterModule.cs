using System.Threading.Tasks;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.SocialMedias
{
    public class TwitterModule : SocialMediaModule
    {
        private readonly ILogger<SocialMediaModule> _logger;
        private const string SocialMediaCommand = "twitter";

        public TwitterModule(ILogger<SocialMediaModule> logger, Twitter socialMedia) : base(logger, socialMedia)
        {
            _logger = logger;
            SocialMedia = socialMedia;
        }

        [Command(SocialMediaCommand + "add")]
        public override async Task AddAsync(string user, ulong channel = 0)
        {
            string result = await SocialMedia.AddSocialMediaUser(Context, user, channel);
            await ReplyAsync(result);
        }

        [Command(SocialMediaCommand + "delete")]
        [Alias(SocialMediaCommand + "del", SocialMediaCommand + "remove")]
        public override async Task DeleteAsync(string user)
        {
            ulong guildId = Context.Guild.Id;
            string result = await SocialMedia.DeleteSocialMediaUser(guildId, user);
            await SendMessage(result);
        }

        [Command(SocialMediaCommand + "get")]
        [Alias(SocialMediaCommand)]
        public override async Task GetAsync(string user)
        {
            string result = await SocialMedia.GetSocialMediaUser(Context, user);
            await SendMessage(result);
        }

        [Command(SocialMediaCommand + "list")]
        [Alias(SocialMediaCommand + "all")]
        public override async Task ListAsync()
        {
            string result = await SocialMedia.ListSavedSocialMediaUsers(Context);
            await SendMessage(result);
        }

        [Command(SocialMediaCommand + "?")]
        [Alias(SocialMediaCommand + "howto", SocialMediaCommand)]
        public override async Task HowToUseAsync()
        {
            var embed = HowToText(SocialMediaCommand);
            await SendMessage(embed);
        }

        protected override async Task SendMessage(string result)
        {
            if (Statics.DEBUG)
                await Statics.DebugSendMessageToChannel(result);
            else
                await ReplyAsync(result);
        }

        protected override async Task SendMessage(Embed result)
        {
            if (Statics.DEBUG)
                await Statics.DebugSendMessageToChannel(result);
            else
                await ReplyAsync(embed: result);
        }
    }
}
