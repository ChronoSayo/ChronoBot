using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2.Web;
using SharpLink;

namespace ChronoBot.Tools
{
    class Music
    {
        private readonly DiscordSocketClient _client;
        private readonly LavalinkManager _lavalinkManager;

        public Music(DiscordSocketClient client, LavalinkManager lavalinkManager)
        {
            _client = client;
            _lavalinkManager = lavalinkManager;

            PlayMusic().GetAwaiter().GetResult();
        }

        private async Task PlayMusic()
        {
            ulong guildId = 171304484096442368;//Info.DEBUG_GUILD_ID;
            SocketVoiceChannel voiceChannel = _client.GetGuild(guildId).GetVoiceChannel(795044947815694366);
            LavalinkPlayer player =
                _lavalinkManager.GetPlayer(guildId) ?? await _lavalinkManager.JoinAsync(voiceChannel);

            string query = "https://youtu.be/KNYGfG_ZEKE";
            var identifier = $"ytsearch:{query}";
            LoadTracksResponse response = await _lavalinkManager.GetTracksAsync(identifier);
            LavalinkTrack track = response.Tracks.First();
            await player.PlayAsync(track);
        }
    }
}
