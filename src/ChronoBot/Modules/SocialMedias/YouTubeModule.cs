using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    [Group("youtube", "Notifies when a YouTuber has uploaded a video.")]
    public class YouTubeModule : SocialMediaModule
    {
        public YouTubeModule(DiscordSocketClient client, YouTube socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.YouTube;
        }

        [SlashCommand(AddCommand, "Adds YouTuber to the list of updates.", runMode: RunMode.Async)]
        public override Task AddSocialMediaUser(
            [Summary("Youtuber", "Insert YouTuber's name.")] string user,
            [Summary("Where", "To which channel should this be updated to. Default is this channel.")]
                [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            return base.AddSocialMediaUser(user, channel);
        }

        [SlashCommand(DeleteCommand, "Deletes YouTuber from the list of updates.", runMode: RunMode.Async)]
        public override Task DeleteSocialMediaUser(
            [Summary("Youtuber", "Insert YouTuber's name.")] string user)
        {
            return base.DeleteSocialMediaUser(user);
        }

        [SlashCommand(GetCommand, "Posts YouTuber's latest video.", runMode: RunMode.Async)]
        public override Task GetSocialMediaUser(
            [Summary("Youtuber", "Insert YouTuber's name.")]
            string user)
        {
            return base.GetSocialMediaUser(user);
        }

        [SlashCommand(ListCommand, "Gets a list of added YouTubers.", runMode: RunMode.Async)]
        public override Task ListSocialMediaUser()
        {
            return base.ListSocialMediaUser();
        }

        [SlashCommand(UpdateCommand, "Updates all listed YouTubers in the server.", runMode: RunMode.Async)]
        public override Task UpdateSocialMediaUser()
        {
            return base.UpdateSocialMediaUser();
        }
    }
}
