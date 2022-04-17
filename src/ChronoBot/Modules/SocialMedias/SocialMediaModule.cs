using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.SocialMedias
{
    public class SocialMediaModule : ModuleBase<SocketCommandContext>
    {
        protected readonly DiscordSocketClient Client;
        private readonly ILogger<SocialMediaModule> _logger;
        protected SocialMedia SocialMedia;
        protected SocialMediaEnum SocialMediaType;

        public SocialMediaModule(DiscordSocketClient client, ILogger<SocialMediaModule> logger, SocialMedia socialMedia)
        {
            Client = client;
            _logger = logger;
            SocialMedia = socialMedia;
        }

        public virtual async Task AddAsync(string user, [Remainder] string option = "")
        {
            ulong guildId = Context.Guild.Id;
            ulong channelId = Context.Channel.Id;
            ulong sendToChannel = Context.Message.MentionedChannels.Count > 0 ? Context.Message.MentionedChannels.FirstOrDefault()!.Id : 0;
            string result = await SocialMedia.AddSocialMediaUser(guildId, channelId, user, sendToChannel, option);
            await SendMessage(result, sendToChannel);
        }

        public virtual async Task DeleteAsync(string user)
        {
            ulong guildId = Context.Guild.Id;
            string result = SocialMedia.DeleteSocialMediaUser(guildId, user, SocialMediaType);
            await SendMessage(result);
        }

        public virtual async Task GetAsync(string user)
        {
            string result = await SocialMedia.GetSocialMediaUser(Context.Guild.Id, Context.Channel.Id, user);
            await SendMessage(result);
        }

        public virtual async Task ListAsync()
        {
            string result = await SocialMedia.ListSavedSocialMediaUsers(Context.Guild.Id, SocialMediaType, Context.Message.MentionedChannels.ElementAt(0).ToString());
            var embed = new EmbedBuilder()
                .WithDescription(result)
                .Build();
            await SendMessage(embed);
        }

        public virtual async Task UpdateAsync()
        {
            string result = await SocialMedia.GetUpdatedSocialMediaUsers(Context.Guild.Id);
            await SendMessage(result);
        }

        public virtual async Task HowToUseAsync()
        {
            await Task.CompletedTask;
        }

        protected virtual Embed HowToText(string socialMedia)
        {
            string urlIcon = "";
            switch (socialMedia)
            {
                case "twitter":
                    urlIcon =
                        "https://cdn.discordapp.com/attachments/891627208089698384/891627590023000074/Twitter_social_icons_-_circle_-_blue.png";
                    break;
                case "youtube":
                    urlIcon =
                        "https://cdn.discordapp.com/attachments/891627208089698384/905575565636010074/youtube-logo-transparent-png-pictures-transparent-background-youtube-logo-11562856729oa42buzkng.png";
                    break;
            }
            return new EmbedBuilder()
                .WithTitle($"How to use {socialMedia.ToUpper()}")
                .WithThumbnailUrl(urlIcon)
                .AddField("Add ", $"{Statics.Prefix}{socialMedia.ToLowerInvariant()}add <name> [channel]", true)
                .AddField("Delete", $"{Statics.Prefix}{socialMedia.ToLowerInvariant()}delete <name>", true)
                .AddField("Get", $"{Statics.Prefix}{socialMedia.ToLowerInvariant()}get <name>", true)
                .AddField("List", $"{Statics.Prefix}{socialMedia.ToLowerInvariant()}list", true)
                .Build();
        }

        protected virtual async Task SendMessage(string result, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(Client, result);
            else if (sendToChannel != 0)
                await Client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(result);
            else
                await ReplyAsync(result);
        }
        protected virtual async Task SendMessage(Embed result, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(Client, result);
            else if (sendToChannel != 0)
                await Client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(embed: result);
            else
                await ReplyAsync(embed: result);
        }
        protected virtual async Task SendFile(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(Client, result, thumbnail);
            else
                await Context.Channel.SendFileAsync(thumbnail, embed: result);
        }
    }
}
