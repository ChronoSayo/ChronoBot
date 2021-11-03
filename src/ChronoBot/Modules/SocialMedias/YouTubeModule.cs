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
    public class YouTubeModule : SocialMediaModule
    {
        private readonly ILogger<SocialMediaModule> _logger;
        private const string SocialMediaCommand = "youtube";
        private const string AltSocialMediaCommand = "yt";

        public YouTubeModule(DiscordSocketClient client, ILogger<SocialMediaModule> logger, YouTube socialMedia) : base(client, logger, socialMedia)
        {
            _logger = logger;
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.YouTube;
        }

        [Command(SocialMediaCommand + "add", RunMode = RunMode.Async), Alias(AltSocialMediaCommand + "add")]
        public override async Task AddAsync(string user, ulong channel = 0)
        {
            await base.AddAsync(user, channel);
        }

        [Command(SocialMediaCommand + "delete", RunMode = RunMode.Async),
         Alias(SocialMediaCommand + "del", SocialMediaCommand + "remove", AltSocialMediaCommand + "delete",
             AltSocialMediaCommand + "del", AltSocialMediaCommand + "remove")]
        public override async Task DeleteAsync(string user)
        {
            await base.DeleteAsync(user);
        }

        [Command(SocialMediaCommand + "get", RunMode = RunMode.Async), 
         Alias(SocialMediaCommand, AltSocialMediaCommand, AltSocialMediaCommand + "get")]
        public override async Task GetAsync(string user)
        {
            await base.GetAsync(user);
        }

        [Command(SocialMediaCommand + "list", RunMode = RunMode.Async), 
         Alias(SocialMediaCommand + "all", AltSocialMediaCommand + "list", AltSocialMediaCommand + "all")]
        public override async Task ListAsync()
        {
            await base.ListAsync();
        }


        [Command(SocialMediaCommand + "update", RunMode = RunMode.Async), 
         Alias(SocialMediaCommand + "latest", AltSocialMediaCommand + "update", AltSocialMediaCommand + "latest")]
        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();
        }

        [Command(SocialMediaCommand + "?", RunMode = RunMode.Async), Alias(SocialMediaCommand + "howto", SocialMediaCommand, AltSocialMediaCommand)]
        public override async Task HowToUseAsync()
        {
            var embed = HowToText(SocialMediaCommand);
            await SendMessage(embed);
        }
    }
}
