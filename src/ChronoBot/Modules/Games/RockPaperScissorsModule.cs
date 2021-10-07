using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Utilities.Games;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.Games
{
    public class RockPaperScissorsModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<RockPaperScissorsModule> _logger;
        private readonly RockPaperScissors _rps;

        public RockPaperScissorsModule(DiscordSocketClient client, ILogger<RockPaperScissorsModule> logger, RockPaperScissors rps)
        {
            _client = client;
            _logger = logger;
            _rps = rps;
        }

        [Command("rps")]
        public async Task PlayAsync(string actor, string mention = null)
        {
            if (Context.Message.Content.Contains("|"))
                actor = actor.Replace("|", "");

            ulong mentionId = 0;
            if (mention != null && Context.Message.MentionedUsers.Count > 0)
                mentionId = Context.Message.MentionedUsers.ElementAt(0).Id;

            var result = _rps.Play(actor, Context.User.Id, mentionId, Context.Channel.Id, Context.Guild.Id);
            if (result.Description.Contains("Wrong input"))
                await SendMessage(result.Description);
            else
            {
                await _client.GetGuild(Context.Guild.Id).GetTextChannel(Context.Channel.Id)
                    .DeleteMessageAsync(Context.Message);
                await SendMessage(result);
            }
        }

        [Command("rpso")]
        public async Task OptionsAsync([Remainder] string action)
        {
            string result = _rps.Options(action, Context.User.Id, Context.Channel.Id, Context.Guild.Id);
            if (result.Contains("Wrong input"))
                await SendMessage(result);
            else
            {
                var embed = new EmbedBuilder()
                    .WithDescription(result);
                await SendMessage(embed.Build());
            }
        }

        private async Task SendMessage(string result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(_client, result);
            else
                await ReplyAsync(result);
        }
        private async Task SendMessage(Embed result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(_client, result);
            else
                await ReplyAsync(embed: result);
        }
        private async Task SendFile(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(_client, result, thumbnail);
            else
                await Context.Channel.SendFileAsync(thumbnail, embed: result);
        }
    }
}
