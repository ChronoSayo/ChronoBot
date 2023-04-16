using System.Threading.Tasks;
using ChronoBot.Enums;
using ChronoBot.Utilities.SocialMedias;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ChronoBot.Modules.SocialMedias
{
    [Group("twitch", "Notifies when a Twitch streamer is broadcasting.")]
    public class TwitchModule : SocialMediaModule
    {
        public TwitchModule(DiscordSocketClient client, Twitch socialMedia) : base(client, socialMedia)
        {
            SocialMedia = socialMedia;
            SocialMediaType = SocialMediaEnum.Twitch;
        }

        [SlashCommand(AddCommand, "Adds streamer to the list of updates.", runMode: RunMode.Async)]
        public override Task AddSocialMediaUser(
            [Summary("Streamer", "Insert streamer's name.")] string user,
            [Summary("Where", "To which channel should this be updated to. Default is this channel.")]
            [ChannelTypes(ChannelType.Text)] IChannel channel = null)
        {
            return base.AddSocialMediaUser(user, channel);
        }

        [SlashCommand(DeleteCommand, "Deletes streamer from the list of updates.", runMode: RunMode.Async)]
        public override Task DeleteSocialMediaUser(
            [Summary("Streamer", "Insert streamer's name.")] string user)
        {
            return base.DeleteSocialMediaUser(user);
        }

        [SlashCommand(GetCommand, "Posts streamer's latest video.", runMode: RunMode.Async)]
        public override Task GetSocialMediaUser(
            [Summary("Streamer" , "Insert streamer's name.")]
            string user)
        {
            return base.GetSocialMediaUser(user);
        }

        [SlashCommand(ListCommand, "Gets a list of added streamers.", runMode: RunMode.Async)]
        public override Task ListSocialMediaUser()
        {
            return base.ListSocialMediaUser();
        }

        [SlashCommand(UpdateCommand, "Updates all listed streamers in the server.", runMode: RunMode.Async)]
        public override Task UpdateSocialMediaUser()
        {
            return base.UpdateSocialMediaUser();
        }
    }
}
