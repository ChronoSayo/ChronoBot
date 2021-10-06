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
        public async Task PlayAsync([Remainder] string action)
        {
            string[] splitAction = action.Split(' ');
            //TODO check if rps class can detect actor from input.
            //if (!Enum.TryParse<RpsActors>(splitAction[0], out var actor))
            {
                string result = await _rps.OtherCommands(action, Context.User.Id, Context.Channel.Id, Context.Guild.Id);
                if (result.Contains("Wrong input"))
                    await SendMessage(result);
                else
                {
                    var embed = new EmbedBuilder()
                        .WithDescription(result);
                    await SendMessage(embed.Build());
                }
            }
            else
            {
                //TODO send in mention id
                await _rps.PlayAsync(actor, Context.User.Id, mention, Context.Channel.Id, Context.Guild.Id,
                    Context.Message.Content.Contains("|"));
            }
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
