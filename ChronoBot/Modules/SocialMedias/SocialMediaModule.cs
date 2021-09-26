using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ChronoBot.Modules.Tools;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.SocialMedias
{
    public class SocialMediaModule : ModuleBase<SocketCommandContext>
    {

        private readonly ILogger<SocialMediaModule> _logger;
        protected SocialMedia SocialMedia;

        public SocialMediaModule(ILogger<SocialMediaModule> logger, SocialMedia socialMedia)
        {
            _logger = logger;
            SocialMedia = socialMedia;
        }

        public virtual async Task AddAsync(string user, ulong channel = 0)
        {
            await Task.CompletedTask;
        }

        public virtual async Task DeleteAsync(string user)
        {
            await Task.CompletedTask;
        }

        public virtual async Task GetAsync(string user)
        {
            await Task.CompletedTask;
        }

        public virtual async Task ListAsync()
        {
            await Task.CompletedTask;
        }

        public virtual async Task HowToUseAsync()
        {
            await Task.CompletedTask;
        }

        protected virtual Embed HowToText(string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources/Images/SocialMedia/{socialMedia}.png");
            return new EmbedBuilder()
                .WithTitle($"How to use {socialMedia.ToUpper()}")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/891627208089698384/891627590023000074/Twitter_social_icons_-_circle_-_blue.png")
                .AddField("Add ", $"{Statics.CommandPrefix}{socialMedia.ToLowerInvariant()}add <name> [channel]", true)
                .AddField("Delete", $"{Statics.CommandPrefix}{socialMedia.ToLowerInvariant()}delete <name>", true)
                .AddField("Get", $"{Statics.CommandPrefix}{socialMedia.ToLowerInvariant()}get <name>", true)
                .AddField("List", $"{Statics.CommandPrefix}{socialMedia.ToLowerInvariant()}list", true)
                .Build();
        }

        protected virtual async Task SendMessage(string result)
        {
            if (Statics.DEBUG)
                await Statics.DebugSendMessageToChannel(result);
            else
                await ReplyAsync(result);
        }
        protected virtual async Task SendMessage(Embed result)
        {
            if (Statics.DEBUG)
                await Statics.DebugSendMessageToChannel(result);
            else
                await ReplyAsync(embed: result);
        }
        protected virtual async Task SendFile(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.DEBUG)
                await Statics.DebugSendFileToChannel(result, thumbnail);
            else
                await Context.Channel.SendFileAsync(thumbnail, embed: result);
        }
    }
}
