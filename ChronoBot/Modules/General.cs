using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ChronoBot.Modules
{
    public class General : ModuleBase<SocketCommandContext>
    {
        //[Command("ping")]
        //[Alias("p", "test")]
        ////[RequireUserPermission(GuildPermission.Administrator)]
        ////[RequireBotPermission(GuildPermission.Administrator)]
        //[RequireOwner]
        //[Summary("Testing")]
        //public async Task PingAsync()
        //{
        //    await Context.Channel.TriggerTypingAsync();
        //    await Context.Channel.SendMessageAsync("Pong!");
        //}

        //[Command("info")]
        //public async Task InfoAsync(SocketGuildUser socketGuildUser = null)
        //{
        //    if (socketGuildUser == null)
        //    {
        //        socketGuildUser = Context.User as SocketGuildUser;
        //    }

        //    await ReplyAsync($"ID: {socketGuildUser.Id}\n" +
        //                     $"Name: {socketGuildUser.Username}#{socketGuildUser.Discriminator}");
        //}
    }
}
