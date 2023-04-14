using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using Discord.WebSocket;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public sealed class Reminder : Deadline
    {
        public Reminder(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users, int seconds = 60) :
            base(client, fileSystem, users, seconds)
        {
        }

        protected override void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            List<DeadlineUserData> remindedUsers = new List<DeadlineUserData>();
            foreach (DeadlineUserData user in Users)
            {
                if (user.DeadlineType != DeadlineEnum.Reminder || now < user.Deadline)
                    continue;

                SendMessage(user);

                remindedUsers.Add(user);
            }

            foreach (var user in from user in remindedUsers let ok = FileSystem.DeleteInFile(user) where ok select user)
                Users.Remove(user);
        }
    }
}
