using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Helpers;
using ChronoBot.Utilities.Games;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.Games
{
    public class RockPaperScissorsModule : ChronoInteractionModuleBase
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
                mentionData = CreatePlayData(vsUser.Id, actor, vsUser.Mention, vsUser.Username,
                    vsUser.GetAvatarUrl() ?? vsUser.GetDefaultAvatarUrl());
            }

            var result = _rps.Play(playData, mentionData);
            if (result.Description.Contains("Wrong input"))
                await SendMessage(_client, result.Description);
            else
                await SendMessage(_client, result);
        }

        [SlashCommand("rps-options", "Rock-Paper-Scissors options", runMode: RunMode.Async)]
        public async Task OptionsAsync([Choice("SeeStats", "s")][Choice("ResetStats", "r")] string option)
        {
            var result = _rps.Options(CreatePlayData(Context.User.Id, option,
                Context.User.Mention, Context.User.Username, 
                Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()));

            await SendMessage(_client, result);
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