using System;
using System.Collections.Generic;
using System.Timers;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using ChronoBot.Modules.Tools.Deadlines;
using Discord.WebSocket;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public sealed class Reminder : Deadline
    {
        public Reminder(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users) :
            base(client, fileSystem, users)
        {
        }

        protected override async void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            List<DeadlineUserData> remindedUsers = new List<DeadlineUserData>();
            foreach (DeadlineUserData user in Users)
            {
                if (user.DeadlineType != DeadlineEnum.Reminder && now < user.Deadline)
                    continue;

                try
                {
                    var embed = DeadlineModule.DeadlineEmbed(user, user.Id, Client);
                    if (Statics.Debug)
                        await Statics.DebugSendMessageToChannelAsync(Client, embed);
                    else
                        await Client.GetGuild(user.GuildId).GetTextChannel(user.ChannelId).SendMessageAsync(embed: embed);
                }
                catch
                {
                    // Continue
                }

                remindedUsers.Add(user);
            }

            foreach (DeadlineUserData user in remindedUsers)
            {
                bool ok = FileSystem.DeleteInFile(user);
                if (ok)
                    Users.Remove(user);
            }
        }
    }
}
