using System;
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
        protected const string AddCommand = "add";
        protected const string DeleteCommand = "delete";
        protected const string GetCommand = "get";
        protected const string ListCommand = "list";
        protected const string UpdateCommand = "update";

        public enum Options
        {
            [ChoiceDisplay("Add")] Add, Delete, Get, List, Update
        }

        public SocialMediaModule(DiscordSocketClient client, SocialMedia socialMedia)
        {
            Client = client;
            SocialMedia = socialMedia;
        }

        public virtual async Task AddSocialMediaUser(
            string user,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)]
            IChannel channel = null)
        {
            await HandleOption(Options.Add, user, channel);
        }

        public virtual async Task AddTwitterUser(
            [Summary("Tweeter", "Insert Twitter handle.")]
            string user,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)]
            IChannel channel = null,
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
            await HandleOption(Options.Add, user, channel, filter);
        }

        public virtual async Task DeleteSocialMediaUser(string user)
        {
            await HandleOption(Options.Delete, user);
        }

        public virtual async Task GetSocialMediaUser(string user)
        {
            await HandleOption(Options.Get, user);
        }

        public virtual async Task ListSocialMediaUser()
        {
            await HandleOption(Options.List, null);
        }

        public virtual async Task UpdateSocialMediaUser()
        {
            await HandleOption(Options.Update, null);
        }

        protected virtual async Task SendFileWithLogo(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(Client, result, thumbnail);
            else
                await FollowupWithFileAsync(thumbnail, embed: result);
        }

        private async Task HandleOption(Options option, string user = "", [ChannelTypes(ChannelType.Text)] IChannel channel = null, string filter = "")
        {
            await DeferAsync();

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
                    result = await SocialMedia.GetSocialMediaUser(guildId, user);
                    await SendMessage(Client, result);
                    break;
                case Options.List:
                    result = await SocialMedia.ListSavedSocialMediaUsers(guildId, SocialMediaType,
                        Client.GetGuild(guildId).GetTextChannel(sendToChannel).Mention);
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
