using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Helpers;
using ChronoBot.Utilities.Games;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.Games
{
    public class RockPaperScissorsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly RockPaperScissors _rps;

        public RockPaperScissorsModule(DiscordSocketClient client, RockPaperScissors rps)
        {
            _client = client;
            _rps = rps;
        }

        [SlashCommand("rock-paper-scissors", "Play Rock-Paper-Scissors", runMode: RunMode.Async)]
        public async Task PlayAsync(
            [Choice("Rock", "r")]
            [Choice("Paper", "P")]
            [Choice("Scissors", "s")] string actor,
            [Choice("VS", "")] IUser vsUser)
        {
            RpsPlayData playData = CreatePlayData(Context.User.Id, actor, Context.User.Mention,
                Context.User.Username, Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());

            RpsPlayData? mentionData = null;
            var message = await GetOriginalResponseAsync();
            if (vsUser != null && vsUser != _client.CurrentUser)
            {
                mentionData = CreatePlayData(mentionUser.Id, actor, mentionUser.Mention, mentionUser.Username,
                    mentionUser.GetAvatarUrl() ?? mentionUser.GetDefaultAvatarUrl());
            }

            var result = _rps.Play(playData, mentionData);
            if (result.Description.Contains("Wrong input"))
                await SendMessage(result.Description);
            else
                await SendMessage(result);
        }

        [SlashCommand("rps-options", "Rock-Paper-Scissors options", runMode: RunMode.Async)]
        public async Task OptionsAsync([Choice("SeeStats", "s")][Choice("ResetStats", "r")] string option)
        {
            var result = _rps.Options(CreatePlayData(Context.User.Id, action,
                Context.User.Mention, Context.User.Username, 
                Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()));

            if (result.Description != null && result.Description.Contains("Wrong input"))
                await SendMessage(result);
            else
                await SendMessage(result);
        }

        private async Task SendMessage(string result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(_client, result);
            else
                await RespondAsync(result);
        }
        private async Task SendMessage(Embed result)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(_client, result);
            else
                await RespondAsync(embed: result);
        }
        private async Task SendFile(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(_client, result, thumbnail);
            else
                await RespondWithFileAsync(thumbnail, embed: result);
        }

        private RpsPlayData CreatePlayData(ulong userId, string input, string mention, string username, string thumbnailIcon)
        {
            return new RpsPlayData()
            {
                UserId = userId,
                ChannelId = Context.Channel.Id,
                GuildId = Context.Guild.Id,
                Input = input,
                Mention = mention,
                Username = username,
                ThumbnailIconUrl = thumbnailIcon
            };
        }
    }
}