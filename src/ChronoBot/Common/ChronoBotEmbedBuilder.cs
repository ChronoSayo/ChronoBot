using Discord.Commands;

namespace ChronoBot.Common
{
    using Discord;
    using System.Diagnostics.Contracts;

    internal class ChronoBotEmbedBuilder : EmbedBuilder
    {
        public ChronoBotEmbedBuilder(string description)
        {
            WithDescription(description);
            WithColor(new Color(181, 24, 24));
        }

        public ChronoBotEmbedBuilder(ICommandContext commandContext)
        {
            WithAuthor(commandContext.User.Username);
            WithTitle($"Channel {commandContext.Channel.Name} in Guild {commandContext.Guild.Name}");
            WithDescription(commandContext.Message.Content);
            AddField("User Id", commandContext.User.Id, true);
            AddField("User Discr.", commandContext.User.Discriminator, true);
            AddField("Channel Id", commandContext.Channel.Id, true);
            AddField("Guild ID", commandContext.Guild.Id, true);
        }
    }
}
