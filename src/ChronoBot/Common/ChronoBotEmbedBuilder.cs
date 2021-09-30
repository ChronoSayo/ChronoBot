namespace ChronoBot.Common
{
    using Discord;

    internal class ChronoBotEmbedBuilder : EmbedBuilder
    {
        public ChronoBotEmbedBuilder(string description)
        {
            WithDescription(description);
            WithColor(new Color(181, 24, 24));
        }
    }
}
