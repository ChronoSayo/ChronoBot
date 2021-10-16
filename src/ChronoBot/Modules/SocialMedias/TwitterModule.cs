using System.Threading.Tasks;
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
        }

        [Command(SocialMediaCommand + "add", RunMode = RunMode.Async)]
        public override async Task AddAsync(string user, ulong channel = 0)
        {
            ulong guildId = Context.Guild.Id;
            ulong channelId = Context.Channel.Id;
            string result = await SocialMedia.AddSocialMediaUser(guildId, channelId, user, channel);
            await SendMessage(result);
        }

        [Command(SocialMediaCommand + "delete", RunMode = RunMode.Async), Alias(SocialMediaCommand + "del", SocialMediaCommand + "remove")]
        public override async Task DeleteAsync(string user)
        {
            ulong guildId = Context.Guild.Id;
            string result = SocialMedia.DeleteSocialMediaUser(guildId, user);
            await SendMessage(result);
        }

        [Command(SocialMediaCommand + "get", RunMode = RunMode.Async), Alias(SocialMediaCommand)]
        public override async Task GetAsync(string user)
        {
            string result = await SocialMedia.GetSocialMediaUser(Context, user);
            await SendMessage(result);
        }

        [Command(SocialMediaCommand + "list", RunMode = RunMode.Async), Alias(SocialMediaCommand + "all")]
        public override async Task ListAsync()
        {
            string result = await SocialMedia.ListSavedSocialMediaUsers(Context);
            await SendMessage(result);
        }

        [Command(SocialMediaCommand + "?", RunMode = RunMode.Async), Alias(SocialMediaCommand + "howto", SocialMediaCommand)]
        public override async Task HowToUseAsync()
        {
            var embed = HowToText(SocialMediaCommand);
            await SendMessage(embed);
        }
    }
}
