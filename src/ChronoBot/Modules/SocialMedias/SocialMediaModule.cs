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
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ChronoBot.Modules.SocialMedias
{
    public class SocialMediaModule : InteractionModuleBase<SocketInteractionContext>
    {
        protected readonly DiscordSocketClient Client;
        private readonly ILogger<SocialMediaModule> _logger;
        protected SocialMedia SocialMedia;
        protected SocialMediaEnum SocialMediaType;

        public enum Actions
        {
            Add, Delete, Get, List, Update
        }

        public SocialMediaModule(DiscordSocketClient client, ILogger<SocialMediaModule> logger, SocialMedia socialMedia)
        {
            Client = client;
            _logger = logger;
            SocialMedia = socialMedia;
        }

        public virtual async Task Action(Actions actions, string user, [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            switch (actions)
            {
                case Actions.Add:
                    try
                    {
                        ulong guildId = Context.Guild.Id;
                        ulong channelId = Context.Channel.Id;
                        ulong sendToChannel = channel == null ? channelId : channel.Id;
                        if (option.Contains(sendToChannel.ToString()))
                            option = option.Remove(option.IndexOf('<'), option.IndexOf('>') - option.IndexOf('<') + 1);
                        string result = await SocialMedia.AddSocialMediaUser(guildId, channelId, user, sendToChannel, option);
                        await SendMessage(result, sendToChannel);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    break;
                case Actions.Delete:
                    break;
                case Actions.Get:
                    break;
                case Actions.List:
                    break;
                case Actions.Update:
                    break;
            }
        }

        public virtual async Task ActionTwitter(Actions actions, string user, 
            [Choice("Posts", "Shows only posts from user.")] string posts,
            [Choice("Retweets", "Shows only retweets from user.")] string retweets,
            [Choice("Likes", "Shows only likes from user.")] string likes,
            [Choice("QuoteTweets", "Shows only quote tweets from user.")] string quoteTweets,
            [Choice("AllMedia", "Shows all media from user.")] string allMedia,
            [Choice("Pictures", "Shows only pictures from user.")] string pictures,
            [Choice("GIF", "Shows only GIF's from user.")] string gifs,
            [Choice("Video", "Shows only videos from user.")] string videos,
            [Choice("All", "Shows everything.")] string all)
        {

        }

        public virtual async Task AddAsync(string user, [Remainder] string option = "")
        {
            try
            {
                ulong guildId = Context.Guild.Id;
                ulong channelId = Context.Channel.Id;
                ulong sendToChannel = Context.Message.MentionedChannels.Count > 0 ? Context.Message.MentionedChannels.FirstOrDefault()!.Id : 0;
                if (option.Contains(sendToChannel.ToString()))
                    option = option.Remove(option.IndexOf('<'), option.IndexOf('>') - option.IndexOf('<') + 1);
                string result = await SocialMedia.AddSocialMediaUser(guildId, channelId, user, sendToChannel, option);
                await SendMessage(result, sendToChannel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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

        protected virtual async Task SendMessage(string result, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(Client, result);
            else if (sendToChannel != 0)
                await Client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(result);
            else
                await RespondAsync(result);
        }
        protected virtual async Task SendMessage(Embed result, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(Client, result);
            else if (sendToChannel != 0)
                await Client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(embed: result);
            else
                await RespondAsync("", embed: result);
        }
        protected virtual async Task SendMessage(string result, Embed resultEmbed, ulong sendToChannel = 0)
        {
            if (Statics.Debug)
                await Statics.DebugSendMessageToChannelAsync(Client, result, resultEmbed);
            else if (sendToChannel != 0)
                await Client.GetGuild(Context.Guild.Id).GetTextChannel(sendToChannel).SendMessageAsync(embed: resultEmbed);
            else
                await RespondAsync(result, embed: resultEmbed);
        }
        protected virtual async Task SendFileWithLogo(Embed result, string socialMedia)
        {
            string thumbnail = Path.Combine(Environment.CurrentDirectory, $@"Resources\Images\SocialMedia\{socialMedia}.png");
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(Client, result, thumbnail);
            else
                await Context.Channel.SendFileAsync(thumbnail, embed: result);
        }
        protected virtual async Task SendFile(Embed result, string file)
        {
            if (Statics.Debug)
                await Statics.DebugSendFileToChannelAsync(Client, result, file);
            else
                await Context.Channel.SendFileAsync(file, embed: result);
        }
    }
}
