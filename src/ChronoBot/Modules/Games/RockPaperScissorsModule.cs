using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Utilities.Games;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.Games
{
    public class RockPaperScissorsModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<RockPaperScissorsModule> _logger;
        private readonly RockPaperScissors _rps;

        public RockPaperScissorsModule(ILogger<RockPaperScissorsModule> logger, RockPaperScissors rps)
        {
            _logger = logger;
            _rps = rps;
        }

        [Command("rps"), Alias("rockpaperscissors")]
        public async Task PlayAsync(RpsActors actor, string otherAction = "", ulong user = 0)
        {
            string result = await _rps.ProcessCommand(actor, user, Context.Message.Content.Contains("|"))
        }
        private async Task SendMessage(string result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(result);
            else
                await ReplyAsync(result);
        }
        private async Task SendMessage(Embed result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(result);
            else
                await ReplyAsync(embed: result);
        }
        private async Task SendFile(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(result, thumbnail);
            else
                await Context.Channel.SendFileAsync(thumbnail, embed: result);
        }
    }
}
