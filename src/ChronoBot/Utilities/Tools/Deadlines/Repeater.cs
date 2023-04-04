using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Modules.Tools.Deadlines;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Timers;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public class Repeater : Deadline
    {
        public Repeater(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users, int seconds = 60) :
            base(client, fileSystem, users, seconds)
        {
        }

        protected override async void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            foreach (DeadlineUserData user in Users)
            {
                if ((user.DeadlineType != DeadlineEnum.Repeater || now.DayOfWeek != user.Deadline.DayOfWeek) &&
                    now.Day == user.Deadline.Day)
                    continue;

                try
                {
                    var embed = DeadlineModule.DeadlineEmbed(user, user.Id, Client);
                    if (Statics.Debug)
                        await Statics.DebugSendMessageToChannelAsync(Client, embed);
                    else
                        await Client.GetGuild(user.GuildId).GetTextChannel(user.ChannelId)
                            .SendMessageAsync(embed: embed);
                }
                catch
                {
                    // Continue
                }
                finally
                {
                    user.Deadline = now;
                }
            }
        }
    }
}
