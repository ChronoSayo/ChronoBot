﻿using System;
using System.IO;
using System.Threading.Tasks;
using ChronoBot.Common;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    public class SocialMediaModule : ChronoInteractionModuleBase
    {
        protected readonly DiscordSocketClient Client;
        protected SocialMedia SocialMedia;
        protected SocialMediaEnum SocialMediaType;

        public enum Options
        {
            [ChoiceDisplay("Add")] Add, Delete, Get, List, Update
        }

        public SocialMediaModule(DiscordSocketClient client, SocialMedia socialMedia)
        {
            Client = client;
            SocialMedia = socialMedia;
        }

        public virtual async Task SetOptions(Options option, string user, [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            await HandleOption(option, user, channel);
        }

        public virtual async Task SetTwitterOption(Options option,
            [Summary("Tweeter", "Insert Twitter handle.")] string user,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
                [ChannelTypes(ChannelType.Text)] IChannel channel = null,
            [Summary("Filter", "Choose which the bot should filter the Tweeter's posts by.")] [Choice("Posts", "p")]
            [Choice("Retweets", "r")]
            [Choice("Likes", "l")]
            [Choice("QuoteTweets", "q")]
            [Choice("AllMedia", "m")]
            [Choice("Pictures", "mp")]
            [Choice("GIF", "mg")]
            [Choice("Video", "mv")]
            [Choice("All", "")] string filter = "")
        {
            await HandleOption(option, user, channel, filter);
        }

        protected virtual async Task SendFileWithLogo(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(Client, result, thumbnail);
            else
                await RespondWithFileAsync(thumbnail, embed: result);
        }

        private async Task HandleOption(Options option, string user, [ChannelTypes(ChannelType.Text)] IChannel channel = null, string filter = "")
        {
            ulong guildId = Context.Guild.Id;
            ulong channelId = Context.Channel.Id;
            ulong sendToChannel = channel?.Id ?? channelId;
            string result;
            switch (option)
            {
                case Options.Add:
                    try
                    {
                        result = await SocialMedia.AddSocialMediaUser(guildId, channelId, user, sendToChannel, filter);
                        await SendMessage(Client, result, sendToChannel);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    break;
                case Options.Delete:
                    result = SocialMedia.DeleteSocialMediaUser(guildId, user, SocialMediaType);
                    await SendMessage(Client, result);
                    break;
                case Options.Get:
                    result = await SocialMedia.GetSocialMediaUser(guildId, channelId, user);
                    await SendMessage(Client, result);
                    break;
                case Options.List:
                    result = await SocialMedia.ListSavedSocialMediaUsers(guildId, SocialMediaType, sendToChannel.ToString());
                    var embed = new EmbedBuilder()
                        .WithDescription(result)
                        .Build();
                    await SendMessage(Client, embed);
                    break;
                case Options.Update:
                    result = await SocialMedia.GetUpdatedSocialMediaUsers(guildId);
                    await SendMessage(Client, result);
                    break;
            }
        }
    }
}
