using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ChronoBot.Modules.Tools;
using ChronoBot.Utilities.SocialMedias;
using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.SocialMedias
{
    public class SocialMediaModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<SocialMediaModule> _logger;
        private readonly Twitter _twitter;

        public SocialMediaModule(ILogger<SocialMediaModule> logger, Twitter twitter)
        {
            _logger = logger;
            _twitter = twitter;
        }

        [Command("twitter")]
        [Alias("twit")]
        public async Task TwitterTestAsync()
        {
            string s = await _twitter.Test();
            await ReplyAsync(s);
        }
    }
}
