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

        public enum Options
        {
            [ChoiceDisplay("Add")] Add, Delete, Get, List, Update
        }

        public SocialMediaModule(DiscordSocketClient client, SocialMedia socialMedia)
        {
            Client = client;
            SocialMedia = socialMedia;
        }

        public virtual async Task HandleOptions(Options option, string user, [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            ulong guildId = Context.Guild.Id;
            ulong channelId = Context.Channel.Id;
            ulong sendToChannel = channel == null ? channelId : channel.Id;
            string result;
            switch (option)
            {
                case Options.Add:
                    try
                    {
                        result = await SocialMedia.AddSocialMediaUser(guildId, channelId, user, sendToChannel);
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

        public virtual async Task HandleTwitterOption(Options option,
            string user,
            [ChannelTypes(ChannelType.Text)] IChannel channel = null,
            [Choice("Posts", "p")]
            [Choice("Retweets", "r")]
            [Choice("Likes", "l")]
            [Choice("QuoteTweets", "q")]
            [Choice("AllMedia", "m")]
            [Choice("Pictures", "mp")]
            [Choice("GIF", "mg")]
            [Choice("Video", "mv")]
            [Choice("All", "")] string filter = "")
        {
            try
            {
                ulong guildId = Context.Guild.Id;
                ulong channelId = Context.Channel.Id;
                ulong sendToChannel = channel == null ? channelId : channel.Id;  
                string result = await SocialMedia.AddSocialMediaUser(guildId, channelId, user, sendToChannel);
                await SendMessage(Client, result, sendToChannel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected virtual async Task SendFileWithLogo(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(Client, result, thumbnail);
            else
                await RespondWithFileAsync(thumbnail, embed: result);
        }
    }
}
