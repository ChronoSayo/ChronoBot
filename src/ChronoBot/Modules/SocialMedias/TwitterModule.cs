using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Timer = System.Timers;

namespace ChronoBot.Modules.SocialMedias
{
    [Group("twitter", "Notifies when a Tweeter has tweeted.")]
    public class TwitterModule : SocialMediaModule
    {
        public TwitterModule(DiscordSocketClient client, Twitter socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.Twitter;
        }

        private void SpamTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        [SlashCommand(AddCommand, "Adds YouTuber to the list of updates.", runMode: RunMode.Async)]
        public override async Task AddTwitterUser(
            [Summary("Tweeter", "Insert Twitter handle.")]
            string user,
            [Summary("Filter1", "Filter tweets based option.")]
            TwitterFiltersEnum.TwitterFilters filter1 = TwitterFiltersEnum.TwitterFilters.All,
            [Summary("Filter2", "Filter tweets based option.")]
            TwitterFiltersEnum.TwitterFilters filter2 = TwitterFiltersEnum.TwitterFilters.All,
            [Summary("Filter3", "Filter tweets based option.")]
            TwitterFiltersEnum.TwitterFilters filter3 = TwitterFiltersEnum.TwitterFilters.All,
            [Summary("Filter4", "Filter tweets based option.")]
            TwitterFiltersEnum.TwitterFilters filter4 = TwitterFiltersEnum.TwitterFilters.All,
            [Summary("Filter5", "Filter tweets based option.")]
            TwitterFiltersEnum.TwitterFilters filter5 = TwitterFiltersEnum.TwitterFilters.All,
            [Summary("Filter6", "Filter tweets based option.")]
            TwitterFiltersEnum.TwitterFilters filter6 = TwitterFiltersEnum.TwitterFilters.All,
            [Summary("Where", "To which channel should this be posted. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)]
            IChannel channel = null)
        {
            await base.AddTwitterUser(user, filter1, filter2, filter3, filter4, filter5, filter6, channel);
        }

        [SlashCommand(DeleteCommand, "Deletes Tweeter from the list of updates.", runMode: RunMode.Async)]
        public override Task DeleteSocialMediaUser(
            [Summary("Tweeter", "Insert Twitter handle.")]
            string user)
        {
            return base.DeleteSocialMediaUser(user);
        }

        [SlashCommand(GetCommand, "Posts Tweeter's latest tweet.", runMode: RunMode.Async)]
        public override Task GetSocialMediaUser(
            [Summary("Tweeter", "Insert Twitter handle.")]
            string user)
        {
            return base.GetSocialMediaUser(user);
        }

        [SlashCommand(ListCommand, "Gets a list of added Tweeters.", runMode: RunMode.Async)]
        public override Task ListSocialMediaUser()
        {
            return base.ListSocialMediaUser();
        }

        [SlashCommand(UpdateCommand, "Updates all listed Tweeters in the server.", runMode: RunMode.Async)]
        public override Task UpdateSocialMediaUser()
        {
            return base.UpdateSocialMediaUser();
        }

        [SlashCommand("show-twitter-video", "Use this if embedded video didn't work from the tweet.", runMode: RunMode.Async)]
        public async Task PostVideoAsync(
            [Summary("URL", "Insert X video URL.")]
            string url)
        {
            string video = await ((Twitter)SocialMedia).PostVideo(url);
            await SendMessage(Client, video);
        }

        [SlashCommand("convert-latest-fx", "Use this if embedded didn't work from the tweet.", runMode: RunMode.Async)]
        public async Task AddFxAsync()
        {
            string findFx = await ((Twitter)SocialMedia).GetMessageFromChannelHistory(Context.Guild.Id, Context.Channel.Id, "https://fx");
            string fx = ((Twitter)SocialMedia).AddFx(findFx);

            await SendMessage(Client, fx);
        }

        [SlashCommand("fx", "Use this if embedded didn't work from the tweet.", runMode: RunMode.Async)]
        public async Task AddFxAsync(
            [Summary("URL", "Insert X URL.")]
            string url)
        {
            string fx = ((Twitter)SocialMedia).AddFx(url);
            await SendMessage(Client, fx);
        }
    }
}
